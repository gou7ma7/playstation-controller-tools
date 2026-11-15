# WASD object interaction: research, not an installed remapper

[简体中文](interaction-research.zh-CN.md)

Riot's April 2026 [input-system update](https://www.leagueoflegends.com/en-sg/news/dev/dev-wasds-ranked-release/) split the former combined WASD action into Select Object, Interact with Object, Auto Attack and Cast Ability. The official explanation specifically includes Hexgate interaction. The [WASD FAQ](https://support.riotgames.com/en-us/league-of-legends/gameplay/league-of-legends-keyboard-wasd-input-faq) confirms that global and champion-specific WASD bindings are independent of Point & Click bindings.

First inspect **Keyboard (WASD) → Casting and Interaction** in the running game's options. Distinguish interacting with the gate from selecting/confirming its destination. Do not confuse either with the screenshot's Move Pet Click binding. Local saved settings may contain older combined actions such as `SmartInteract`; their presence alone does not prove why a click failed, and saved files can omit defaults.

Prefer binding the required actions inside LoL. Space alone at the mouse cursor may be a useful alternative if the player accepts it and the current client supports that binding. Exact **hold Space + click left** support is unverified: the official ability to bind keyboard keys does not establish that an ordinary key can act as a chord modifier. Do not insert `[Space][Button 1]` into an entire saved configuration and assume it works. Back up relevant bindings and verify in practice mode before applying any change.

If native binding cannot express the requested combination, a separate optional LoL-only remapper could translate one physical combination to one Ctrl + click. It would need to handle chat, modifier ownership, both release orders, focus loss, lock/suspend, exit and process-privilege differences. It must never release a Ctrl held by the user or leave an injected Ctrl down outside the game. Those are design requirements, not guarantees of an implemented feature. Windows [SendInput](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput) is subject to process-integrity restrictions.

No keyboard/mouse conversion is shipped in the rumble program. Steam controller mapping, PowerToys keyboard swaps and this possible future feature are distinct concerns. Investigate native bindings before adding another input hook.
