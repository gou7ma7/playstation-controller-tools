# 名称与版本迁移

[English](migration.md)

项目现名为 **PlayStation Controller Tools**，地址为 <https://github.com/gou7ma7/playstation-controller-tools>。`mapping/`、`rumble/`、`sleep-light/` 仍是独立模块，路径保留；通用 PS4/PS5 灯光工具不依赖英雄联盟。

更新已有仓库的远端地址：

```powershell
git remote set-url origin https://github.com/gou7ma7/playstation-controller-tools.git
git pull --ff-only
```

GitHub 会重定向旧 `lol-ds4-steam-input` 链接。历史 v0.1.0 附件保留原名和原代码，里面仍是较早的 DS4 专用睡眠工具；当前源码及 v0.2.0 使用新名称。

## ControllerLight 灭灯工具

- 主程序/命名空间：`ControllerLight.exe` / `ControllerLight`。
- 编译时配置：`sleep-light/config/LightConfig.cs`，类名 `LightConfig`；旧版定制参数可先迁入，再编译。
- 构建把同一程序复制为 `DS4SleepLight.exe`，作为兼容别名，不是第二个服务或启动器。两种名称共用旧单实例与退出事件标识，也能停止更名前的实例。
- 替换前从托盘退出旧工具，或执行 `DS4SleepLight.exe --stop`。检查源码并编译后，把两个程序名和报告复制到自己的目录，运行 `ControllerLight.exe`。
- 使用 `sleep-light/scripts/autostart.ps1 -Action Enable -Executable <ControllerLight.exe路径>` 更新自启动；先保存 `ControllerLight.lnk`，再移除旧 `DS4SleepLight.lnk`。Disable 移除两种入口。其他快捷方式可改指向新程序，也可继续使用兼容别名。
- 主日志仍为 EXE 旁的 `logs/events.log`；新版回退目录为 `%LOCALAPPDATA%\ControllerLight\events.log`，旧 `%LOCALAPPDATA%\DS4SleepLight` 日志保留。
- 构建清单记录两个程序名和校验值。CI 附件改名为 `ControllerLight-<语言>-windows-x64`，包含两种名称；CI 仍不负责安装、启动或发布程序。

## 按模块说明设备支持

ControllerLight 和当前 LoL 振动均支持 USB DS4 第二代与 DualSense；映射 VDF 仍是 DS4 导出，DualSense 可手动重建核心操作。更名不代表已经加入蓝牙、DualSense Edge、原生 PS5 游戏反馈或已验证的 DualSense VDF 支持。可选空格修饰键已集中到同一个 LoL exe，见 [输入模块](../rumble/docs/input-remap.zh-CN.md)。

仍建议先检查源码再自行编译；Windows 完全睡眠时无法执行周期灭灯刷新。
