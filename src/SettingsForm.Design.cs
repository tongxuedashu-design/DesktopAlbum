using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace PhotoFrameSettings
{
    public sealed partial class SettingsForm
    {
        private readonly List<NavButton> navigation=new List<NavButton>();
        private NavRail navigationRail;
        private Label pageTitle,pageDescription,runningLabel,previewDetail;
        private Preview preview;
        private readonly string[] pageNames={"照片与轮播","尺寸与外观","悬停透明度","底部控制栏","工具与备份"};
        private readonly string[] descriptions={"选择照片文件夹，设置换图间隔。","调整相框大小、圆角和边框。","设置鼠标移入后的透明度和过渡速度。","设置控制栏的出现位置和动画。","设置托盘行为，查看配置和备份。"};
        private static Label LabelText(string text,int width)
        {return new Label{Text=text,AutoSize=true,MaximumSize=new Size(width,0),Margin=Padding.Empty,ForeColor=Theme.Muted,BackColor=Color.Transparent};}
        private Button ButtonText(string text,EventHandler click,string name)
        {
            SoftButton button=new SoftButton{Text=text,Name=name,Width=Math.Max(96,TextRenderer.MeasureText(text,Font).Width+32),BackColor=Color.White};button.Click+=click;return button;
        }
        private void Build()
        {
            TableLayoutPanel shell=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty,Padding=Padding.Empty,BackColor=Theme.Canvas};
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,204));shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            shell.Controls.Add(BuildSidebar(),0,0);
            TableLayoutPanel main=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(28,0,28,0),BackColor=Theme.Canvas};
            main.RowStyles.Add(new RowStyle(SizeType.Absolute,139));main.RowStyles.Add(new RowStyle(SizeType.Percent,100));main.RowStyles.Add(new RowStyle(SizeType.Absolute,86));
            main.Controls.Add(BuildHeader(),0,0);
            TableLayoutPanel body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Margin=Padding.Empty,BackColor=Theme.Canvas};
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,18));body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,226));
            tabs=new PageHost{Dock=DockStyle.Fill,Name="SettingsTabs",Margin=Padding.Empty};
            body.Controls.Add(tabs,0,0);body.Controls.Add(BuildPreview(),2,0);main.Controls.Add(body,0,1);
            BuildLibrary();BuildAppearance();BuildHover();BuildControls();BuildTools();
            main.Controls.Add(BuildFooter(),0,2);shell.Controls.Add(main,1,0);Controls.Add(shell);
            tabs.SelectedIndexChanged+=delegate{SelectPage(tabs.SelectedIndex);};SelectPage(0);
        }
        private Control BuildSidebar()
        {
            TableLayoutPanel sidebar=new TableLayoutPanel{Dock=DockStyle.Fill,BackColor=Theme.Sidebar,ColumnCount=1,RowCount=3,Padding=new Padding(17,30,17,22),Margin=Padding.Empty};
            sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute,116));sidebar.RowStyles.Add(new RowStyle(SizeType.Percent,100));sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute,70));
            Panel brand=new Panel{Dock=DockStyle.Fill,BackColor=Theme.Sidebar};BrandMark mark=new BrandMark{Location=new Point(7,0)};brand.Controls.Add(mark);
            brand.Controls.Add(new Label{Text="桌面相册",AutoSize=true,Location=new Point(5,53),Font=new Font(Font.FontFamily,16,FontStyle.Bold),ForeColor=Theme.Ink});
            sidebar.Controls.Add(brand,0,0);
            NavRail links=new NavRail{Dock=DockStyle.Fill,Name="NavigationRail"};navigationRail=links;
            string[] icons={"photos","appearance","hover","controls","tools"};
            for(int i=0;i<pageNames.Length;i++)
            {
                int index=i;NavButton button=new NavButton{Text=pageNames[i],Name="Navigation"+i,Glyph=icons[i],Size=new Size(170,46),Margin=new Padding(0,0,0,7),Font=Font};
                button.Click+=delegate{tabs.SelectedIndex=index;};navigation.Add(button);links.Controls.Add(button);
            }
            sidebar.Controls.Add(links,0,1);
            FlowLayoutPanel note=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(7,8,0,0)};
            note.Controls.Add(new Label{Text="桌面照片轮播",AutoSize=true,ForeColor=Color.FromArgb(94,114,98),Font=new Font(Font.FontFamily,9F),Margin=new Padding(0,0,0,8)});
            note.Controls.Add(new Label{Text="Windows 桌面工具",AutoSize=true,ForeColor=Theme.Muted,Font=new Font("Segoe UI",8F),Margin=Padding.Empty});sidebar.Controls.Add(note,0,2);return sidebar;
        }
        private Control BuildHeader()
        {
            TableLayoutPanel head=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty,Padding=new Padding(0,23,0,16),BackColor=Theme.Canvas};
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,114));
            FlowLayoutPanel titles=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false};
            titles.Controls.Add(new Label{Text="设置中心",AutoSize=true,ForeColor=Theme.Muted,Font=new Font(Font.FontFamily,8.5F),Margin=new Padding(0,0,0,4)});
            pageTitle=new Label{AutoSize=true,Font=new Font(Font.FontFamily,21F,FontStyle.Bold),ForeColor=Theme.Ink,Margin=new Padding(0,0,0,6)};
            pageDescription=LabelText("",650);pageDescription.Font=new Font(Font.FontFamily,9F);titles.Controls.Add(pageTitle);titles.Controls.Add(pageDescription);head.Controls.Add(titles,0,0);
            runningLabel=new Label{Text="●  相框运行中",Name="FrameStatus",AutoSize=false,Size=new Size(114,28),TextAlign=ContentAlignment.MiddleCenter,BackColor=Theme.AccentSoft,ForeColor=Theme.Accent,Margin=new Padding(0,30,0,0),Font=new Font(Font.FontFamily,8.5F)};head.Controls.Add(runningLabel,1,0);return head;
        }
        private Control BuildPreview()
        {
            FlowLayoutPanel aside=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=Padding.Empty,BackColor=Theme.Canvas};
            Card card=new Card{Width=226,Height=310,Padding=new Padding(16,18,16,16)};
            TableLayoutPanel layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Margin=Padding.Empty};
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,30));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,171));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,31));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            layout.Controls.Add(new Label{Text="相框预览",AutoSize=true,ForeColor=Theme.Ink,Font=new Font(Font.FontFamily,10,FontStyle.Bold)},0,0);
            preview=new Preview{Dock=DockStyle.Fill,Name="FramePreview",Margin=Padding.Empty};layout.Controls.Add(preview,0,1);
            previewDetail=new Label{Text="480 × 480  ·  圆角 16",AutoSize=false,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Theme.Muted,Font=new Font("Segoe UI",8.5F)};layout.Controls.Add(previewDetail,0,2);
            layout.Controls.Add(new Label{Text="鼠标移入预览透明度\n移到下方查看控制栏",Dock=DockStyle.Fill,TextAlign=ContentAlignment.TopCenter,ForeColor=Theme.Muted,Font=new Font(Font.FontFamily,8F)},0,3);
            card.Controls.Add(layout);aside.Controls.Add(card);
            Label hint=LabelText("预览效果后，点击\n「保存并应用」更新相框。",212);hint.Margin=new Padding(7,3,0,0);hint.Font=new Font(Font.FontFamily,8.5F);aside.Controls.Add(hint);return aside;
        }
        private Control BuildFooter()
        {
            TableLayoutPanel footer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(0,21,0,15),Margin=Padding.Empty,BackColor=Theme.Canvas};
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            status=new Label{Dock=DockStyle.Fill,Name="Status",AutoEllipsis=true,TextAlign=ContentAlignment.MiddleLeft,ForeColor=Theme.Muted,Font=new Font(Font.FontFamily,8.5F),Margin=new Padding(0,0,12,0)};footer.Controls.Add(status,0,0);
            FlowLayoutPanel buttons=new FlowLayoutPanel{AutoSize=true,WrapContents=false,Margin=Padding.Empty};
            buttons.Controls.Add(ButtonText("重新读取",delegate{Reload();},"Reload"));buttons.Controls.Add(ButtonText("仅保存",delegate{Save(false);},"SaveOnly"));
            SoftButton apply=(SoftButton)ButtonText("保存并应用",delegate{Save(true);},"SaveApply");apply.Primary=true;apply.Width=124;apply.Margin=Padding.Empty;buttons.Controls.Add(apply);footer.Controls.Add(buttons,1,0);return footer;
        }
        private TableLayoutPanel Page(string title)
        {
            TabPage page=new TabPage(title){BackColor=Theme.Canvas,Padding=Padding.Empty,Margin=Padding.Empty};
            Panel scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Theme.Canvas,Padding=new Padding(0,0,5,0)};
            Card card=new Card{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Padding=new Padding(20,5,20,6)};
            TableLayoutPanel table=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,RowCount=0,Margin=Padding.Empty,BackColor=Color.White};
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));card.Controls.Add(table);scroll.Controls.Add(card);page.Controls.Add(scroll);tabs.TabPages.Add(page);return table;
        }
        private void Row(TableLayoutPanel table,string label,Control input,string hint)
        {
            int n=table.RowStyles.Count;table.RowCount=n+1;table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            TableLayoutPanel row=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,RowCount=1,Margin=Padding.Empty,Padding=new Padding(0,16,0,16),BackColor=Color.White};
            bool full=input is InputShell && input.Controls.Contains(photos) || label=="" || input is CheckBox;
            if(full)
            {
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,0));
                FlowLayoutPanel stack=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=Padding.Empty,BackColor=Color.White};
                if(!String.IsNullOrEmpty(label)){Label caption=LabelText(label,500);caption.ForeColor=Theme.Ink;caption.Font=new Font(Font.FontFamily,9.5F,FontStyle.Bold);caption.Margin=new Padding(0,0,0,10);stack.Controls.Add(caption);}
                if(input is InputShell){input.Width=480;stack.SizeChanged+=delegate{input.Width=Math.Max(100,stack.ClientSize.Width);};}
                input.Margin=Padding.Empty;stack.Controls.Add(input);
                if(!String.IsNullOrEmpty(hint)){Label helper=LabelText(hint,510);helper.Margin=new Padding(0,7,0,0);helper.Font=new Font(Font.FontFamily,8.5F);stack.Controls.Add(helper);}
                row.Controls.Add(stack,0,0);row.SetColumnSpan(stack,2);
            }
            else
            {
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,215));
                FlowLayoutPanel text=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=new Padding(0,0,18,0)};
                Label caption=LabelText(label,310);caption.ForeColor=Theme.Ink;caption.Font=new Font(Font.FontFamily,9.5F);caption.Margin=new Padding(0,0,0,5);text.Controls.Add(caption);
                if(!String.IsNullOrEmpty(hint)){Label helper=LabelText(hint,270);helper.Font=new Font(Font.FontFamily,8.3F);text.Controls.Add(helper);text.SizeChanged+=delegate{helper.MaximumSize=new Size(Math.Max(100,text.ClientSize.Width),0);};}
                row.Controls.Add(text,0,0);input.Anchor=AnchorStyles.Right;input.Margin=Padding.Empty;row.Controls.Add(input,1,0);
            }
            if(n>0)row.Paint+=delegate(object sender,PaintEventArgs e){using(Pen pen=new Pen(Theme.Line))e.Graphics.DrawLine(pen,0,0,row.Width,0);};
            table.Controls.Add(row,0,n);
        }
        private void BuildLibrary()
        {
            TableLayoutPanel library=Page(pageNames[0]);
            photos=new TextBox{Name="PhotosPath",BorderStyle=BorderStyle.None,BackColor=Color.White,Font=new Font("Segoe UI",9.5F)};photos.TextChanged+=Changed;
            InputShell path=new InputShell(photos,480);Row(library,"照片库位置",path,"JPG、JPEG、PNG、BMP · 自动包含子文件夹");
            FlowLayoutPanel actions=new FlowLayoutPanel{AutoSize=true,WrapContents=false,BackColor=Color.White};actions.Controls.Add(ButtonText("选择文件夹…",Browse,"BrowsePhotos"));actions.Controls.Add(ButtonText("打开照片库",delegate{Guard(delegate{OpenFolder(photos.Text);});},"OpenPhotos"));Row(library,"",actions,"");
            photoStatus=LabelText("",510);photoStatus.Name="PhotoStatus";photoStatus.Font=new Font(Font.FontFamily,8.5F);Row(library,"",photoStatus,"");
            AddNumber(library,"ChangeSeconds");Row(library,"桌面相框",Actions(),"关闭相框后，仍可在这里修改设置。");
        }
        private void BuildAppearance()
        {
            TableLayoutPanel page=Page(pageNames[1]);foreach(string key in new string[]{"SizeW","SizeH","CornerRadius","ShadowOpacity","FramePadding","OutlineWidth"})AddNumber(page,key);AddColor(page,"BorderColor","描边颜色");AddColor(page,"BackColor","背景颜色");
        }
        private void BuildHover()
        {
            TableLayoutPanel page=Page(pageNames[2]);AddSwitch(page,"HoverEnabled","开启悬停淡化");foreach(string key in new string[]{"HoverOpacity","HoverFadeDuration","HoverEnterDuration","HoverLeaveDuration"})AddNumber(page,key);
        }
        private void BuildControls()
        {
            TableLayoutPanel page=Page(pageNames[3]);AddSwitch(page,"ShowControls","开启底部控制栏");foreach(string key in new string[]{"ControlTriggerPercent","ControlShowDuration","ControlHideDuration","ControlSlideDistance"})AddNumber(page,key);
        }
        private void BuildTools()
        {
            TableLayoutPanel page=Page(pageNames[4]);
            trayOnClose=new Toggle{Text="关闭窗口后留在托盘",Name="TrayOnClose",Width=430,Checked=ReadTrayPreference()};trayOnClose.CheckedChanged+=delegate{if(!loading)Guard(SaveTrayPreference);};Row(page,"托盘",trayOnClose,"双击托盘图标打开窗口，右键菜单可退出设置中心。");
            Row(page,"配置文件",ButtonText("打开配置文件",delegate{Guard(delegate{Process.Start(new ProcessStartInfo("notepad.exe","\""+store.PathName+"\""){UseShellExecute=true});});},"OpenConfig"),"也可以用记事本直接修改设置。");
            Row(page,"自动备份",ButtonText("查看备份",delegate{Guard(delegate{Directory.CreateDirectory(store.BackupDirectory);OpenFolder(store.BackupDirectory);});},"OpenBackups"),"保存修改前会备份旧配置，方便之后恢复。");
            Row(page,"使用指南",ButtonText("查看说明",delegate{Guard(delegate{Process.Start(new ProcessStartInfo("notepad.exe","\""+Program.UserGuide(root)+"\""){UseShellExecute=true});});},"OpenGuide"),"了解设置项含义、托盘操作和备份恢复方法。");
            Row(page,"恢复默认",ButtonText("填入默认值",Defaults,"Defaults"),"照片库位置不变，点击保存后才会生效。");
            FlowLayoutPanel credits=new FlowLayoutPanel{AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,BackColor=Color.White};
            credits.Controls.Add(new Label{Text="桌面相册",AutoSize=true,Font=new Font(Font.FontFamily,11,FontStyle.Bold),ForeColor=Theme.Ink,Margin=new Padding(0,0,0,6)});
            credits.Controls.Add(LabelText("基于 Rainmeter 和 Reminisce 修改，\n增加了尺寸适配、悬停动画和设置中心。",510));
            FlowLayoutPanel links=new FlowLayoutPanel{AutoSize=true,Margin=new Padding(0,9,0,0),BackColor=Color.White};
            foreach(string source in new string[]{"Rainmeter","Reminisce"})
            {string url=source=="Rainmeter"?"https://github.com/rainmeter/rainmeter":"https://github.com/misterhaan/Reminisce";LinkLabel link=new LinkLabel{Text=source,AutoSize=true,LinkColor=Theme.Accent,ActiveLinkColor=Theme.Ink,Margin=new Padding(0,0,18,0)};link.LinkClicked+=delegate{Guard(delegate{Process.Start(new ProcessStartInfo(url){UseShellExecute=true});});};links.Controls.Add(link);}
            credits.Controls.Add(links);Row(page,"",credits,"");
        }
        private void AddNumber(TableLayoutPanel table,string key)
        {
            SettingSpec spec=Schema.Find(key);NumericUpDown input=new NumericUpDown{Name=key,Minimum=spec.Minimum<0?0:spec.Minimum,Maximum=spec.Maximum,DecimalPlaces=spec.Decimals,Increment=spec.Decimals>0?.1M:1,Width=115,BorderStyle=BorderStyle.None,BackColor=Color.White,Font=new Font("Segoe UI",10F)};
            numbers[key]=input;input.ValueChanged+=Changed;input.TextChanged+=Changed;
            FlowLayoutPanel group=new FlowLayoutPanel{AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,BackColor=Color.White,Margin=Padding.Empty};
            FlowLayoutPanel value=new FlowLayoutPanel{AutoSize=true,WrapContents=false,BackColor=Color.White,Margin=Padding.Empty};value.Controls.Add(new InputShell(input,130));Label unit=LabelText(spec.Unit,65);unit.Margin=new Padding(1,9,0,0);unit.Font=new Font(Font.FontFamily,8.5F);value.Controls.Add(unit);group.Controls.Add(value);
            if(spec.Minimum<0)
            {CheckBox check=new Toggle{Text="沿用共用时长",Name=key+"Inherit",Width=200,Height=25,Font=new Font(Font.FontFamily,8.5F),ForeColor=Theme.Muted,Margin=new Padding(0,8,0,0)};inherit[key]=check;check.CheckedChanged+=delegate{input.Enabled=!check.Checked;Changed(check,EventArgs.Empty);};group.Controls.Add(check);}
            Row(table,spec.Label,group,spec.Hint);
        }
        private void AddSwitch(TableLayoutPanel table,string key,string label)
        {Toggle input=new Toggle{Text=label,Name=key,Width=420,Font=new Font(Font.FontFamily,10F)};switches[key]=input;input.CheckedChanged+=Changed;Row(table,"",input,key=="HoverEnabled"?"鼠标移入时透出后方桌面，移出后恢复。":"进入照片底部时出现，离开后收起。");}
        private void AddColor(TableLayoutPanel table,string key,string label)
        {
            TextBox input=new TextBox{Name=key,MaxLength=8,CharacterCasing=CharacterCasing.Upper,BorderStyle=BorderStyle.None,Font=new Font("Segoe UI",9F)};colors[key]=input;input.TextChanged+=Changed;
            FlowLayoutPanel controls=new FlowLayoutPanel{AutoSize=true,WrapContents=false,BackColor=Color.White,Margin=Padding.Empty};controls.Controls.Add(new InputShell(input,111));
            Button picker=ButtonText("选色",delegate{using(ColorDialog dialog=new ColorDialog()){dialog.FullOpen=true;try{dialog.Color=ColorTranslator.FromHtml("#"+input.Text.Substring(0,6));}catch(ArgumentException){}if(dialog.ShowDialog(this)==DialogResult.OK)input.Text=dialog.Color.R.ToString("X2")+dialog.Color.G.ToString("X2")+dialog.Color.B.ToString("X2")+(input.Text.Length==8?input.Text.Substring(6,2):"FF");}},key+"Picker");picker.Width=65;picker.MinimumSize=Size.Empty;picker.Margin=Padding.Empty;controls.Controls.Add(picker);Row(table,label,controls,"前六位为颜色，末两位为透明度（00–FF）。");
        }
        private Control Actions()
        {
            FlowLayoutPanel actions=new FlowLayoutPanel{AutoSize=true,WrapContents=false,BackColor=Color.White,Margin=Padding.Empty};
            Button start=ButtonText("启动",delegate{Guard(delegate{frame.Start();feedback="已启动相框。";UpdateStatus();});},"StartFrame");start.Width=80;
            Button stop=ButtonText("关闭",delegate{Guard(delegate{frame.Stop();feedback="已关闭相框。";UpdateStatus();});},"StopFrame");stop.Width=80;stop.Margin=Padding.Empty;actions.Controls.Add(start);actions.Controls.Add(stop);return actions;
        }
        private void SelectPage(int index)
        {
            if(index<0||index>=pageNames.Length)return;pageTitle.Text=pageNames[index];pageDescription.Text=descriptions[index];
            for(int i=0;i<navigation.Count;i++){navigation[i].Selected=i==index;navigation[i].Invalidate();}
            navigationRail.SelectItem(navigation[index]);
        }
        private void RefreshVisuals(bool running)
        {
            if(preview==null || !numbers.ContainsKey("SizeW"))return;
            runningLabel.Text=running?"●  相框运行中":"○  相框已关闭";runningLabel.ForeColor=running?Theme.Accent:Theme.Muted;runningLabel.BackColor=running?Theme.AccentSoft:Theme.Sidebar;
            preview.MaximumW=numbers["SizeW"].Value;preview.MaximumH=numbers["SizeH"].Value;preview.Radius=numbers["CornerRadius"].Value;preview.Opacity=numbers["HoverOpacity"].Value;preview.Trigger=numbers["ControlTriggerPercent"].Value;preview.Distance=numbers["ControlSlideDistance"].Value;
            preview.Fade=switches["HoverEnabled"].Checked;preview.ControlsEnabled=switches["ShowControls"].Checked;preview.Invalidate();
            preview.EnterDuration=inherit["HoverEnterDuration"].Checked?numbers["HoverFadeDuration"].Value:numbers["HoverEnterDuration"].Value;
            preview.LeaveDuration=inherit["HoverLeaveDuration"].Checked?numbers["HoverFadeDuration"].Value:numbers["HoverLeaveDuration"].Value;
            preview.ShowDuration=numbers["ControlShowDuration"].Value;preview.HideDuration=numbers["ControlHideDuration"].Value;
            previewDetail.Text="最大 "+numbers["SizeW"].Value+" × "+numbers["SizeH"].Value+"  ·  圆角 "+numbers["CornerRadius"].Value;
        }
    }
}
