# LoL left-hand controller + right-hand mouse mapping

[简体中文](README.zh-CN.md) · English

This module is independently usable. It contains a native Steam Input layout and the LoL WASD settings needed to turn controller outputs into skill actions. No feedback application is required.

The VDF is a faithful **DS4 export**. DualSense users can recreate the documented left-stick/L1/L2/L3 keyboard outputs manually. A native DualSense export or cross-controller import is unverified; the DS4 filename and controller type are deliberately preserved.

> [!IMPORTANT]
> **Required: start Steam as administrator before using this mapping in LoL.** Completely exit the existing Steam instance, then right-click Steam → **Run as administrator** and approve the Windows prompt. A normal Steam instance may apply the layout on the desktop but have no permission to control LoL. Follow the [startup and troubleshooting steps](docs/setup.md). This requirement applies to Steam mapping; the independent feedback tools do not need administrator rights.

## Controls

- Left stick: WASD movement.
- L2: Q skill; Steam emits keyboard E.
- L1: W skill; Steam emits Left Shift.
- Mouse right: E skill, assigned directly in LoL.
- L3: R skill; Steam emits R.

Only the controller's left half is used. The right hand uses a real mouse for aiming, interaction and additional bindings. See [full setup](docs/setup.md).

## Apply manually

For automatic LoL-only activation, keep the layout on a local non-Steam shortcut and use the [optional foreground integration](../../rumble/docs/integration.md). The game can still launch from its usual launcher. Steam performs the input mapping; the optional app only selects/releases the saved layout. Keep the shortcut as the layout identity, configure its local AppID, and stop any older selection watcher. This avoids applying the LoL controls to the entire desktop.

The following desktop-layout method is the original manual setup. A desktop layout is global and can interfere with other games; save the original layout before trying it.

1. Completely exit Steam, then start it with **Run as administrator**.
2. Connect your controller and enable its Steam Input support.
3. For DS4, preview and apply `steam://controllerconfig/413080/3827898683` (英雄联盟 LOL WASD和鼠标). For DualSense, manually recreate the core outputs in its own Steam Input desktop layout.
4. Enable LoL's WASD mode and assign Q→E, W→Left Shift, E→Mouse right, R→R in that mode's settings.
5. Test movement and skill use in practice mode before normal play.

413080 identifies Steam's desktop layout, not LoL. Save your previous desktop layout first if you want to return to it. The Steam link was confirmed in the original user's local export/load record; access from other accounts is unverified. If unavailable, rebuild the core controls using the setup guide. Downloading or double-clicking a VDF does not automatically import it.

## Champion and interaction notes

Riot now exposes separate WASD selection, interaction, attack and cast bindings. Check those native options before adding an operating-system remapper. [Interaction research](docs/interaction-research.md) explains the remaining uncertainty around Space + left click; no Space-to-Ctrl conversion is included.

For sustained/channelled R, D-pad down can avoid accidentally moving the left stick while pressing L3. The exported layout emits **3** there, so set that champion's R to 3 in the game. Passive-E champions such as Teemo/Twisted Fate, or champions using Q/W very frequently, can use a per-champion right-button swap. Some interactions, including Frost Gates and destination selection, need **Ctrl + left click** in the user's setup. These are manual notes, not automatic code; see [usage notes](docs/usage-notes.md).

## Files and AI use

- [Native VDF](steam-input/lol-left-hand-ds4.vdf), [format/provenance](steam-input/README.md), [metadata](steam-input/metadata.json).
- [Game reference JSON](game-settings/wasd-keybinds.reference.json): reference only, not a Riot import file.
- [AGENTS.md](AGENTS.md) and [English prompt](prompts/agent.en.md) / [Chinese prompt](prompts/agent.zh-CN.md).
- [MIT license](LICENSE).
- [Related mapping work](docs/related-work.md).

The layout retains inactive groups and old unused bindings for faithful backup. Functional group/preset/settings data matches the native export. Second-computer import remains untested.
