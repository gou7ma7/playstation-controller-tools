# WASD 物体交互：调研，不是已安装的转换功能

[English](interaction-research.md)

Riot 在 2026 年 4 月的[输入系统更新说明](https://www.leagueoflegends.com/en-sg/news/dev/dev-wasds-ranked-release/)中，将原来合并的 WASD 操作拆成选取物体、与物体交互、普攻、确认施法，并明确提到 Hexgate 传送门交互。[官方 WASD FAQ](https://support.riotgames.com/en-us/league-of-legends/gameplay/league-of-legends-keyboard-wasd-input-faq)说明，全局和英雄专属的 WASD 绑定与传统点击移动绑定分开保存。

先在实际游戏中检查 **键盘（WASD）→ 施放和交互**。进入传送门与选取、确认落点可能涉及不同动作，不能把截图中的“移动宠物点击”当成同一功能。旧保存配置可能仍含 `SmartInteract` 等合并动作；仅凭这个字段，不能认定它导致点击失效，保存文件也可能省略默认值。

优先在 LoL 内绑定所需动作。如果当前客户端支持，且使用者接受，可以考虑按空格直接在鼠标所在位置交互。精确的 **按住空格＋点击左键** 尚未验证：官方允许绑定普通键，不代表普通键能作为组合键修饰符。不要直接把 `[Space][Button 1]` 写入整份保存配置并假定有效。任何实际改动都应备份相关绑定，并在训练模式验证。

原生绑定表达不了这个组合时，才考虑独立、可选、仅 LoL 前台生效的转换：一次物理组合对应一次 Ctrl＋左键。它需要处理聊天、修饰键归属、两种松键顺序、切出窗口、锁屏/休眠、退出与进程权限差异，不能释放使用者自己按住的 Ctrl，也不能把模拟 Ctrl 留在游戏外。这些是设计要求，不是已实现功能的保证。Windows [SendInput](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput)受进程完整性级别限制。

振动程序没有提供键盘或鼠标转换。Steam 手柄映射、PowerToys 键盘交换与这个可能的后续功能各自独立；先验证游戏原生绑定，再考虑增加输入钩子。
