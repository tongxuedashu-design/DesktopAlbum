using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PhotoFrameSettings
{
    // Publication artwork uses only synthetic defaults. Never load a user's
    // configuration, photo library, desktop, or an existing application window.
    internal static class ReleaseScreenshots
    {
        private static Control Find(Control control,string name){return control.Controls.Find(name,true)[0];}
        private static void Wait(int milliseconds){DateTime until=DateTime.UtcNow.AddMilliseconds(milliseconds);while(DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(12);}}
        private static void Capture(Control control,string path)
        {
            using(Bitmap bitmap=new Bitmap(control.Width,control.Height))
            {
                control.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));
                bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);
            }
        }
        [STAThread] private static int Main(string[] args)
        {
            string output=Path.GetFullPath(args[0]);
            string fixture=Path.Combine(Path.GetTempPath(),"AlbumPublication_"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(fixture,"Config"));Directory.CreateDirectory(output);
            try
            {
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                Dictionary<string,string> values=Schema.Defaults(fixture);
                values["PhotosPath"]=@"C:\Photos\Sample Album";
                StringBuilder config=new StringBuilder("[Variables]\r\n");
                foreach(KeyValuePair<string,string> pair in values)config.AppendLine(pair.Key+"="+pair.Value);
                File.WriteAllText(Path.Combine(fixture,"Config","PhotoFrame.inc"),config.ToString(),Encoding.Unicode);
                using(SettingsForm form=new SettingsForm(fixture,false))
                {
                    form.Show();Wait(180);
                    ((System.Windows.Forms.Timer)typeof(SettingsForm).GetField("statusTimer",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form)).Stop();
                    // Explicitly label the generic library as demonstration data.
                    Find(form,"PhotoStatus").Text="示例照片库 · 展示用";
                    Find(form,"PhotoStatus").ForeColor=Theme.Muted;
                    Find(form,"Status").Text="界面演示 · 默认参数";
                    string[] names={"settings","appearance","hover","controls","tools"};
                    for(int i=0;i<names.Length;i++)
                    {
                        ((Button)Find(form,"Navigation"+i)).PerformClick();Wait(280);
                        // Remove mouse/keyboard focus from the artwork only.
                        // Normal application keyboard navigation remains enabled.
                        form.ActiveControl=null;
                        Capture(form,Path.Combine(output,names[i]+".png"));
                    }
                    ((CheckBox)Find(form,"TrayOnClose")).Checked=false;
                    form.Close();
                }
                Console.WriteLine("Generated 5 screenshots from synthetic defaults.");return 0;
            }
            catch(Exception ex){Console.WriteLine(ex);return 1;}
            finally{if(Directory.Exists(fixture))Directory.Delete(fixture,true);}
        }
    }
}
