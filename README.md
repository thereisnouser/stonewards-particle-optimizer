# Stonewards Particle Optimizer

Client-side BepInEx 5 mod for suppressing selected mining, rock breaking, destructible-prop hit, and wall impact particle effects in Stonewards.

**Version:** 0.1.0 (initial source release; requires in-game validation)

## How it works

- Harmony patches `PlayerFX.UserCode_RpcGetParticleAndRelease__String__Vector3__Quaternion__Vector3__Single` after Mirror has delivered an RPC to the **local client**, but before the client obtains a pooled particle system. The network command, terrain change, resource spawning, and other players remain untouched.
- Mining VFX names come from `DiggingManager.DiggingSettingsSO.digMatterProfiles` (`nonDestructionParticleID` and `rockImpactParticleID`).
- Prop hit VFX names come from loaded `DestructibleProp.particleID` fields. These IDs are refreshed a few times during scene loading, **not** on every frame.
- Wall impact particle effects are skipped directly in `WallPartAnimator.PlayDestroyFx`. Physical chunk animations are untouched.

**Limitations:** An effect used by both mining and another action can be suppressed in both contexts (IDs are not unique to a context). Prop prefabs instantiated late may be missed by the one-time scans. Further runtime testing is required to confirm the real-world FPS effect and coverage. This mod cannot fix performance issues unrelated to the selected VFX.

## Build & install

Requires: Windows PowerShell, .NET SDK, game files, a mod profile containing BepInEx 5 (`BepInEx/core/BepInEx.dll` and `0Harmony.dll`).

From PowerShell in the repository directory:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-and-install.ps1 `
    -GameDir 'F:\Steam\steamapps\common\Stonewards' `
    -ProfileDir 'C:\path\to\r2modman\profiles\your-profile'
```

Replace the profile path with the real r2modman profile path. The DLL is installed to:

`<ProfileDir>\BepInEx\plugins\StonewardsParticleOptimizer\Stonewards.ParticleOptimizer.dll`

Run the game, then open the BepInEx log and look for:

`Stonewards Particle Optimizer v0.1.0 initialized`

The config is generated after first game launch:

`<ProfileDir>\BepInEx\config\com.thereisnouser.stonewards.particleoptimizer.cfg`

## Configuration

Each of `DigHit`, `DigBreak`, `PropHit`, `WallBreak` supports:

- `Vanilla`: don't suppress the effect.
- `Reduced`: keep 1 out of every 4 spawn events per VFX ID (25%).
- `Disabled`: completely skip the chosen effect's particle spawn.

The default for all four is `Disabled`. Combat effects and loot are not intentionally filtered. `ExtraBlockedVfxIds` (optional) allows comma-separated IDs for effects not automatically discovered; these extras always use Disabled mode.

Change the configuration before launching the game to guarantee the filter has the new values. The mod does not modify save files or game assemblies.

## Validation checklist

1. With all options Disabled, hit terrain without fully mining it: hit dust should disappear.
2. Fully mine rock: break particles should disappear while resources and terrain changes still work.
3. Hit a destructible prop: its particles should disappear but sound, damage, and drops still work.
4. Observe a wall chunk breaking: particle burst should disappear; wall physics and chunks should behave normally.
5. Test as a non-host client in multiplayer: the host and other clients should see their normal effects.
6. Toggle all categories to Vanilla; compare appearance and frame-time in the same scene.

**Status:** Inspected the Assembly-CSharp.dll supplied 2026-10-24 and identified method calls/fields. A Windows .NET build and live Stonewards session were not available in this environment, so this is a source release, not a claimed tested DLL.
