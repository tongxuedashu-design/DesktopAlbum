using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PhotoFrameSettings
{
    internal static class PortableTests
    {
        private static readonly List<string> report=new List<string>();
        private static void Check(string name,bool condition)
        {report.Add((condition?"PASS ":"FAIL ")+name);Console.WriteLine(report[report.Count-1]);if(!condition)throw new Exception(name);}
        private static string Photos(string root)
        {return new SettingsStore(AppPaths.Settings(root),Path.Combine(root,"backup")).ReadValues()["PhotosPath"];}
        private static int Main(string[] args)
        {
            string temporary=Path.Combine(Path.GetTempPath(),"AlbumPortable_"+Guid.NewGuid().ToString("N"));
            string original=Path.Combine(temporary,"原位置 中文 & 空格"),moved=Path.Combine(temporary,"新位置 空格"),again=Path.Combine(temporary,"再次移动");
            try
            {
                string config=Path.Combine(original,"Config");Directory.CreateDirectory(config);
                StringBuilder template=new StringBuilder("[Variables]\r\n");
                foreach(KeyValuePair<string,string> item in Schema.Defaults(original))template.Append(item.Key+"="+(item.Key=="PhotosPath"?"__PHOTO_LIBRARY__":item.Value)+"\r\n");
                File.WriteAllText(Path.Combine(config,"PhotoFrame.example.inc"),template.ToString(),Encoding.Unicode);
                File.WriteAllText(Path.Combine(config,"Rainmeter.example.ini"),"[Rainmeter]\r\nSkinPath=__SKIN_PATH__\r\n",Encoding.Unicode);
                File.WriteAllText(Path.Combine(config,"portable.json"),"{}");
                Check("find_root_accepts_uninitialized_template",Program.FindRoot(original)==original);
                PortableSetup.Ensure(original);
                Check("first_launch_initializes_library",Photos(original)==Path.Combine(original,"Photos") && Directory.Exists(Photos(original)));
                Check("first_launch_initializes_skin_path",File.ReadAllText(Path.Combine(config,"Rainmeter.ini")).Contains("SkinPath="+Path.Combine(config,"Skins")+Path.DirectorySeparatorChar));
                byte[] initial=File.ReadAllBytes(Path.Combine(config,"PhotoFrame.inc"));
                Check("generated_configuration_is_utf16",initial[0]==255 && initial[1]==254);
                Check("portable_backups_stay_inside_configuration",Program.BackupDirectory(original)==Path.Combine(config,"Backups","SettingsCenter"));
                Check("portable_guide_uses_product_documentation",Program.UserGuide(original)==Path.Combine(original,"Docs","USER_GUIDE.md"));
                PortableSetup.Ensure(original);
                Check("repeated_launch_does_not_rewrite_settings",Convert.ToBase64String(initial)==Convert.ToBase64String(File.ReadAllBytes(Path.Combine(config,"PhotoFrame.inc"))));
                string path=Path.Combine(config,"PhotoFrame.inc");
                File.WriteAllText(path,File.ReadAllText(path).Replace("SizeW=480","SizeW=672"),Encoding.Unicode);
                Directory.Move(original,moved);PortableSetup.Ensure(moved);
                Check("moving_folder_relocates_default_library",Photos(moved)==Path.Combine(moved,"Photos"));
                Check("moving_folder_preserves_other_settings",File.ReadAllText(Path.Combine(moved,"Config","PhotoFrame.inc")).Contains("SizeW=672"));
                string external=Path.Combine(temporary,"外置照片库");Directory.CreateDirectory(external);
                SettingsStore store=new SettingsStore(Path.Combine(moved,"Config","PhotoFrame.inc"),Path.Combine(temporary,"backups"));
                store.Save(new Dictionary<string,string>{{"PhotosPath",external}});
                Directory.Move(moved,again);PortableSetup.Ensure(again);
                Check("moving_folder_preserves_external_library",Photos(again)==external);
                Check("location_record_tracks_latest_folder",File.ReadAllText(Path.Combine(again,"Config","location.txt"))==again);
                string legacy=Path.Combine(temporary,"旧版相册");Directory.CreateDirectory(Path.Combine(legacy,"02_配置"));
                Directory.CreateDirectory(Path.Combine(legacy,"01_程序"));Directory.CreateDirectory(Path.Combine(legacy,"03_照片库"));
                File.Copy(Path.Combine(again,"Config","PhotoFrame.example.inc"),Path.Combine(legacy,"02_配置","相框设置.example.inc"));
                File.Copy(Path.Combine(again,"Config","Rainmeter.example.ini"),Path.Combine(legacy,"02_配置","Rainmeter.example.ini"));
                File.Copy(Path.Combine(legacy,"02_配置","相框设置.example.inc"),Path.Combine(legacy,"02_配置","相框设置.inc"));
                File.WriteAllText(Path.Combine(legacy,"02_配置","便携版.json"),"{}");PortableSetup.Ensure(legacy);
                Check("legacy_copy_uses_existing_configuration",Program.FindRoot(legacy)==legacy && File.Exists(Path.Combine(legacy,"02_配置","相框设置.inc")));
                Check("legacy_copy_uses_existing_engine",AppPaths.Engine(legacy)==Path.Combine(legacy,"01_程序"));
                Directory.Move(Path.Combine(legacy,"02_配置"),Path.Combine(legacy,"Config"));
                Directory.Move(Path.Combine(legacy,"01_程序"),Path.Combine(legacy,"Engine"));
                Directory.Move(Path.Combine(legacy,"03_照片库"),Path.Combine(legacy,"Photos"));PortableSetup.Ensure(legacy);
                Check("renamed_copy_relocates_builtin_photo_path",Photos(legacy)==Path.Combine(legacy,"Photos"));
                Check("renamed_copy_does_not_recreate_numbered_directories",!Directory.Exists(Path.Combine(legacy,"01_程序")) && !Directory.Exists(Path.Combine(legacy,"02_配置")) && !Directory.Exists(Path.Combine(legacy,"03_照片库")));
                Console.WriteLine("ALL PASSED: "+report.Count);return 0;
            }
            catch(Exception ex){Console.WriteLine(ex);report.Add(ex.ToString());return 1;}
            finally
            {
                Directory.CreateDirectory(args[1]);File.WriteAllLines(Path.Combine(args[1],"portable-tests.txt"),report.ToArray(),new UTF8Encoding(true));
                if(Directory.Exists(temporary))Directory.Delete(temporary,true);
            }
        }
    }
}
