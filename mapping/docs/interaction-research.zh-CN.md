# WASD 物体交互与空格修饰键

[English](interaction-research.md)

Riot 在 2026 年 4 月的[输入系统更新说明](https://www.leagueoflegends.com/en-sg/news/dev/dev-wasds-ranked-release/)中，将原来合并的 WASD 操作拆成选取物体、与物体交互、普攻、确认施法，并明确提到 Hexgate 传送门交互。[官方 WASD FAQ](https://support.riotgames.com/en-us/league-of-legends/gameplay/league-of-legends-keyboard-wasd-input-faq)说明，全局和英雄专属的 WASD 绑定与传统点击移动绑定分开保存。

先在实际游戏中检查 **键盘（WASD）→ 施放和交互**。进入传送门与选取、确认落点可能涉及不同动作，不能把截图中的“移动宠物点击”当成同一功能。旧保存配置可能仍含 `SmartInteract` 等合并动作；仅凭这个字段，不能认定它导致点击失效，保存文件也可能省略默认值。

优先在 LoL 内绑定所需动作。如果当前客户端支持，且使用者接受，可以考虑按空格直接在鼠标所在位置交互。精确的 **按住空格＋点击左键** 尚未验证：官方允许绑定普通键，不代表普通键能作为组合键修饰符。不要直接把 `[Space][Button 1]` 写入整份保存配置并假定有效。任何实际改动都应备份相关绑定，并在训练模式验证。

社区存在修改隐藏 `ModifierEvents` 的做法，但[近期使用者报告新版失效](https://www.reddit.com/r/leagueoflegends/comments/1xuo4kj/modifierevents_keybind_changes_in/)，不能视为稳定原生方案。

当前可选 [空格作为右 Ctrl](../../rumble/docs/input-remap.zh-CN.md) 使用同一个 LoL exe 的隔离输入工作进程，仅实际对局前台转换空格按下/松开；鼠标和技能键仍由使用者操作。使用者已确认空格＋左键、技能升级及松开后输入正常，不区分游戏聊天/菜单；其他电脑和版本需自行验证。普通 USB/托盘进程没有键盘钩子，不改 Steam 或 PowerToys 设置。Windows [SendInput](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput)仍受权限限制；修饰键归属、切出释放、按需任务和退出方法见输入模块说明。它替代旧版独立四事件点击工具；仅调研不代表允许安装此功能。
