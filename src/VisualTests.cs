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
    internal static class VisualTests
    {
        private static readonly List<string> results=new List<string>();
        private static Control Find(Control form,string name){return form.Controls.Find(name,true)[0];}
        private static void Wait(int milliseconds){DateTime end=DateTime.UtcNow.AddMilliseconds(milliseconds);while(DateTime.UtcNow<end){Application.DoEvents();Thread.Sleep(12);}}
        private static void Check(string name,bool passed){results.Add((passed?"PASS ":"FAIL ")+name);Console.WriteLine(results[results.Count-1]);if(!passed)throw new Exception(name);}
        private static Bitmap Capture(Control control){Bitmap bitmap=new Bitmap(control.Width,control.Height);control.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));return bitmap;}
        private static Color Sample(Control control,int x,int y){using(Bitmap image=Capture(control))return image.GetPixel(x,y);}
        private static int MarkerTop(Control rail)
        {
            using(Bitmap image=Capture(rail))
                for(int y=0;y<image.Height;y++)if(image.GetPixel(12,y).ToArgb()==Color.FromArgb(213,228,213).ToArgb())return y;
            return -1;
        }
        private static void MouseEvent(Control control,string method)
        {control.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(control,new object[]{EventArgs.Empty});}
        private static void CheckNavigationMotion(SettingsForm form,string output)
        {
            Control rail=Find(form,"NavigationRail");Button first=(Button)Find(form,"Navigation0"),last=(Button)Find(form,"Navigation4");
            for(int i=0;i<5;i++)MouseEvent(Find(form,"Navigation"+i),"OnMouseLeave");
            first.PerformClick();Wait(280);int start=MarkerTop(rail),offset=start-first.Top;
            last.PerformClick();int immediate=MarkerTop(rail);
            Check("navigation_click_starts_at_previous_position",start>=0 && Math.Abs(immediate-start)<3);
            Wait(75);int middle=MarkerTop(rail);
            Check("navigation_selection_moves_between_items",middle>start+3 && middle<last.Top-3);
            using(Bitmap bitmap=Capture(rail))bitmap.Save(Path.Combine(output,"navigation-midpoint.png"));
            first.PerformClick();int reverse=MarkerTop(rail);
            Check("navigation_reverse_keeps_current_position",reverse>start+3 && Math.Abs(reverse-middle)<40);
            Wait(70);int returning=MarkerTop(rail);
            Check("navigation_reverse_moves_back",returning<reverse);
            last.PerformClick();((Button)Find(form,"Navigation1")).PerformClick();Wait(280);
            Check("navigation_rapid_clicks_finish_at_latest_item",Math.Abs(MarkerTop(rail)-(Find(form,"Navigation1").Top+offset))<2 && ((TabControl)Find(form,"SettingsTabs")).SelectedIndex==1);
            Control hover=Find(form,"Navigation3");MouseEvent(hover,"OnMouseLeave");Wait(200);
            Color normal=Sample(hover,12,hover.Height/2);MouseEvent(hover,"OnMouseEnter");Wait(55);Color half=Sample(hover,12,hover.Height/2);Wait(130);Color full=Sample(hover,12,hover.Height/2);
            Check("navigation_hover_fades_in",normal.ToArgb()!=half.ToArgb() && half.ToArgb()!=full.ToArgb());
            MouseEvent(hover,"OnMouseLeave");Wait(220);
            Check("navigation_hover_fades_out",normal.ToArgb()==Sample(hover,12,hover.Height/2).ToArgb());
        }
        private static void Move(Control control,int x,int y,int wait)
        {
            // Raise the same managed events without moving the user's mouse or
            // registering native tracking against the real cursor position.
            Control target=control is Preview?control:Find(control,"FramePreview");
            string method=control is Preview?"OnMouseMove":"OnMouseLeave";
            object argument=control is Preview?(object)new MouseEventArgs(MouseButtons.None,0,x,y,0):EventArgs.Empty;
            typeof(Control).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,new object[]{argument});
            Wait(wait);
        }
        [STAThread] private static int Main(string[] args)
        {
            string root=args[0],output=args[1],fixture=Path.Combine(Path.GetTempPath(),"AlbumVisual_"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(fixture,"Config"));Directory.CreateDirectory(output);
            try
            {
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                string settings=Path.Combine(fixture,"Config","PhotoFrame.inc");File.Copy(Path.Combine(root,"Config","PhotoFrame.inc"),settings);
                using(SettingsForm form=new SettingsForm(fixture,false))
                {
                    form.Show();Wait(180);TabControl tabs=(TabControl)Find(form,"SettingsTabs");
                    for(int i=0;i<5;i++)
                    {
                        ((Button)Find(form,"Navigation"+i)).PerformClick();Wait(280);Check("navigation_page_"+i,tabs.SelectedIndex==i);
                        using(Bitmap bmp=Capture(form))bmp.Save(Path.Combine(output,"page-"+(i+1)+".png"));
                    }
                    CheckNavigationMotion(form,output);
                    ((Button)Find(form,"Navigation2")).PerformClick();Wait(80);
                    foreach(Size size in new Size[]{new Size(1040,660),new Size(1280,820)})
                    {
                        form.ClientSize=size;Wait(100);Control save=Find(form,"SaveApply");Rectangle bounds=new Rectangle(form.PointToClient(save.PointToScreen(Point.Empty)),save.Size);
                        Check("footer_visible_at_"+size.Width,form.ClientRectangle.Contains(bounds));using(Bitmap bmp=Capture(form))bmp.Save(Path.Combine(output,"window-"+size.Width+".png"));
                    }
                    form.ClientSize=new Size(1120,730);Wait(80);
                    Preview preview=(Preview)Find(form,"FramePreview");form.Activate();form.BringToFront();Wait(100);
                    ((NumericUpDown)Find(form,"CornerRadius")).Value=24;Check("numeric_change_updates_preview",preview.Radius==24);
                    Move(form,30,60,300);Color normal=Sample(preview,preview.Width/2,40);
                    Move(preview,preview.Width/2,40,350);Color faded=Sample(preview,preview.Width/2,40);
                    Check("preview_hover_fades_photo",normal.ToArgb()!=faded.ToArgb());
                    Color before=Sample(preview,preview.Width/2,132);Move(preview,preview.Width/2,137,400);Color after=Sample(preview,preview.Width/2,132);Check("preview_bottom_reveals_toolbar",before.ToArgb()!=after.ToArgb());
                    Move(form,30,60,350);Color restored=Sample(preview,preview.Width/2,40);Check("preview_leave_restores_photo",normal.ToArgb()==restored.ToArgb());
                    ((CheckBox)Find(form,"HoverEnabled")).Checked=false;Move(preview,preview.Width/2,40,350);Check("preview_respects_fade_switch",normal.ToArgb()==Sample(preview,preview.Width/2,40).ToArgb());
                    // Drop only this temporary form's unsaved demonstration values.
                    typeof(SettingsForm).GetField("dirty",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(form,false);
                    ((CheckBox)Find(form,"TrayOnClose")).Checked=false;form.Close();
                }
                return 0;
            }
            catch(Exception ex){results.Add("ERROR "+ex);Console.WriteLine(ex);return 1;}
            finally{File.WriteAllLines(Path.Combine(output,"visual-tests.txt"),results.ToArray(),new UTF8Encoding(true));if(Directory.Exists(fixture))Directory.Delete(fixture,true);}
        }
    }
}
