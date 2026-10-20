# 使用说明 / User Guide

[中文](#中文) · [English](#english)

## 中文

### 启动和选择照片

完整解压后双击 `DesktopAlbum.exe`。在「照片与轮播」页选择照片文件夹，支持 JPG、JPEG、PNG 和 BMP，也会读取子文件夹。设好换图间隔，点击「保存并应用」。自动播放随机换图，手动翻图按文件名排序。

「尺寸与外观」页可设置最大宽高、圆角、阴影和描边。照片保持原比例，大图缩小，小图保留原尺寸。Windows 显示缩放会影响屏幕上的实际大小。

右侧示意图可以预览尺寸、圆角和鼠标效果。底部按钮的作用如下。

| 按钮 | 操作 |
| --- | --- |
| 重新读取 | 载入文件中保存的设置，丢弃窗口中尚未保存的修改 |
| 仅保存 | 将修改写入配置文件 |
| 保存并应用 | 保存设置并更新桌面相框 |

### 鼠标移入效果

在「悬停透明度」页调整淡化程度。「悬停可见度」范围为 1 到 255，数值越小越透明。默认 90 约为 65% 透明，128 约为一半透明，255 完全可见。

移入和移出默认都用 180 毫秒。需要分开调整时，关闭对应的「沿用共用时长」开关，再填写时长。范围为 0 到 5000 毫秒，0 表示立即切换。配置文件中的 `-1` 表示沿用共用时长。

相框变透明后仍可拖动和点击，鼠标不会穿透到后面的窗口。

### 底部控制栏

鼠标进入照片底部时，控制栏从下方滑入；离开这片区域后收起。「底部控制栏」页可以调整触发范围、出现时长、收起时长和滑动距离。

默认在底部 30% 区域触发，出现用 240 毫秒，收起用 200 毫秒，滑动 54 像素。小照片至少保留 64 像素的触发区域，照片太小时隐藏控制栏。滑动距离为 0 时只淡入淡出。

控制栏可切换上一张、暂停或继续、切换下一张、打开原图。按住照片主体可拖动，双击打开原图。右键相框可打开设置中心或照片库。

### 托盘和关闭

关闭设置窗口后，默认留在系统托盘。双击图标重新打开，右键菜单可退出设置中心。「工具与备份」页的「关闭窗口后留在托盘」开关可修改这个行为。

「退出设置中心」只关闭设置窗口和它的托盘图标。要关闭相框，在「照片与轮播」页点击「关闭」，或使用托盘菜单中的「关闭相框」。相框位于普通窗口后方，按 Win+D 可回到桌面查看。

### 备份和恢复

便携版的设置保存在 `Config/PhotoFrame.inc`，托盘偏好在 `Config/SettingsCenter.ini`。首次启动会从模板生成相框配置；托盘偏好在修改开关时保存。

每次保存修改前，程序将旧配置备份到 `Config/Backups/SettingsCenter`。「工具与备份」页的「查看备份」可打开该目录。恢复时先退出设置中心，用选定的备份替换 `Config/PhotoFrame.inc`，再重新打开并应用。

也可以在「工具与备份」页点击「打开配置文件」，用记事本编辑。改完后点击「重新读取」，再保存并应用。程序保存前会检查其他软件是否改过文件，避免覆盖外部修改。

「填入默认值」保留照片库位置，点击保存后才生效。颜色使用 8 位 `RRGGBBAA` 格式，末两位表示透明度，`00` 完全透明，`FF` 完全不透明。

### 文件夹和移动

便携版包含 `Engine`、`Config`、`Photos`、`Docs` 和 `DesktopAlbum.exe`。自己的照片可放入 `Photos`，也可以另选文件夹。`Docs/sources` 保留本程序和 Rainmeter 的源码，日常使用无需解压。

移动前先退出设置中心和相框引擎，再移动整个文件夹。程序会更新内置照片库和相框资源路径，外部照片库位置保持不变。首次运行后的 `Config/location.txt` 用于识别内置库的旧位置。

源码开发环境的相框资源放在 `runtime`，开发备份位于 `artifacts/Backups/SettingsCenter`。开发步骤见源码包根目录的 README，版权和许可见同一包内的 LICENSE。

## English

### Starting the app and choosing photos

Extract the whole archive and double-click `DesktopAlbum.exe`. On 「照片与轮播」 (Photos and slideshow), select your photo folder. JPG, JPEG, PNG, and BMP files are supported, including files in subfolders. Set the interval and click 「保存并应用」 (Save and apply). Automatic playback selects photos at random; manual previous/next navigation follows filename order.

「尺寸与外观」 (Size and appearance) sets the maximum width and height, corner radius, shadow, and outline. Photos keep their aspect ratio. Large images are scaled down; small images keep their original size. Windows display scaling affects the size you see on screen.

The illustration on the right previews size, corners, and mouse effects. The footer buttons work as follows.

| Button in the app | Action |
| --- | --- |
| 重新读取 — Reload | Reads saved settings and discards unsaved changes in the window |
| 仅保存 — Save only | Writes changes to the configuration file |
| 保存并应用 — Save and apply | Saves changes and updates the desktop frame |

### Mouse hover transparency

「悬停透明度」 (Hover transparency) controls how much the frame fades. 「悬停可见度」 (Hover visibility) accepts 1–255; lower values make the frame more transparent. The default, 90, is about 65% transparent. A value of 128 is about half transparent; 255 is fully visible.

Both transitions take 180 milliseconds by default. To set separate times, turn off the corresponding 「沿用共用时长」 (Use shared duration) switch and enter a duration. The range is 0–5000 milliseconds; 0 changes immediately. In the configuration file, `-1` means use the shared duration.

The frame still accepts clicks and dragging while faded. Mouse input does not pass through to windows behind it.

### Bottom controls

The controls slide up when the pointer enters the bottom part of the photo and slide down when it leaves that area. 「底部控制栏」 (Bottom controls) sets the trigger area, show/hide durations, and slide distance.

By default, the trigger area covers the bottom 30%, the controls appear in 240 milliseconds, disappear in 200 milliseconds, and move 54 pixels. Small photos retain a trigger area at least 64 pixels high; controls are hidden if the photo is too small. A slide distance of 0 uses fading only.

The buttons show the previous photo, pause/resume, show the next photo, and open the original image. Drag the body of the photo to move the frame. Double-click it to open the original. Right-click the frame to open settings or the photo folder.

### Tray behavior and closing

The settings window stays in the system tray when closed by default. Double-click the tray icon to reopen it, or use its menu to exit the settings center. Change this behavior with 「关闭窗口后留在托盘」 (Keep in tray when closed) on 「工具与备份」 (Tools and backups).

「退出设置中心」 (Exit settings center) closes only the settings window and its tray icon. To close the frame, click 「关闭」 (Close) on the photos page or choose 「关闭相框」 (Close frame) in the tray menu. The frame sits behind normal windows; press Win+D to view the desktop.

### Backups and restoration

The portable app stores settings in `Config/PhotoFrame.inc` and tray preferences in `Config/SettingsCenter.ini`. The first launch creates the frame configuration from its template. Tray preferences are saved when you change the tray switch.

Before saving changes, the app backs up the previous configuration to `Config/Backups/SettingsCenter`. Click 「查看备份」 (View backups) on the tools page to open the folder. To restore a backup, exit the settings center, replace `Config/PhotoFrame.inc` with the chosen backup, then reopen the app and apply it.

You can also click 「打开配置文件」 (Open configuration file) to edit it in Notepad. After editing, click Reload, then Save and apply. The app checks for external file changes before saving to avoid overwriting another program's edits.

「填入默认值」 (Fill defaults) keeps the photo folder; the defaults take effect after saving. Colors use eight-digit `RRGGBBAA`. The last two digits are opacity, with `00` fully transparent and `FF` fully opaque.

### Folders and relocation

The portable folder contains `Engine`, `Config`, `Photos`, `Docs`, and `DesktopAlbum.exe`. Add photos to `Photos` or select a separate folder. `Docs/sources` contains the app and Rainmeter source archives; extraction is unnecessary for normal use.

Exit the settings center and Rainmeter engine before moving the entire folder. On launch, paths to the built-in photo library and skin resources update automatically. External library paths stay unchanged. `Config/location.txt`, created on first launch, records the previous location of the built-in library.

In the source package, runtime resources are under `runtime` and development backups go to `artifacts/Backups/SettingsCenter`. See the source package's root README for build instructions and its LICENSE for copyright and license information.
