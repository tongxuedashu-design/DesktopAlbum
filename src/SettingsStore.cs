using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PhotoFrameSettings
{
    internal static class AppPaths
    {
        // New copies use English names. Existing copies can retain their names.
        private static string DirectoryPath(string root,string current,string previous,string legacy)
        {
            foreach(string name in new string[]{current,previous,legacy})
                if(Directory.Exists(Path.Combine(root,name)))return Path.Combine(root,name);
            return Path.Combine(root,current);
        }
        private static string FilePath(string folder,string current,string previous)
        {string path=Path.Combine(folder,current);return File.Exists(path) || !File.Exists(Path.Combine(folder,previous))?path:Path.Combine(folder,previous);}
        internal static string Engine(string root){return DirectoryPath(root,"Engine","程序","01_程序");}
        internal static string Config(string root){return DirectoryPath(root,"Config","配置","02_配置");}
        internal static string Photos(string root){return DirectoryPath(root,"Photos","照片库","03_照片库");}
        internal static string Settings(string root){return FilePath(Config(root),"PhotoFrame.inc","相框设置.inc");}
        internal static string Template(string root){return FilePath(Config(root),"PhotoFrame.example.inc","相框设置.example.inc");}
        internal static string Preferences(string root){return FilePath(Config(root),"SettingsCenter.ini","设置中心.ini");}
        internal static string Location(string root){return FilePath(Config(root),"location.txt","便携位置.txt");}
    }

    public sealed class SettingSpec
    {
        public readonly string Key, Label, Unit, Hint, Default;
        public readonly decimal Minimum, Maximum;
        public readonly int Decimals;
        public SettingSpec(string key, string label, string unit, string hint, string value, decimal min, decimal max, int decimals)
        { Key=key; Label=label; Unit=unit; Hint=hint; Default=value; Minimum=min; Maximum=max; Decimals=decimals; }
    }

    public static class Schema
    {
        public static readonly SettingSpec[] Numbers = new SettingSpec[] {
            new SettingSpec("ChangeSeconds","换图间隔","秒","自动播放时随机换图，手动翻图按文件名排序。","30",1,86400,0),
            new SettingSpec("SizeW","最大宽度","像素","大照片按比例缩小，不会拉伸或裁切。","480",1,8192,0),
            new SettingSpec("SizeH","最大高度","像素","宽高不超过设定值，小照片保留原尺寸。","480",1,8192,0),
            new SettingSpec("CornerRadius","圆角半径","像素","0 为直角，数值越大，边角越圆。","16",0,1000,0),
            new SettingSpec("ShadowOpacity","阴影强度","0–255","0 关闭阴影，数值越大阴影越明显。","75",0,255,0),
            new SettingSpec("FramePadding","阴影留白","像素","照片外围留给阴影的空间，最少 20 像素。","20",20,500,0),
            new SettingSpec("OutlineWidth","细描边宽度","像素","0 关闭细描边；默认 0.7 像素。","0.7",0,20,1),
            new SettingSpec("HoverOpacity","悬停可见度","1–255","数值越小越透明，255 完全可见。\n90 约为 65% 透明。","90",1,255,0),
            new SettingSpec("HoverFadeDuration","共用过渡时长","毫秒","移入、移出默认使用这个时长，0 表示立即切换。","180",0,5000,0),
            new SettingSpec("HoverEnterDuration","移入淡化时长","毫秒","关闭下方开关，就可以单独设置移入时长。","-1",-1,5000,0),
            new SettingSpec("HoverLeaveDuration","移出恢复时长","毫秒","可单独调整离开相框后的恢复速度。","-1",-1,5000,0),
            new SettingSpec("ControlTriggerPercent","底部触发范围","%","占照片高度的比例。\n小图至少留 64 像素。","30",1,100,0),
            new SettingSpec("ControlShowDuration","出现时长","毫秒","控制栏从下方滑入，0 表示立即显示。","240",0,5000,0),
            new SettingSpec("ControlHideDuration","收起时长","毫秒","离开底部区域后向下收起，0 表示立即隐藏。","200",0,5000,0),
            new SettingSpec("ControlSlideDistance","滑动距离","像素","默认 54 像素，设为 0 时只淡入淡出。","54",0,200,0)
        };
        public static readonly string[] Switches = { "HoverEnabled", "ShowControls" };
        public static SettingSpec Find(string key)
        { foreach (SettingSpec spec in Numbers) if(spec.Key==key) return spec; throw new ArgumentException(key); }
        public static Dictionary<string,string> Defaults(string root)
        {
            Dictionary<string,string> result = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            foreach(SettingSpec spec in Numbers) result[spec.Key]=spec.Default;
            result["PhotosPath"]=AppPaths.Photos(root);
            result["HoverEnabled"]="1"; result["ShowControls"]="1";
            result["BorderColor"]="ffffff30"; result["BackColor"]="181a20ee";
            return result;
        }
        public static string Validate(Dictionary<string,string> values)
        {
            string path;
            if(!values.TryGetValue("PhotosPath",out path) || String.IsNullOrWhiteSpace(path)) return "请选择照片库文件夹。";
            if(path.IndexOfAny(new char[]{'\r','\n','\0','\"'})>=0) return "照片库路径包含无法使用的字符。";
            try { if(!Path.IsPathRooted(path) || !Directory.Exists(path)) return "找不到照片文件夹，请点击「选择文件夹」重新选择。"; }
            catch(Exception) { return "照片库路径格式不正确，请重新选择文件夹。"; }
            foreach(SettingSpec spec in Numbers)
            {
                decimal n; string value;
                if(!values.TryGetValue(spec.Key,out value) || !Decimal.TryParse(value,NumberStyles.Number,CultureInfo.InvariantCulture,out n) || n<spec.Minimum || n>spec.Maximum || (spec.Decimals==0 && Decimal.Truncate(n)!=n))
                    return spec.Label+"应在 "+spec.Minimum+" 到 "+spec.Maximum+" 之间。";
            }
            foreach(string key in Switches) if(!values.ContainsKey(key) || (values[key]!="0" && values[key]!="1")) return key+" 的值只能是 0 或 1。";
            foreach(string key in new string[]{"BorderColor","BackColor"}) if(!values.ContainsKey(key) || !Regex.IsMatch(values[key],"^[0-9a-fA-F]{8}$")) return key+" 需要填写 8 位颜色代码，格式为 RRGGBBAA。";
            return null;
        }
    }

    public sealed class SettingsStore
    {
        public readonly string PathName, BackupDirectory;
        public string Text { get; private set; }
        public Encoding Encoding { get; private set; }
        private byte[] baseline;
        private bool hasBom;
        private static readonly Regex KeyLine = new Regex(@"^(\s*)([A-Za-z][A-Za-z0-9_]*)(\s*=\s*)(.*)$");
        public SettingsStore(string path, string backups) {PathName=path; BackupDirectory=backups; Reload();}
        public void Reload()
        {
            baseline=File.ReadAllBytes(PathName);
            if(baseline.Length>=2 && baseline[0]==255 && baseline[1]==254) { Encoding=new UnicodeEncoding(false,true,true); hasBom=true; }
            else if(baseline.Length>=2 && baseline[0]==254 && baseline[1]==255) { Encoding=new UnicodeEncoding(true,true,true); hasBom=true; }
            else if(baseline.Length>=3 && baseline[0]==239 && baseline[1]==187 && baseline[2]==191) { Encoding=new UTF8Encoding(true,true); hasBom=true; }
            else { Encoding=new UTF8Encoding(false,true); hasBom=false; }
            int skip=hasBom?Encoding.GetPreamble().Length:0;
            Text=Encoding.GetString(baseline,skip,baseline.Length-skip);
        }
        public Dictionary<string,string> ReadValues()
        {
            Dictionary<string,string> result=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            bool inVariables=false;
            foreach(string line in Regex.Split(Text,"\r\n|\n|\r"))
            {
                string t=line.Trim();
                if(t.StartsWith("[") && t.EndsWith("]")) {inVariables=t.Equals("[Variables]",StringComparison.OrdinalIgnoreCase);continue;}
                if(!inVariables) continue;
                Match m=KeyLine.Match(line); if(m.Success)result[m.Groups[2].Value]=m.Groups[4].Value.Trim();
            }
            return result;
        }
        public string Merge(Dictionary<string,string> edits)
        {
            string newline=Text.Contains("\r\n")?"\r\n":"\n";
            List<string> lines=new List<string>(Regex.Split(Text,"\r\n|\n|\r"));
            HashSet<string> found=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool inVariables=false, hasSection=false; int insertion=-1;
            for(int i=0;i<lines.Count;i++)
            {
                string trimmed=lines[i].Trim();
                if(trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    if(inVariables && insertion<0)insertion=i;
                    inVariables=trimmed.Equals("[Variables]",StringComparison.OrdinalIgnoreCase); hasSection|=inVariables;continue;
                }
                if(!inVariables)continue;
                Match m=KeyLine.Match(lines[i]); string value;
                if(m.Success && edits.TryGetValue(m.Groups[2].Value,out value))
                { lines[i]=m.Groups[1].Value+m.Groups[2].Value+m.Groups[3].Value+value; found.Add(m.Groups[2].Value); }
            }
            if(!hasSection)throw new InvalidDataException("配置文件缺少 [Variables]，请从备份恢复后再打开。");
            if(insertion<0)insertion=lines.Count;
            foreach(KeyValuePair<string,string> edit in edits) if(!found.Contains(edit.Key))lines.Insert(insertion++,edit.Key+"="+edit.Value);
            return String.Join(newline,lines);
        }
        public string Save(Dictionary<string,string> edits)
        {
            if(!Equal(baseline,File.ReadAllBytes(PathName)))throw new IOException("其他程序修改了配置文件。请先点击「重新读取」，再保存。");
            string next=Merge(edits); if(next==Text)return null;
            byte[] body=Encoding.GetBytes(next), preamble=hasBom?Encoding.GetPreamble():new byte[0];
            byte[] output=new byte[preamble.Length+body.Length];Buffer.BlockCopy(preamble,0,output,0,preamble.Length);Buffer.BlockCopy(body,0,output,preamble.Length,body.Length);
            Directory.CreateDirectory(BackupDirectory);
            string backup=System.IO.Path.Combine(BackupDirectory,"PhotoFrame_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+"_"+Guid.NewGuid().ToString("N").Substring(0,6)+".inc");
            string temporary=PathName+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                using(FileStream stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {stream.Write(output,0,output.Length);stream.Flush(true);}
                File.Replace(temporary,PathName,backup,true);
            }
            finally {if(File.Exists(temporary))File.Delete(temporary);}
            Reload();return backup;
        }
        private static bool Equal(byte[] a,byte[] b)
        {if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
    }
}
