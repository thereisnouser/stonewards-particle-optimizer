# Stonewards Particle Optimizer

**Keep mining and destruction scenes cleaner by reducing or disabling selected particle effects in Stonewards.** The mod filters local visual effects rather than intentionally changing how digging or destruction works.

## ✨ Features

- **Cleaner mining** — filter particles from terrain hits and rock breaking.
- **Less destruction clutter** — filter recognized prop-hit and wall-break particles.
- **Adjustable effects** — choose vanilla, reduced, or disabled particles for each category.
- **Automatic operation** — no hotkeys or in-game menu required.

## 🎮 How to use

Launch Stonewards with the mod enabled. It automatically filters the selected visual effects; all four categories are disabled by default.

## ⚙️ Configuration

After the first launch, edit:

`BepInEx/config/com.thereisnouser.stonewards.particleoptimizer.cfg`

The settings `DigHit`, `DigBreak`, `PropHit`, and `WallBreak` each support:

- `Vanilla` — show normal effects.
- `Reduced` — allow 25% of effect spawn events.
- `Disabled` — suppress the selected effects (default).

For best results, edit the file while the game is closed.

## 📦 Installation

Install the package using **r2modman** or another compatible Thunderstore mod manager, then launch **Stonewards** through the manager. **BepInEx 5** is required.

## 🔧 Compatibility

- **Game:** Stonewards; exact supported game version not yet established.
- **Mod loader:** BepInEx 5.
- **Multiplayer:** Designed for client-side visual filtering; host/client behavior has not been independently verified.

## ⚠️ Known limitations

- Not every particle effect is covered. Some effects may share IDs and be hidden in other situations.
- FPS improvements vary by system and scene and have not been benchmarked.

## 🔗 Links

[GitHub](https://github.com/thereisnouser/stonewards-particle-optimizer) · [Report an issue](https://github.com/thereisnouser/stonewards-particle-optimizer/issues) · [Changelog](https://github.com/thereisnouser/stonewards-particle-optimizer/blob/main/CHANGELOG.md)

---

*Made by thereisnouser*
