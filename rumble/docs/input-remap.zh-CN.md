# 可选：LoL 空格作为右 Ctrl

[English](input-remap.md)

使用同一个 **`LoLDamageRumble.exe`**，共用 LoL 工具托盘和已有登录入口。振动、Steam 前台布局选择、空格转换在代码中独立；不需要单独的点击程序。旧版本曾用独立鼠标工具转换空格＋左键，新方案应先退出旧工具。

## 行为

- 仅实际 **`League of Legends.exe` 前台**时，把物理空格按下/松开转换为右 Ctrl 按下/松开，不传递原始空格。
- 按住空格时，鼠标和技能键仍由使用者操作并照常交给游戏，可以使用 Ctrl＋左键交互，以及 Ctrl＋技能键升级技能。
- 空格自动重复不会反复按 Ctrl。松开空格、离开游戏前台、锁屏/休眠或退出输入模块时释放它拥有的右 Ctrl。
- 使用者自己按住的右 Ctrl 会保留；左 Ctrl、左 Alt 和 PowerToys 的交换不在此模块的转换范围。
- 在游戏外按住空格、再切回游戏，不会自动把已按住的键改成 Ctrl；松开后重新按下即可。
- 不区分游戏聊天或菜单：只按前台进程限定。在 LoL 前台，空格用于修饰键，无法同时作为输入空格或镜头复位键。这是当前使用者不在对局打字时选择的行为。

没有连点、技能自动化、读游戏内存或注入游戏进程。运行状态仅保存当前修饰键归属及计数，不记录文字或按键历史。

## 检查源码再启用

检查 [LoLInputRemap.cs](../src/LoLInputRemap.cs)、[测试](../tests/LoLRumbleTests.cs)，使用[常规构建脚本](../scripts/build.ps1)自行编译。构建/测试不安装钩子、不模拟输入。默认不开启转换，也不要求振动程序依赖键盘钩子。

把 [示例](../config/input-remap.example.json)复制到自己编译的 exe 旁，改名为 `input-remap.json`，根据需要将 `enabled` 改为 `true`。它只有一个布尔字段，与 `lol-integration.json` 分开。托盘“LoL 空格作为右 Ctrl”可临时开关，不会重写配置文件；修改启动默认需要编辑配置并重启。

## 一个 exe，隔离工作进程

普通权限进程负责托盘、比赛数据、USB 振动和 Steam 布局选择；**它没有键盘钩子**。同一个 exe 以 `--input-worker` 模式启动另一个工作进程，仅安装键盘钩子并处理空格/右 Ctrl，不访问 USB、比赛接口或 Steam。输入模块不能与 USB 写入共用容易阻塞的消息循环。

Windows `SendInput` 无法控制更高权限的游戏，所以输入工作进程按需提权；**不需要给 PowerToys、振动主进程或灭灯程序提权**。没有预先安装工作任务时，启用功能会弹出该 exe 的 UAC。

如希望已有普通权限登录入口自动带起输入模块，检查 [任务脚本](../scripts/input-worker-task.ps1)，再在管理员 PowerShell 中注册一次按需任务：

```powershell
powershell.exe -NoProfile -File .\rumble\scripts\input-worker-task.ps1 -Action Enable -Executable "C:\YourTools\LoLDamageRumble.exe"
```

任务名 `PlayStationControllerTools-LoLInput`，运行当前用户的同一个 exe，参数仅 `--input-worker`，无密码、无独立登录触发器。主进程启用功能时启动任务；已有 LoL 登录入口保持普通权限。不要从任务计划程序强制结束仍按住修饰键的工作进程。

关闭托盘开关或退出主程序，会停止心跳并让工作进程释放修饰键后退出。主进程意外退出或失去响应时，心跳超时也让工作进程退出。`input-host.json` 是主进程心跳，`input-status.json` 是工作进程当前状态；不要上传个人运行数据。运行目录必须可由当前用户写入。

要移除按需任务，先在托盘关闭转换并确认工作进程退出，再对同一路径执行脚本 `-Action Disable`。不要删除或改名任务所引用的 exe 后留下旧任务。

## 验证

158 项离线检查包含原有振动/前台选择检查，以及修饰键归属、两种松键顺序、切出窗口、重复键与右 Ctrl 报告格式。**使用者已在训练模式确认空格＋左键、空格＋技能键升级、松开后正常输入三项生效**；运行计数确认右 Ctrl 按下/释放配对，无模拟输入错误。其他电脑和游戏版本仍需自行验证。

来源：[Windows SendInput 权限限制](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput)、[LowLevelKeyboardProc](https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelkeyboardproc)。原生 LoL 绑定调研：[mapping/docs/interaction-research.zh-CN.md](../../mapping/docs/interaction-research.zh-CN.md)。
