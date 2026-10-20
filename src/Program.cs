using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PhotoFrameSettings
{
    internal static class Program
    {
        internal const string Title="桌面相册 · 设置中心";
        [STAThread] private static void Main(string[] args)
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            string root=FindRoot(AppDomain.CurrentDomain.BaseDirectory);
            if(root==null) {MessageBox.Show("缺少相册配置文件，请完整解压后再打开。",Title);return;}
            try
            {
                using(Mutex initialization=new Mutex(false,"Local\\PhotoFrameInitialization_"+StableId(root)))
                {
                    bool acquired=false;
                    try
                    {
                        try {acquired=initialization.WaitOne(10000);}catch(AbandonedMutexException){acquired=true;}
                        if(!acquired)throw new IOException("另一份桌面相册正在准备配置，请稍后重试。");
                        PortableSetup.Ensure(root);
                    }
                    finally {if(acquired)initialization.ReleaseMutex();}
                }
            }
            catch(Exception ex) {MessageBox.Show("无法准备相册配置。\r\n"+ex.Message+"\r\n请确认已完整解压，且可以在所在文件夹中保存文件。",Title,MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
            if(Array.IndexOf(args,"--open-library")>=0)
            {
                try {SettingsStore store=new SettingsStore(AppPaths.Settings(root),BackupDirectory(root));string path=store.ReadValues()["PhotosPath"];if(!Directory.Exists(path))throw new DirectoryNotFoundException("照片库文件夹不存在，请在设置中心重新选择。");Process.Start(new ProcessStartInfo("explorer.exe","\""+path+"\""){UseShellExecute=true});}
                catch(Exception ex) {MessageBox.Show(ex.Message,Title,MessageBoxButtons.OK,MessageBoxIcon.Warning);}return;
            }
            if(Array.IndexOf(args,"--start-frame")>=0 || (args.Length==0 && IsPortable(root)))
            {
                try {new FrameController(root).Start();}
                catch(Exception ex) {MessageBox.Show(ex.Message,Title,MessageBoxButtons.OK,MessageBoxIcon.Warning);}
            }
            bool owned;
            using(Mutex mutex=new Mutex(true,"Local\\PhotoFrameSettings_"+StableId(root),out owned))
            {
                if(!owned) {if(Array.IndexOf(args,"--tray")<0)Native.ShowExisting(Title);return;}
                try { Application.Run(new SettingsForm(root,Array.IndexOf(args,"--tray")>=0)); }
                catch(Exception ex) {MessageBox.Show("设置中心无法启动。\r\n"+ex.Message,Title,MessageBoxButtons.OK,MessageBoxIcon.Error);}
                finally {mutex.ReleaseMutex();}
            }
        }
        internal static string FindRoot(string directory)
        {
            DirectoryInfo p=new DirectoryInfo(directory);
            while(p!=null) {string config=AppPaths.Config(p.FullName);if(File.Exists(AppPaths.Settings(p.FullName)) || File.Exists(AppPaths.Template(p.FullName)))return p.FullName;p=p.Parent;}return null;
        }
        internal static string WorkspaceRoot(string directory)
        {
            DirectoryInfo current=new DirectoryInfo(directory);
            return current.Name=="runtime" && current.Parent!=null?current.Parent.FullName:directory;
        }
        internal static bool IsPortable(string root)
        {return File.Exists(Path.Combine(AppPaths.Config(root),"portable.json")) || File.Exists(Path.Combine(AppPaths.Config(root),"便携版.json")) || File.Exists(Path.Combine(root,"便携版.json"));}
        internal static string BackupDirectory(string root)
        {return IsPortable(root)?Path.Combine(AppPaths.Config(root),"Backups","SettingsCenter"):Path.Combine(WorkspaceRoot(root),"artifacts","Backups","SettingsCenter");}
        internal static string UserGuide(string root)
        {return IsPortable(root)?Path.Combine(root,"Docs","USER_GUIDE.md"):Path.Combine(WorkspaceRoot(root),"docs","USER_GUIDE.md");}
        private static string StableId(string path)
        {using(var hash=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(path.ToUpperInvariant()))).Replace("-","").Substring(0,20);}
    }

    // Portable copies create their own configuration on first launch. Only the
    // built-in photo library follows a moved folder; an external choice stays put.
    internal static class PortableSetup
    {
        internal static void Ensure(string root)
        {
            string config=AppPaths.Config(root),settings=AppPaths.Settings(root);
            string position=AppPaths.Location(root),library=AppPaths.Photos(root);
            string previous=File.Exists(position)?File.ReadAllText(position,Encoding.UTF8).Trim():null;
            if(!File.Exists(settings))
            {
                string template=AppPaths.Template(root);
                File.WriteAllText(settings,File.ReadAllText(template).Replace("__PHOTO_LIBRARY__",library),Encoding.Unicode);
            }
            string text=File.ReadAllText(settings);
            string oldLibrary=!String.IsNullOrEmpty(previous) && Path.IsPathRooted(previous)?Path.Combine(previous,Path.GetFileName(library)):null;
            string legacyLibrary=oldLibrary!=null?Path.Combine(previous,"03_照片库"):null;
            string formerLibrary=oldLibrary!=null?Path.Combine(previous,"照片库"):null;
            string updated=System.Text.RegularExpressions.Regex.Replace(text,@"(?m)^PhotosPath=([^\r\n]*)",delegate(System.Text.RegularExpressions.Match match){
                string path=match.Groups[1].Value.Trim();
                return path=="__PHOTO_LIBRARY__" || (oldLibrary!=null && (String.Equals(path,oldLibrary,StringComparison.OrdinalIgnoreCase) || String.Equals(path,legacyLibrary,StringComparison.OrdinalIgnoreCase) || String.Equals(path,formerLibrary,StringComparison.OrdinalIgnoreCase)))?"PhotosPath="+library:match.Value;
            });
            if(updated!=text)File.WriteAllText(settings,updated,Encoding.Unicode);
            string engineConfig=Path.Combine(config,"Rainmeter.ini");
            if(!File.Exists(engineConfig))
            {
                string template=Path.Combine(config,"Rainmeter.example.ini");
                string skinPath=Path.Combine(config,"Skins")+Path.DirectorySeparatorChar;
                File.WriteAllText(engineConfig,File.ReadAllText(template).Replace("__SKIN_PATH__",skinPath),Encoding.Unicode);
            }
            // Existing source installations do not need a new location record.
            if(Program.IsPortable(root))
            {
                Directory.CreateDirectory(library);
                if(previous!=root)File.WriteAllText(position,root,new UTF8Encoding(false));
            }
        }
    }

    internal static class Native
    {
        internal delegate bool EnumProc(IntPtr window,IntPtr data);
        [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumProc proc,IntPtr data);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern int GetClassName(IntPtr window,StringBuilder text,int count);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern int GetWindowText(IntPtr window,StringBuilder text,int count);
        [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern IntPtr FindWindow(string cls,string title);
        [DllImport("user32.dll")] internal static extern bool ShowWindow(IntPtr hwnd,int command);
        [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr hwnd);
        [DllImport("user32.dll")] internal static extern bool PostMessage(IntPtr hwnd,uint message,IntPtr wparam,IntPtr lparam);
        [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)] internal static extern IntPtr SendMessageTimeout(IntPtr window,uint message,IntPtr wparam,ref CopyData data,uint flags,uint timeout,out IntPtr result);
        [StructLayout(LayoutKind.Sequential)] internal struct CopyData {internal IntPtr Tag;internal int Size;internal IntPtr Data;}
        internal static void ShowExisting(string title)
        {
            IntPtr window=FindWindow(null,title);
            if(window!=IntPtr.Zero)PostMessage(window,0x802A,IntPtr.Zero,IntPtr.Zero);
        }
    }

    internal sealed class FrameController
    {
        private readonly string root, executable;
        internal FrameController(string folder) {root=folder;executable=Path.Combine(AppPaths.Engine(root),"Rainmeter","Rainmeter.exe");}
        internal IntPtr FindWindow(bool controlWindow)
        {
            IntPtr found=IntPtr.Zero;
            Native.EnumWindows(delegate(IntPtr hwnd,IntPtr unused) {
                StringBuilder cls=new StringBuilder(256),title=new StringBuilder(512);Native.GetClassName(hwnd,cls,256);Native.GetWindowText(hwnd,title,512);
                if(controlWindow ? cls.ToString()!="DummyRainWClass" : cls.ToString()!="RainmeterMeterWindow" || !String.Equals(title.ToString(),Path.Combine(AppPaths.Config(root),"Skins","Reminisce","Reminisce.ini"),StringComparison.OrdinalIgnoreCase))return true;
                uint pid;Native.GetWindowThreadProcessId(hwnd,out pid);
                try { using(Process p=Process.GetProcessById((int)pid))if(String.Equals(p.MainModule.FileName,executable,StringComparison.OrdinalIgnoreCase)){found=hwnd;return false;} }
                catch(System.ComponentModel.Win32Exception) {} catch(InvalidOperationException) {} catch(ArgumentException) {}
                return true;
            },IntPtr.Zero);return found;
        }
        internal bool Running {get{return FindWindow(false)!=IntPtr.Zero;}}
        internal void Send(string command)
        {
            IntPtr hwnd=FindWindow(false);if(hwnd==IntPtr.Zero)hwnd=FindWindow(true);if(hwnd==IntPtr.Zero)throw new InvalidOperationException("相框还没启动，请在「照片与轮播」页点击「启动」。");
            IntPtr text=Marshal.StringToHGlobalUni(command);
            try {Native.CopyData data=new Native.CopyData{Tag=new IntPtr(1),Size=(command.Length+1)*2,Data=text};IntPtr answer;
                if(Native.SendMessageTimeout(hwnd,0x4A,IntPtr.Zero,ref data,2,2000,out answer)==IntPtr.Zero)throw new IOException("相框暂时未响应，请稍后再试。");}
            finally {Marshal.FreeHGlobal(text);}
        }
        internal void Start()
        {
            if(Running)return;
            if(FindWindow(true)!=IntPtr.Zero) {Send("[!ActivateConfig Reminisce Reminisce.ini]");return;}
            if(!File.Exists(executable))throw new FileNotFoundException("缺少相框引擎，请重新完整解压便携包。",executable);
            // Rainmeter stores an absolute SkinPath. Refresh it when a portable
            // copy has moved, preserving the user's other engine preferences.
            string ini=Path.Combine(AppPaths.Config(root),"Rainmeter.ini");
            string text=File.ReadAllText(ini),skinPath=Path.Combine(AppPaths.Config(root),"Skins")+Path.DirectorySeparatorChar;
            string next=System.Text.RegularExpressions.Regex.Replace(text,@"(?m)^SkinPath=[^\r\n]*",delegate(System.Text.RegularExpressions.Match match){return "SkinPath="+skinPath;});
            if(next!=text)File.WriteAllText(ini,next,Encoding.Unicode);
            Process.Start(new ProcessStartInfo(executable,"\""+ini+"\""){WorkingDirectory=root,UseShellExecute=true});
            // The user may have closed the engine while this skin was inactive.
            // Activate explicitly after its own control window becomes available.
            for(int attempt=0;attempt<60;attempt++)
            {
                if(Running)return;
                if(attempt%10==0 && FindWindow(true)!=IntPtr.Zero)Send("[!ActivateConfig Reminisce Reminisce.ini]");
                Thread.Sleep(100);
            }
            throw new IOException("相框启动后没有响应，请稍后再试。");
        }
        internal void Apply() {if(Running)Send("[!Refresh Reminisce]");else Start();}
        internal void Stop() {if(Running)Send("[!DeactivateConfig Reminisce]");}
    }

    public sealed partial class SettingsForm : Form
    {
        private readonly string root;
        private readonly SettingsStore store;
        private readonly FrameController frame;
        private readonly Dictionary<string,NumericUpDown> numbers=new Dictionary<string,NumericUpDown>();
        private readonly Dictionary<string,CheckBox> inherit=new Dictionary<string,CheckBox>();
        private readonly Dictionary<string,CheckBox> switches=new Dictionary<string,CheckBox>();
        private readonly Dictionary<string,TextBox> colors=new Dictionary<string,TextBox>();
        private TextBox photos;
        private Label status, photoStatus;
        private CheckBox trayOnClose;
        private NotifyIcon tray;
        private System.Windows.Forms.Timer statusTimer;
        private TabControl tabs;
        private bool loading, dirty, exit, trayHint;
        private readonly bool startInTray;
        private string feedback="";
        internal SettingsForm(string directory,bool background)
        {
            root=directory;startInTray=background;
            store=new SettingsStore(AppPaths.Settings(root),Program.BackupDirectory(root));
            frame=new FrameController(root);
            Text=Program.Title;Name="SettingsCenter";Font=new Font("Microsoft YaHei UI",9F);
            BackColor=Theme.Canvas;ForeColor=Theme.Ink;
            AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(1120,730);MinimumSize=new Size(1040,660);StartPosition=FormStartPosition.CenterScreen;
            Icon=Theme.CreateIcon();DoubleBuffered=true;
            Build();LoadValues(false);BuildTray();
            FormClosing+=OnClosing;Shown+=delegate {if(startInTray)Hide();};
            statusTimer=new System.Windows.Forms.Timer();statusTimer.Interval=2500;statusTimer.Tick+=delegate{UpdateStatus();};statusTimer.Start();
        }
        private void Changed(object sender,EventArgs args) {if(loading)return;dirty=true;feedback="";UpdateStatus();}
        private void LoadValues(bool defaults)
        {
            Dictionary<string,string> values=Schema.Defaults(root);
            if(!defaults)foreach(KeyValuePair<string,string> item in store.ReadValues())values[item.Key]=item.Value;
            else values["PhotosPath"]=photos.Text;
            loading=true;
            try
            {
                photos.Text=values["PhotosPath"];
                foreach(SettingSpec spec in Schema.Numbers)
                {
                    decimal value;
                    if(!Decimal.TryParse(values[spec.Key],NumberStyles.Number,CultureInfo.InvariantCulture,out value))throw new InvalidDataException(spec.Key+" 需要填写数字，请检查配置文件。");
                    if(value<spec.Minimum || value>spec.Maximum || (spec.Decimals==0 && value!=Decimal.Truncate(value)))throw new InvalidDataException(spec.Label+"的数值超出允许范围，请检查配置文件。");
                    NumericUpDown input=numbers[spec.Key];
                    if(inherit.ContainsKey(spec.Key)) {inherit[spec.Key].Checked=value<0;input.Enabled=value>=0;input.Value=value<0?numbers["HoverFadeDuration"].Value:value;}
                    else input.Value=value;
                }
                foreach(string key in Schema.Switches)switches[key].Checked=values[key]!="0";
                foreach(string key in colors.Keys)colors[key].Text=values[key];
            }
            finally {loading=false;}
            dirty=defaults;feedback=defaults?"已填入默认值，点击保存后生效。":"";UpdateStatus();
        }
        private Dictionary<string,string> Gather()
        {
            Dictionary<string,string> result=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);result["PhotosPath"]=photos.Text.Trim().TrimEnd(Path.DirectorySeparatorChar);
            if(result["PhotosPath"].Length==2 && result["PhotosPath"][1]==':')result["PhotosPath"]+=Path.DirectorySeparatorChar;
            foreach(SettingSpec spec in Schema.Numbers)
            {NumericUpDown input=numbers[spec.Key];input.ValidateEditTextForSettings();result[spec.Key]=inherit.ContainsKey(spec.Key)&&inherit[spec.Key].Checked?"-1":input.Value.ToString(CultureInfo.InvariantCulture);}
            foreach(string key in Schema.Switches)result[key]=switches[key].Checked?"1":"0";
            foreach(string key in colors.Keys)result[key]=colors[key].Text.Trim();return result;
        }
        private void Save(bool apply)
        {
            Guard(delegate {
                Dictionary<string,string> values=Gather();string error=Schema.Validate(values);if(error!=null)throw new InvalidDataException(error);
                string backup=store.Save(values);dirty=false;
                feedback=backup==null?"设置已保存。":"设置已保存，旧配置已自动备份。";
                if(apply)
                {
                    try {frame.Apply();feedback+=" 已通知相框更新。";}
                    catch(Exception ex) {UpdateStatus();MessageBox.Show(this,"设置已保存，相框暂时没能更新。\r\n"+ex.Message,Program.Title,MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
                }
                UpdateStatus();
            });
        }
        private void Reload()
        {
            if(dirty && MessageBox.Show(this,"重新读取会丢弃还没保存的修改。继续吗？",Program.Title,MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
            Guard(delegate{store.Reload();LoadValues(false);feedback="已重新读取配置。";UpdateStatus();});
        }
        private void Defaults(object sender,EventArgs e)
        {if(MessageBox.Show(this,"要填回默认设置吗？\r\n照片库位置不变，保存后才会生效。",Program.Title,MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes)Guard(delegate{LoadValues(true);});}
        private void Browse(object sender,EventArgs e)
        {using(FolderBrowserDialog dialog=new FolderBrowserDialog()){dialog.Description="选择照片库文件夹（自动包含子文件夹）";dialog.ShowNewFolderButton=true;if(Directory.Exists(photos.Text))dialog.SelectedPath=photos.Text;if(dialog.ShowDialog(this)==DialogResult.OK){photos.Text=dialog.SelectedPath;UpdateStatus();}}}
        private void OpenFolder(string path)
        {if(!Directory.Exists(path))throw new DirectoryNotFoundException("该文件夹不存在，请重新选择。");Process.Start(new ProcessStartInfo("explorer.exe","\""+path+"\""){UseShellExecute=true});}
        private void UpdateStatus()
        {
            if(status==null)return;
            bool running=frame.Running;
            status.Text=dirty?"● 有未保存的修改":String.IsNullOrEmpty(feedback)?"设置已载入":feedback;
            status.ForeColor=dirty?Color.FromArgb(158,106,38):Theme.Muted;
            RefreshVisuals(running);
            photoStatus.Text=Directory.Exists(photos.Text)?"✓ 照片文件夹可用":"找不到照片文件夹，请重新选择。";
            photoStatus.ForeColor=Directory.Exists(photos.Text)?Color.FromArgb(22,101,52):Color.FromArgb(180,83,9);
        }
        private void Guard(Action action)
        {try{action();}catch(Exception ex){MessageBox.Show(this,ex.Message,Program.Title,MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
        private bool ReadTrayPreference()
        {try{return File.ReadAllText(AppPaths.Preferences(root)).Trim()!="CloseToTray=0";}catch(IOException){return true;}}
        private void SaveTrayPreference()
        {File.WriteAllText(AppPaths.Preferences(root),trayOnClose.Checked?"CloseToTray=1":"CloseToTray=0",new UTF8Encoding(true));}
        private void BuildTray()
        {
            tray=new NotifyIcon{Text=Program.Title,Icon=Icon,Visible=true};ContextMenuStrip menu=new ContextMenuStrip();
            menu.Items.Add("打开设置中心",null,delegate{ShowCenter();});menu.Items.Add("打开照片库",null,delegate{Guard(delegate{OpenFolder(photos.Text);});});
            menu.Items.Add("启动相框",null,delegate{Guard(delegate{frame.Start();});});menu.Items.Add("关闭相框",null,delegate{Guard(delegate{frame.Stop();});});
            menu.Items.Add(new ToolStripSeparator());menu.Items.Add("退出设置中心",null,delegate{exit=true;Close();exit=false;});tray.ContextMenuStrip=menu;tray.DoubleClick+=delegate{ShowCenter();};
        }
        private void ShowCenter(){Show();WindowState=FormWindowState.Normal;Activate();UpdateStatus();}
        protected override void WndProc(ref Message message)
        {if(message.Msg==0x802A){ShowCenter();return;}base.WndProc(ref message);}
        private void OnClosing(object sender,FormClosingEventArgs e)
        {
            if(e.CloseReason==CloseReason.WindowsShutDown || e.CloseReason==CloseReason.TaskManagerClosing)return;
            if(!exit && trayOnClose.Checked)
            {e.Cancel=true;Hide();if(!trayHint){tray.ShowBalloonTip(2000,Program.Title,"设置中心仍在托盘运行，双击图标即可打开。",ToolTipIcon.Info);trayHint=true;}return;}
            if(dirty)
            {
                DialogResult result=MessageBox.Show(this,"仍有未保存的设置。退出前保存吗？",Program.Title,MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question);
                if(result==DialogResult.Cancel){e.Cancel=true;return;}
                if(result==DialogResult.Yes){Save(false);if(dirty)e.Cancel=true;}
            }
        }
        protected override void Dispose(bool disposing)
        {if(disposing){if(statusTimer!=null)statusTimer.Dispose();if(tray!=null){tray.Visible=false;tray.Dispose();}}base.Dispose(disposing);}
    }
    internal static class NumericExtensions
    {
        internal static void ValidateEditTextForSettings(this NumericUpDown input)
        {decimal parsed;if(!Decimal.TryParse(input.Text,NumberStyles.Number,CultureInfo.CurrentCulture,out parsed) || parsed<input.Minimum || parsed>input.Maximum || (input.DecimalPlaces==0 && Decimal.Truncate(parsed)!=parsed))throw new InvalidDataException(input.Name+" 的数值格式不正确，或超出了允许范围。");input.Value=parsed;}
    }
}
