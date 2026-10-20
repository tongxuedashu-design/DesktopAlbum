# 分发文件说明 / Distribution Notes

## 中文

本项目源码包采用英文目录和文件名，包含设置中心源码、相框皮肤和脚本、默认配置、示例图、说明和上游许可。目录结构与源码 README 中的构建命令对应。

照片库只放代码绘制的山景。五张演示图使用默认设置和通用路径，不读取实际照片或截取桌面。实际配置、个人照片、聊天、备份、日志和本机快捷方式均未打包。`.gitignore` 排除了本地运行文件和新增照片。

`source-manifest.json` 记录公开文件的相对路径、大小和 SHA-256，不记录私人文件名或哈希。可运行 `src/GenerateScreenshots.ps1` 重新生成演示图。

便携版已附引擎和对应源码。本项目源码 ZIP 不附引擎运行文件，开发准备步骤见根目录 README。

## English

This app's source archive uses English folder and file names. It includes the settings center, skin and scripts, default templates, sample image, documentation, and upstream notices. Its layout matches the build commands in the source README.

The photo library contains only a landscape drawn by code. The five demonstration screenshots use default settings and generic paths; they do not read personal photos or capture the desktop. Active settings, personal photos, chats, backups, logs, and local shortcuts are excluded. `.gitignore` excludes local runtime files and added photos.

`source-manifest.json` lists the relative paths, sizes, and SHA-256 hashes of public files, with no private filenames or hashes. Run `src/GenerateScreenshots.ps1` to regenerate the demonstration images.

The portable release includes the engine and its corresponding source. This app's source ZIP excludes engine binaries; see the root README for development setup.
