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
    internal static class LiveTests
    {
        static List<string> results=new List<string>();
        static void Check(string name,bool passed){results.Add((passed?"PASS ":"FAIL ")+name);Console.WriteLine(results[results.Count-1]);if(!passed)throw new Exception(name);}
        static Control Find(Control form,string name){return form.Controls.Find(name,true)[0];}
        static void Wait(int milliseconds){DateTime end=DateTime.UtcNow.AddMilliseconds(milliseconds);while(DateTime.UtcNow<end){Application.DoEvents();Thread.Sleep(15);}}
        [STAThread] static int Main(string[] args)
        {
            string root=args[0],output=args[1],settings=Path.Combine(root,"Config","PhotoFrame.inc");
            byte[] original=File.ReadAllBytes(settings);FrameController frame=new FrameController(root);
            string trayPreference=Path.Combine(root,"Config","SettingsCenter.ini");
            byte[] originalTray=File.Exists(trayPreference)?File.ReadAllBytes(trayPreference):null;
            try
            {
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                Check("detects_installed_frame_only",frame.Running && frame.FindWindow(true)!=IntPtr.Zero);
                string photos=Path.Combine(output,"照片库 中文 空格");Directory.CreateDirectory(photos);
                using(Bitmap bmp=new Bitmap(480,320)){using(Graphics g=Graphics.FromImage(bmp)){g.Clear(Color.SteelBlue);}bmp.Save(Path.Combine(photos,"test-photo.png"));}
                using(SettingsForm form=new SettingsForm(root,false))
                {
                    form.Show();Wait(100);
                    using(Bitmap bmp=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bmp,new Rectangle(Point.Empty,bmp.Size));bmp.Save(Path.Combine(output,"live-settings.png"));}
                    ((TextBox)Find(form,"PhotosPath")).Text=photos;
                    ((NumericUpDown)Find(form,"ChangeSeconds")).Value=31;
                    ((CheckBox)Find(form,"HoverEnterDurationInherit")).Checked=false;
                    ((NumericUpDown)Find(form,"HoverEnterDuration")).Value=310;
                    ((NumericUpDown)Find(form,"ControlHideDuration")).Value=210;
                    ((Button)Find(form,"SaveApply")).PerformClick();Wait(700);
                    frame.Send("[!CommandMeasure Layout \"SKIN:Bang('!Log','CENTER_LIVE|'..SKIN:GetVariable('PhotosPath')..'|'..SKIN:GetVariable('ChangeSeconds')..'|'..SKIN:GetVariable('HoverEnterDuration')..'|'..SKIN:GetVariable('ControlHideDuration'),'Notice')\"]");Wait(150);
                    string log=File.ReadAllText(Path.Combine(root,"Config","Rainmeter.log"));
                    Check("gui_save_apply_reaches_live_frame",log.Contains("CENTER_LIVE|"+photos+"|31|310|210"));
                    Check("live_library_change_loads_fixture",log.Contains("PhotoFrame: "+Path.Combine(photos,"test-photo.png")));
                    ((Button)Find(form,"StopFrame")).PerformClick();Wait(900);Check("stop_closes_frame",!frame.Running);
                    ((Button)Find(form,"StartFrame")).PerformClick();Wait(700);Check("start_reactivates_existing_rainmeter",frame.Running);
                    ((Button)Find(form,"StopFrame")).PerformClick();Wait(900);
                    frame.Send("[!Quit]");Wait(900);Check("engine_shutdown_is_detected",frame.FindWindow(true)==IntPtr.Zero);
                    ((Button)Find(form,"StartFrame")).PerformClick();Wait(1300);Check("cold_start_uses_portable_config",frame.Running);
                    ((CheckBox)Find(form,"TrayOnClose")).Checked=false;form.Close();Wait(80);
                }
                Console.WriteLine("ALL PASSED: "+results.Count);return 0;
            }
            catch(Exception ex){Console.WriteLine(ex);results.Add("ERROR "+ex);return 1;}
            finally
            {
                File.WriteAllBytes(settings,original);
                if(originalTray!=null)File.WriteAllBytes(trayPreference,originalTray);else if(File.Exists(trayPreference))File.Delete(trayPreference);
                Wait(900);frame.Apply();Wait(700);
                File.WriteAllLines(Path.Combine(output,"live-tests.txt"),results.ToArray(),new UTF8Encoding(true));
            }
        }
    }
}
