# 可复制的振动反馈 Prompt

把下面内容交给能访问此仓库的 AI，最后一行替换成你的具体需求。

```text
请帮助我使用或定制 rumble/ 模块。先读取根目录和模块 AGENTS.md、rumble/README.zh-CN.md、docs/configuration.zh-CN.md、config/FeedbackConfig.cs、src/LoLDamageRumble.cs、src/LoLIntegration.cs、docs/integration.zh-CN.md 和 tests/LoLRumbleTests.cs。

这是 Windows x64、USB DS4 第二代 / DualSense 普通双通道兼容振动的英雄联盟本地比赛数据反馈程序，独立于 Steam 按键布局。当前 default 是原使用者的默认方案。优先通过带注释的配置修改阈值、开关、强度、节奏和优先级，再重新编译；原生 Steam 布局留在 mapping/；可选本机前台整合共用此托盘，但不成为振动依赖。

先说明我需要改哪些参数，以及单位、取值范围、触发/恢复门槛和震动重叠行为。保留事件去重、失联重建基准、过期反馈丢弃和暂停/退出停转。使用模拟数据验证，不靠实际振动验证纯逻辑。验证 USB 游戏手柄描述符与各型号启动/停止报告，普通马达报告不启用灯光、扳机、音频或麦克风更新标志。可选前台整合可发送独立的 USB DualSense 双扳机 Off 报告；保留切出 LoL、锁屏、睡眠及退出时交回控制，不假装能恢复任意旧效果。不要声称支持蓝牙、DualSense Edge、音频触觉或游戏触发的自适应扳机反馈。

检查源码和脚本后，在本地运行 scripts/build.ps1，选择适当的 -Preset 和 -Language。检查测试结果并提供生成文件位置。同步中英文注释/说明；不要建议我直接运行来历不明的 .exe，也不要声称不存在的控制状态、地图视野或弹药接口可用。

我的具体需求：<填写希望的反馈体验>
```
