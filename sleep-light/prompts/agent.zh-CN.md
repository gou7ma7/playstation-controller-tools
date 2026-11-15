# 可复制的睡眠灭灯 Prompt

交给能访问仓库的 AI，替换最后一行。

```text
请帮助我使用或定制 sleep-light/。先读取根目录/模块 AGENTS.md、sleep-light/README.zh-CN.md、docs/configuration.zh-CN.md、config/LightConfig.cs、src/ 全部源码和 tests/SleepLightTests.cs。

这是独立的 Windows x64 USB DS4 第二代 / DualSense 睡眠与空闲灭灯工具。校验设备能力，按型号选择输入/输出布局，不能把 DualSense 当成 DS4。默认共享读取按键、方向键、摇杆和扳机；三分钟不操作就灭灯，实际操作手柄恢复配置蓝色。DualSense 白色玩家灯一起灭灯，恢复时使用可配置掩码（默认中央一灯）；保留麦克风静音、触觉、自适应扳机和音频字段。忽略静置上报、计数器、传感器/电量变化和触摸滑动，用配置阈值过滤漂移。空闲且电脑运行时低频重发灭灯；正常使用不周期性写灯色，恢复/解锁本身不点亮。EnableIdleLight=false 保留仅睡眠模式及恢复/解锁点亮。Windows 完全睡眠时无法执行重发，Steam 最后灯色无法可靠读取。保持模块独立，不启用马达、不注入或映射游戏输入；待处理 IO 完成/取消后才能释放缓冲区。保留已有程序和单实例名称，兼容已安装的快捷方式。

优先修改有注释的编译时配置，校验范围，通过 scripts/build.ps1 自行编译，同步双语文档。离线测试不发送 USB 指令，不自动休眠我的电脑。明确区分写入成功与实际肉眼/睡眠验证。解释可选的当前用户自启动及撤销方式。不运行网上下载的二进制，不为了灯光安装额外驱动。

我的具体需求：<期望的行为或配置>

命名：ControllerLight.exe / ControllerLight 命名空间，LightConfig.cs / LightConfig。保留内容一致的 DS4SleepLight.exe 兼容别名及旧单实例/退出标识，自启动迁移旧快捷方式；DS4 协议名是型号标识，不是过期品牌名。
```
