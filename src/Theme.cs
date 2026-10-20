using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PhotoFrameSettings
{
    internal static class Theme
    {
        internal static readonly Color Canvas=Color.FromArgb(246,247,243), Surface=Color.White,
            Sidebar=Color.FromArgb(235,239,232), Ink=Color.FromArgb(36,51,44), Muted=Color.FromArgb(112,124,116),
            Accent=Color.FromArgb(48,105,83), AccentSoft=Color.FromArgb(222,235,224), Line=Color.FromArgb(226,232,224);
        internal static GraphicsPath Round(RectangleF bounds,float radius)
        {
            GraphicsPath path=new GraphicsPath();float d=Math.Min(radius*2,Math.Min(bounds.Width,bounds.Height));
            if(d<=0){path.AddRectangle(bounds);return path;}
            path.AddArc(bounds.X,bounds.Y,d,d,180,90);path.AddArc(bounds.Right-d,bounds.Y,d,d,270,90);
            path.AddArc(bounds.Right-d,bounds.Bottom-d,d,d,0,90);path.AddArc(bounds.X,bounds.Bottom-d,d,d,90,90);path.CloseFigure();return path;
        }
        internal static void FillRound(Graphics g,Color color,RectangleF rect,float radius)
        {using(GraphicsPath path=Round(rect,radius))using(Brush brush=new SolidBrush(color))g.FillPath(brush,path);}
        internal static void StrokeRound(Graphics g,Color color,RectangleF rect,float radius)
        {using(GraphicsPath path=Round(rect,radius))using(Pen pen=new Pen(color))g.DrawPath(pen,path);}
        internal static void Icon(Graphics g,string name,Rectangle rect,Color color)
        {
            GraphicsState state=g.Save();g.TranslateTransform(rect.X,rect.Y);g.ScaleTransform(rect.Width/24F,rect.Height/24F);
            using(Pen pen=new Pen(color,1.6F)){pen.StartCap=pen.EndCap=LineCap.Round;pen.LineJoin=LineJoin.Round;
                if(name=="photos" || name=="brand")
                {using(GraphicsPath path=Round(new RectangleF(3,4,18,16),3))g.DrawPath(pen,path);g.DrawEllipse(pen,6.5F,7.5F,3,3);g.DrawLines(pen,new PointF[]{new PointF(4,17),new PointF(10,11),new PointF(14,15),new PointF(17,12),new PointF(21,16)});}
                else if(name=="appearance")
                {g.DrawEllipse(pen,3,3,18,18);g.DrawEllipse(pen,7,6,1.5F,1.5F);g.DrawEllipse(pen,13,6,1.5F,1.5F);g.DrawEllipse(pen,6,12,1.5F,1.5F);g.DrawArc(pen,11,10,7,7,20,240);}
                else if(name=="hover")
                {g.DrawLines(pen,new PointF[]{new PointF(5,3),new PointF(5,19),new PointF(9,15),new PointF(12,21),new PointF(15,19),new PointF(12,13),new PointF(18,13),new PointF(5,3)});g.DrawArc(pen,13,2,8,8,260,100);}
                else if(name=="controls")
                {g.DrawLine(pen,4,6,20,6);g.DrawLine(pen,4,12,20,12);g.DrawLine(pen,4,18,20,18);foreach(Point point in new Point[]{new Point(8,6),new Point(16,12),new Point(10,18)}){using(Brush b=new SolidBrush(Theme.Surface))g.FillEllipse(b,point.X-2,point.Y-2,4,4);g.DrawEllipse(pen,point.X-2,point.Y-2,4,4);}}
                else if(name=="tools")
                {g.DrawLines(pen,new PointF[]{new PointF(9,4),new PointF(9,2),new PointF(15,2),new PointF(15,4)});using(GraphicsPath path=Round(new RectangleF(3,5,18,15),3))g.DrawPath(pen,path);g.DrawLine(pen,3,11,21,11);g.DrawLine(pen,10,10,14,10);g.DrawLine(pen,12,10,12,13);}
                else if(name=="check")g.DrawLines(pen,new Point[]{new Point(5,12),new Point(10,17),new Point(19,7)});
                else if(name=="arrow")g.DrawLines(pen,new Point[]{new Point(8,5),new Point(15,12),new Point(8,19)});
                else if(name=="folder")
                {g.DrawLines(pen,new Point[]{new Point(3,8),new Point(3,5),new Point(10,5),new Point(13,8),new Point(21,8),new Point(21,19),new Point(3,19),new Point(3,8),new Point(21,8)});}
            }g.Restore(state);
        }
        internal static Icon CreateIcon()
        {
            using(Bitmap bmp=new Bitmap(64,64))using(Graphics g=Graphics.FromImage(bmp))
            {
                g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.Transparent);
                FillRound(g,Accent,new RectangleF(1,1,62,62),15);Icon(g,"brand",new Rectangle(10,10,44,44),Color.White);
                IntPtr handle=bmp.GetHicon();try{return (Icon)System.Drawing.Icon.FromHandle(handle).Clone();}finally{DestroyIcon(handle);}
            }
        }
        [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr icon);
    }

    internal sealed class SoftButton : Button
    {
        internal bool Primary {get;set;}
        private bool hovered,pressed;
        internal SoftButton()
        {SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;AutoSize=false;Height=38;Margin=new Padding(0,0,10,0);Font=new Font("Microsoft YaHei UI",9F);}
        protected override void OnMouseEnter(EventArgs e){hovered=true;Invalidate();base.OnMouseEnter(e);}
        protected override void OnMouseLeave(EventArgs e){hovered=pressed=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnMouseDown(MouseEventArgs e){pressed=true;Invalidate();base.OnMouseDown(e);}
        protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
        protected override void OnGotFocus(EventArgs e){Invalidate();base.OnGotFocus(e);}
        protected override void OnLostFocus(EventArgs e){Invalidate();base.OnLostFocus(e);}
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;e.Graphics.Clear(Parent==null?Theme.Canvas:Parent.BackColor);
            Color fill=Primary?(pressed?Color.FromArgb(34,79,62):hovered?Color.FromArgb(60,122,96):Theme.Accent):(pressed?Theme.AccentSoft:hovered?Color.FromArgb(242,246,240):Color.White);
            RectangleF rect=new RectangleF(0.5F,0.5F,Width-1,Height-1);Theme.FillRound(e.Graphics,fill,rect,8);
            Theme.StrokeRound(e.Graphics,Focused?Theme.Accent:Primary?fill:Theme.Line,rect,8);
            TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,Enabled?(Primary?Color.White:Theme.Ink):Theme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
        }
    }
    internal sealed class Motion
    {
        internal float Value {get;private set;}
        internal float Target {get;private set;}
        internal bool Active {get;private set;}
        private float start;private long began;private int duration;
        internal void Snap(float value){Value=Target=value;Active=false;}
        internal void To(float value,int milliseconds)
        {
            // Retarget from the last displayed frame. Tab layout can delay the
            // UI thread; advancing here would jump over frames never shown.
            if(value==Target)return;
            start=Value;Target=value;began=Stopwatch.GetTimestamp();duration=milliseconds;
            Active=duration>0 && Math.Abs(Target-start)>.001F;
            if(!Active)Value=Target;
        }
        internal void Step()
        {
            if(!Active)return;
            double progress=Math.Min(1,(Stopwatch.GetTimestamp()-began)*1000.0/Stopwatch.Frequency/duration);
            Value=start+(Target-start)*(float)(progress*progress*(3-2*progress));
            if(progress>=1){Value=Target;Active=false;}
        }
    }
    internal sealed class NavRail : FlowLayoutPanel
    {
        private readonly Timer animation=new Timer();private readonly Motion position=new Motion();
        private NavButton selected;private bool placed;
        internal NavRail()
        {
            DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true);
            BackColor=Theme.Sidebar;FlowDirection=FlowDirection.TopDown;WrapContents=false;
            animation.Interval=16;animation.Tick+=delegate{position.Step();Invalidate(true);if(!position.Active)animation.Stop();};
        }
        internal void SelectItem(NavButton button)
        {
            if(selected==button && placed && position.Target==button.Top)return;
            selected=button;
            if(!placed || !IsHandleCreated){position.Snap(button.Top);placed=true;}
            else position.To(button.Top,210);
            if(position.Active)animation.Start();else animation.Stop();
            Invalidate(true);
        }
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if(selected!=null && (!placed || position.Target!=selected.Top))
            {position.Snap(selected.Top);placed=true;animation.Stop();Invalidate(true);}
        }
        internal void PaintSelection(Graphics graphics,Point origin)
        {
            if(!placed || selected==null)return;
            RectangleF rect=new RectangleF(selected.Left-origin.X,position.Value-origin.Y,selected.Width-1,selected.Height-1);
            graphics.SmoothingMode=SmoothingMode.AntiAlias;
            Theme.FillRound(graphics,Color.FromArgb(213,228,213),rect,9);
        }
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);PaintSelection(e.Graphics,Point.Empty);}
        protected override void Dispose(bool disposing){if(disposing)animation.Dispose();base.Dispose(disposing);}
    }
    internal sealed class NavButton : Button
    {
        private readonly Timer animation=new Timer();private readonly Motion hover=new Motion();
        private bool selected,hovered;
        internal bool Selected
        {
            get{return selected;}
            set{if(selected==value)return;selected=value;AnimateHover();Invalidate();}
        }
        internal string Glyph;
        internal NavButton()
        {
            SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);
            Cursor=Cursors.Hand;FlatStyle=FlatStyle.Flat;TabStop=true;
            animation.Interval=16;animation.Tick+=delegate{hover.Step();Invalidate();if(!hover.Active)animation.Stop();};
        }
        private void AnimateHover()
        {hover.To(hovered && !Selected?1:0,hovered?130:170);if(hover.Active)animation.Start();else animation.Stop();Invalidate();}
        protected override void OnMouseEnter(EventArgs e){hovered=true;AnimateHover();base.OnMouseEnter(e);}
        protected override void OnMouseLeave(EventArgs e){hovered=false;AnimateHover();base.OnMouseLeave(e);}
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;e.Graphics.Clear(Theme.Sidebar);
            NavRail rail=Parent as NavRail;
            if(rail!=null)rail.PaintSelection(e.Graphics,Location);
            else if(Selected)Theme.FillRound(e.Graphics,Color.FromArgb(213,228,213),new RectangleF(0,0,Width-1,Height-1),9);
            if(hover.Value>0)Theme.FillRound(e.Graphics,Color.FromArgb((int)(255*hover.Value),226,233,223),new RectangleF(0,0,Width-1,Height-1),9);
            Color color=Selected?Theme.Accent:Theme.Muted;Theme.Icon(e.Graphics,Glyph,new Rectangle(15,(Height-21)/2,21,21),color);
            using(Font f=new Font(Font.FontFamily,9.5F,Selected?FontStyle.Bold:FontStyle.Regular))TextRenderer.DrawText(e.Graphics,Text,f,new Rectangle(47,0,Width-56,Height),Selected?Theme.Ink:Color.FromArgb(90,104,93),TextFormatFlags.VerticalCenter|TextFormatFlags.Left);
            if(Focused && ShowFocusCues)Theme.StrokeRound(e.Graphics,Theme.Accent,new RectangleF(4.5F,4.5F,Width-10,Height-10),7);
        }
        protected override void Dispose(bool disposing){if(disposing)animation.Dispose();base.Dispose(disposing);}
    }
    internal sealed class Toggle : CheckBox
    {
        private readonly Timer animation=new Timer();private float progress,target;
        internal Toggle(){AutoSize=false;Height=28;Cursor=Cursors.Hand;SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);animation.Interval=16;animation.Tick+=delegate{progress+=(target-progress)*0.35F;if(Math.Abs(progress-target)<.02F){progress=target;animation.Stop();}Invalidate();};}
        protected override void OnCheckedChanged(EventArgs e){target=Checked?1:0;if(IsHandleCreated)animation.Start();else progress=target;base.OnCheckedChanged(e);}
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;e.Graphics.Clear(Parent==null?Color.White:Parent.BackColor);
            int y=(Height-20)/2;Theme.FillRound(e.Graphics,Checked?Theme.Accent:Color.FromArgb(194,204,193),new RectangleF(0,y,36,20),10);
            using(Brush brush=new SolidBrush(Color.White))e.Graphics.FillEllipse(brush,3+16*progress,y+3,14,14);
            TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(47,0,Math.Max(1,Width-47),Height),Enabled?Theme.Ink:Theme.Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
            if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(0,0,Width-1,Height-1));
        }
        protected override void Dispose(bool disposing){if(disposing)animation.Dispose();base.Dispose(disposing);}
    }
    internal sealed class InputShell : Panel
    {
        internal InputShell(Control input,int width)
        {
            Width=width;Height=36;BackColor=Color.White;Padding=new Padding(10,7,10,5);Margin=new Padding(0,0,8,0);
            DoubleBuffered=true;input.Dock=DockStyle.Fill;Controls.Add(input);input.GotFocus+=delegate{Invalidate();};input.LostFocus+=delegate{Invalidate();};
        }
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;Theme.StrokeRound(e.Graphics,ContainsFocus?Theme.Accent:Theme.Line,new RectangleF(.5F,.5F,Width-1,Height-1),7);}
    }
    internal sealed class Card : Panel
    {
        internal Card(){BackColor=Color.White;Padding=new Padding(22,14,22,14);Margin=new Padding(0,0,0,14);DoubleBuffered=true;}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;Theme.StrokeRound(e.Graphics,Theme.Line,new RectangleF(.5F,.5F,Width-1,Height-1),12);}
    }
    internal sealed class PageHost : TabControl
    {
        internal PageHost(){Appearance=TabAppearance.FlatButtons;ItemSize=new Size(1,1);SizeMode=TabSizeMode.Fixed;Multiline=true;Padding=Point.Empty;}
        public override Rectangle DisplayRectangle {get{return new Rectangle(0,0,Width,Height);}}
        protected override void WndProc(ref Message m){if(m.Msg==0x1328){m.Result=new IntPtr(1);return;}base.WndProc(ref m);}
    }
    internal sealed class BrandMark : Control
    {
        internal BrandMark(){Size=new Size(39,39);DoubleBuffered=true;}
        protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;Theme.FillRound(e.Graphics,Theme.Accent,new RectangleF(0,0,Width,Height),10);Theme.Icon(e.Graphics,"brand",new Rectangle(6,6,Width-12,Height-12),Color.White);}
    }
    internal sealed class Preview : Control
    {
        internal decimal Radius=16,Opacity=90,MaximumW=480,MaximumH=480,Trigger=30,Distance=54;
        internal decimal EnterDuration=180,LeaveDuration=180,ShowDuration=240,HideDuration=200;
        internal bool Fade=true,ControlsEnabled=true;
        private sealed class Tween
        {
            internal float Value,Target;private float start;private long began;private double duration;
            internal void To(float target,decimal milliseconds){if(target==Target)return;start=Value;Target=target;began=Stopwatch.GetTimestamp();duration=(double)milliseconds*Math.Abs(target-start);if(duration<=0)Value=target;}
            internal void Step(){if(Value==Target)return;double p=Math.Min(1,(Stopwatch.GetTimestamp()-began)*1000.0/Stopwatch.Frequency/Math.Max(1,duration));Value=start+(Target-start)*(float)(p*p*(3-2*p));}
        }
        private readonly Timer timer=new Timer();private readonly Tween fade=new Tween(),bar=new Tween();
        internal Preview()
        {
            DoubleBuffered=true;Height=178;BackColor=Color.FromArgb(235,239,231);Cursor=Cursors.Default;
            timer.Interval=16;timer.Tick+=delegate{fade.Step();bar.Step();if(fade.Value==fade.Target && bar.Value==bar.Target)timer.Stop();Invalidate();};
            MouseMove+=delegate(object sender,MouseEventArgs e){RectangleF photo=PhotoBounds();bool inside=photo.Contains(e.Location);fade.To(inside?1:0,inside?EnterDuration:LeaveDuration);bool bottom=inside && e.Y>=photo.Bottom-photo.Height*(float)Trigger/100;bar.To(bottom?1:0,bottom?ShowDuration:HideDuration);timer.Start();};
            MouseLeave+=delegate{fade.To(0,LeaveDuration);bar.To(0,HideDuration);timer.Start();};AccessibleName="相框预览，鼠标移入查看透明度，移到底部查看控制栏";
        }
        private RectangleF PhotoBounds()
        {
            float ratio=(float)(MaximumW/Math.Max(1,MaximumH));ratio=Math.Max(.6F,Math.Min(1.8F,ratio));float h=130,w=h*ratio;if(w>Width-65){w=Width-65;h=w/ratio;}
            return new RectangleF((Width-w)/2,(Height-h)/2,w,h);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(BackColor);
            RectangleF photo=PhotoBounds();float w=photo.Width,h=photo.Height,radius=Math.Min((float)Radius/4,18);
            Theme.FillRound(g,Color.FromArgb(19,40,52,34),new RectangleF(photo.X-4,photo.Y+4,w+8,h+4),radius+3);
            GraphicsState state=g.Save();using(GraphicsPath path=Theme.Round(photo,radius))g.SetClip(path);
            using(LinearGradientBrush sky=new LinearGradientBrush(photo,Color.FromArgb(220,224,197),Color.FromArgb(149,178,155),90))g.FillRectangle(sky,photo);
            using(Brush sun=new SolidBrush(Color.FromArgb(244,238,205)))g.FillEllipse(sun,photo.Right-w*.3F,photo.Y+h*.16F,22,22);
            using(Brush far=new SolidBrush(Color.FromArgb(119,155,133)))g.FillPolygon(far,new PointF[]{new PointF(photo.Left,photo.Bottom),new PointF(photo.Left,photo.Y+h*.68F),new PointF(photo.Left+w*.37F,photo.Y+h*.34F),new PointF(photo.Left+w*.7F,photo.Bottom)});
            using(Brush near=new SolidBrush(Color.FromArgb(60,111,91)))g.FillPolygon(near,new PointF[]{new PointF(photo.Left+w*.25F,photo.Bottom),new PointF(photo.Left+w*.72F,photo.Y+h*.4F),new PointF(photo.Right,photo.Y+h*.74F),new PointF(photo.Right,photo.Bottom)});
            if(Fade)using(Brush wash=new SolidBrush(Color.FromArgb((int)(fade.Value*(255-(float)Opacity)),BackColor)))g.FillRectangle(wash,photo);
            if(ControlsEnabled && bar.Value>0)
            {
                float transition=bar.Value;
                float y=photo.Bottom-28+(1-transition)*(float)Distance*.55F;
                Theme.FillRound(g,Color.FromArgb((int)(210*transition),35,57,46),new RectangleF(photo.X+w/2-45,y,90,21),10);
                using(Pen pen=new Pen(Color.FromArgb((int)(255*transition),Color.White),1.5F))
                {float x=photo.X+w/2;g.DrawLines(pen,new PointF[]{new PointF(x-25,y+6),new PointF(x-30,y+10),new PointF(x-25,y+14)});g.DrawLine(pen,x-7,y+6,x-7,y+14);g.DrawLine(pen,x-2,y+6,x-2,y+14);g.DrawLines(pen,new PointF[]{new PointF(x+17,y+6),new PointF(x+22,y+10),new PointF(x+17,y+14)});g.DrawRectangle(pen,x+31,y+7,6,6);}
            }
            g.Restore(state);Theme.StrokeRound(g,Color.FromArgb(60,255,255,255),photo,radius);
        }
        protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
    }
}
