# Stonewards Particle Optimizer

**Reduce visual clutter from mining and destruction in Stonewards.** Particle Optimizer filters selected local particle effects so digging and breaking objects can look cleaner, with the potential to reduce particle-related rendering work.

## ✨ Features

- **Mining effects** — independently filter particle bursts from hitting and breaking terrain.
- **Destructible props** — filter recognized hit particles from destructible objects.
- **Wall destruction** — filter particle effects triggered by breaking wall sections.
- **Three modes per category** — keep vanilla effects, show 25% of spawn events, or suppress them.
- **Client-side design** — filters visual-effect calls locally without intentionally changing mining, damage, or resource logic.

## 🎮 How to use

The mod works automatically when Stonewards starts with BepInEx. There are no hotkeys or in-game menus.

By default, all four effect categories are set to `Disabled`. To restore or reduce a particular effect, change its setting in the BepInEx configuration file.

## ⚙️ Configuration

The configuration file is created after the first launch:

`BepInEx/config/com.thereisnouser.stonewards.particleoptimizer.cfg`

| Setting | Effect | Default |
| --- | --- | --- |
| `DigHit` | Mining hits that do not break terrain | `Disabled` |
| `DigBreak` | Terrain and rock breaking effects | `Disabled` |
| `PropHit` | Hit effects from recognized destructible props | `Disabled` |
| `WallBreak` | Wall destruction particles | `Disabled` |

Each setting accepts:

- `Vanilla` — leave the effect unchanged.
- `Reduced` — allow one out of every four spawn events for a given effect ID (25% of events, **not** 25% of particles per burst).
- `Disabled` — skip the selected effect's spawn call.

Advanced options:

- `ExtraBlockedVfxIds` — comma-, semicolon-, or newline-separated effect IDs to suppress.
- `DebugLogging` — log the first suppressed occurrence of each effect ID; off by default.

Edit the configuration while the game is closed for predictable results.

## 📦 Installation

The repository currently provides source code and a build/install script. No published Thunderstore package is linked yet.

To build and install it into a BepInEx-enabled Stonewards profile, use **Windows PowerShell**, a **.NET SDK**, a local **Stonewards** installation, and **BepInEx 5**.

```powershell
git clone https://github.com/thereisnouser/stonewards-particle-optimizer.git
cd stonewards-particle-optimizer

$gameDir = Read-Host "Stonewards installation directory"
$profileDir = Read-Host "BepInEx-enabled mod profile directory"

powershell -NoProfile -ExecutionPolicy Bypass -File .\build-and-install.ps1 -GameDir $gameDir -ProfileDir $profileDir
```

The script builds the plugin and copies `Stonewards.ParticleOptimizer.dll` into:

`<profile>/BepInEx/plugins/StonewardsParticleOptimizer/`

Launch Stonewards through the profile you installed the plugin into.

## 🔧 Compatibility

- **Game:** Stonewards; an exact supported game version has not been established.
- **Mod loader:** BepInEx 5.
- **Multiplayer:** Designed as a client-side visual filter. Host/client multiplayer behavior has not been independently verified.

## ⚠️ Known limitations

- Only recognized particle effect IDs and the hooked wall effect are filtered; other effects may remain visible.
- Some game systems may share effect IDs, so suppressing a mining effect can also hide that same effect elsewhere.
- Destructible props loaded after the scene scans may not be recognized automatically.
- FPS improvements have not been benchmarked and depend on whether particles are the performance bottleneck.

## 🛠️ Development

The C# plugin uses Harmony to intercept selected local VFX calls. It discovers mining effect IDs from `DiggingManager` profiles, reads recognized `DestructibleProp` effect IDs during scene loading, filters received `PlayerFX` particle spawns, and separately handles `WallPartAnimator` destruction effects. These patches target visual effects rather than the gameplay commands.

The project targets `netstandard2.1`. Build and installation are handled by the repository's `build-and-install.ps1` script.

## 🔗 Links

[Source code](https://github.com/thereisnouser/stonewards-particle-optimizer) · [Report an issue](https://github.com/thereisnouser/stonewards-particle-optimizer/issues) · [Changelog](CHANGELOG.md)

---

*Made by thereisnouser*
