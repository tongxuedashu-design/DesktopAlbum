using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace PhotoFrameSettings
{
    internal static class SettingsTests
    {
        static int count;
        static List<string> report=new List<string>();
        static void Check(string name,bool passed)
        {count++;report.Add((passed?"PASS ":"FAIL ")+name);Console.WriteLine(report[report.Count-1]);if(!passed)throw new Exception(name);}
        static T Field<T>(object obj,string name) {return (T)obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(obj);}
        static Control Find(Control form,string name) {Control[] controls=form.Controls.Find(name,true);if(controls.Length!=1)throw new Exception("Missing control "+name);return controls[0];}
        [STAThread] static int Main(string[] args)
        {
            string album=args[0],output=args[1],temporary=Path.Combine(Path.GetTempPath(),"相框设置验证_"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(output);Directory.CreateDirectory(Path.Combine(temporary,"Config"));
            try
            {
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                string config=Path.Combine(temporary,"Config","PhotoFrame.inc");
                string original=File.ReadAllText(Path.Combine(album,"Config","PhotoFrame.inc"),Encoding.Unicode);
                string fixtures=Path.Combine(temporary,"照片 空格路径");Directory.CreateDirectory(fixtures);
                Dictionary<string,string> defaults=Schema.Defaults(album);defaults["PhotosPath"]=fixtures;
                Check("default_schema_is_valid",Schema.Validate(defaults)==null);
                Dictionary<string,string> bad=new Dictionary<string,string>(defaults);bad["PhotosPath"]=fixtures+"不存在";Check("nonexistent_library_rejected",Schema.Validate(bad)!=null);
                bad["PhotosPath"]=fixtures+"\r\nSizeW=1";Check("multiline_path_rejected",Schema.Validate(bad)!=null);
                bad=new Dictionary<string,string>(defaults);bad["HoverOpacity"]="0";Check("invisible_frame_rejected",Schema.Validate(bad)!=null);
                bad=new Dictionary<string,string>(defaults);bad["HoverEnterDuration"]="-2";Check("invalid_inheritance_rejected",Schema.Validate(bad)!=null);
                bad=new Dictionary<string,string>(defaults);bad["BorderColor"]="FFF";Check("invalid_color_rejected",Schema.Validate(bad)!=null);
                bad=new Dictionary<string,string>(defaults);bad["ControlShowDuration"]="0";bad["ControlHideDuration"]="0";bad["ControlSlideDistance"]="0";Check("instant_animation_supported",Schema.Validate(bad)==null);
                string seed="; 中文注释\r\n[Variables]\r\nPhotosPath="+fixtures+"\r\nSizeW=480\r\nUnknownSetting=keep-me\r\n[Other]\r\nSizeW=999\r\n";
                File.WriteAllText(config,seed,Encoding.Unicode);
                string previousBytes=Convert.ToBase64String(File.ReadAllBytes(config));
                SettingsStore store=new SettingsStore(config,Path.Combine(temporary,"Backups"));
                Dictionary<string,string> edits=new Dictionary<string,string>();edits["SizeW"]="640";edits["HoverEnterDuration"]="300";
                string backup=store.Save(edits);byte[] raw=File.ReadAllBytes(config);
                Check("utf16_bom_preserved",raw[0]==255 && raw[1]==254);
                Check("comments_unknowns_other_sections_preserved",store.Text.Contains("; 中文注释\r\n") && store.Text.Contains("UnknownSetting=keep-me") && store.Text.Contains("[Other]\r\nSizeW=999"));
                Check("new_settings_added_to_variables",store.ReadValues()["HoverEnterDuration"]=="300" && store.ReadValues()["SizeW"]=="640");
                Check("backup_contains_exact_previous_bytes",backup!=null && Convert.ToBase64String(File.ReadAllBytes(backup))==previousBytes);
                Check("unchanged_save_does_not_create_backup",store.Save(edits)==null);
                File.AppendAllText(config,"; externally changed\r\n",Encoding.Unicode);
                bool conflict=false;try{store.Save(edits);}catch(IOException){conflict=true;}Check("external_change_blocks_overwrite",conflict && File.ReadAllText(config,Encoding.Unicode).Contains("externally changed"));
                foreach(bool bom in new bool[]{false,true})
                {File.WriteAllText(config,seed,new UTF8Encoding(bom));store=new SettingsStore(config,Path.Combine(temporary,"Backups"));store.Save(edits);raw=File.ReadAllBytes(config);Check("utf8_"+(bom?"bom":"no_bom")+"_preserved",(raw[0]==239)==bom && File.ReadAllText(config,Encoding.UTF8).Contains("中文注释"));}
                File.WriteAllText(config,original,Encoding.Unicode);store=new SettingsStore(config,Path.Combine(temporary,"Backups"));store.Save(defaults);
                using(SettingsForm form=new SettingsForm(temporary,false))
                {
                    form.Show();Application.DoEvents();
                    TabControl tabs=(TabControl)Find(form,"SettingsTabs");Check("all_five_pages_present",tabs.TabPages.Count==5);
                    for(int i=0;i<tabs.TabPages.Count;i++)
                    {
                        tabs.SelectedIndex=i;Application.DoEvents();
                        using(Bitmap bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));bitmap.Save(Path.Combine(output,"page-"+(i+1)+".png"));}
                    }
                    NumericUpDown width=(NumericUpDown)Find(form,"SizeW");width.Value=672;
                    TextBox path=(TextBox)Find(form,"PhotosPath");path.Text=fixtures;
                    CheckBox enter=(CheckBox)Find(form,"HoverEnterDurationInherit");enter.Checked=false;((NumericUpDown)Find(form,"HoverEnterDuration")).Value=350;
                    ((Button)Find(form,"SaveOnly")).PerformClick();Application.DoEvents();
                    SettingsStore saved=new SettingsStore(config,Path.Combine(temporary,"Backups"));Dictionary<string,string> actual=saved.ReadValues();
                    Check("gui_save_writes_actual_values",actual["SizeW"]=="672" && actual["HoverEnterDuration"]=="350" && actual["PhotosPath"]==fixtures);
                    Check("gui_save_creates_backup",Directory.GetFiles(Path.Combine(temporary,"artifacts","Backups","SettingsCenter")).Length>0);
                    Check("gui_save_clears_dirty_state",!Field<bool>(form,"dirty"));
                    CheckBox closeToTray=(CheckBox)Find(form,"TrayOnClose");closeToTray.Checked=true;
                    form.Close();Application.DoEvents();Check("close_to_tray_keeps_form_alive",!form.Visible && !form.IsDisposed);
                    form.Show();Application.DoEvents();closeToTray.Checked=false;form.Close();Application.DoEvents();Check("disable_tray_allows_exit",form.IsDisposed);
                }
                Check("tray_preference_persists",File.ReadAllText(Path.Combine(temporary,"Config","SettingsCenter.ini")).Trim()=="CloseToTray=0");
                Console.WriteLine("ALL PASSED: "+count);return 0;
            }
            catch(Exception ex){Console.WriteLine(ex);report.Add("ERROR "+ex);return 1;}
            finally {File.WriteAllLines(Path.Combine(output,"settings-tests.txt"),report.ToArray(),new UTF8Encoding(true));if(Directory.Exists(temporary))Directory.Delete(temporary,true);}
        }
    }
}
