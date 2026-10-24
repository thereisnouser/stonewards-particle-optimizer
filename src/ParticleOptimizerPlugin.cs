using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StonewardsParticleOptimizer
{
    public enum EffectMode
    {
        Vanilla,
        Reduced,
        Disabled
    }

    [BepInPlugin(PluginId, PluginName, PluginVersion)]
    public sealed class ParticleOptimizerPlugin : BaseUnityPlugin
    {
        public const string PluginId = "com.thereisnouser.stonewards.particleoptimizer";
        public const string PluginName = "Stonewards Particle Optimizer";
        public const string PluginVersion = "0.1.0";

        private enum EffectKind
        {
            DigHit,
            DigBreak,
            PropHit,
            Extra
        }

        internal static ParticleOptimizerPlugin Instance;
        private Harmony _harmony;
        private ConfigEntry<EffectMode> _digHitMode;
        private ConfigEntry<EffectMode> _digBreakMode;
        private ConfigEntry<EffectMode> _propHitMode;
        private ConfigEntry<EffectMode> _wallBreakMode;
        private ConfigEntry<string> _extraVfxIds;
        private ConfigEntry<bool> _debugLogging;

        private readonly Dictionary<string, EffectKind> _effectIds =
            new Dictionary<string, EffectKind>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _effectCounters =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _loggedIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly FieldInfo PropParticleIdField =
            AccessTools.Field(typeof(DestructibleProp), "particleID");

        private void Awake()
        {
            Instance = this;
            _digHitMode = Config.Bind("01 - Mining", "DigHit", EffectMode.Disabled,
                "Particles from hits that do not yet destroy a terrain block.");
            _digBreakMode = Config.Bind("01 - Mining", "DigBreak", EffectMode.Disabled,
                "Particles emitted when terrain/rock is actually broken.");
            _propHitMode = Config.Bind("02 - Destructible props", "PropHit", EffectMode.Disabled,
                "Hit particles from DestructibleProp (does not disable loot/sound/damage).");
            _wallBreakMode = Config.Bind("03 - Walls", "WallBreak", EffectMode.Disabled,
                "Particles emitted when wall chunks break. Does not change the chunks themselves.");
            _extraVfxIds = Config.Bind("04 - Advanced", "ExtraBlockedVfxIds", "",
                "Optional additional VFX IDs to suppress, comma/semicolon/newline separated.");
            _debugLogging = Config.Bind("04 - Advanced", "DebugLogging", false,
                "Log VFX IDs when first suppressed. Useful for troubleshooting.");

            _harmony = new Harmony(PluginId);
            PatchRequired(
                AccessTools.Method(typeof(PlayerFX),
                    "UserCode_RpcGetParticleAndRelease__String__Vector3__Quaternion__Vector3__Single"),
                nameof(FilterReceivedParticlePrefix), null,
                "client-side PlayerFX VFX receiver");

            PatchOptional(
                AccessTools.Method(typeof(DiggingManager), "OnStartClient"),
                null, nameof(DiggingManagerStartedPostfix),
                "DiggingManager.OnStartClient");

            PatchOptional(
                AccessTools.Method(typeof(WallPartAnimator), "PlayDestroyFx"),
                nameof(WallDestroyFxPrefix), null,
                "WallPartAnimator.PlayDestroyFx");

            SceneManager.sceneLoaded += OnSceneLoaded;
            RefreshKnownEffects();
            Logger.LogInfo(PluginName + " v" + PluginVersion + " initialized (client-side VFX filtering).");
        }

        private void PatchRequired(MethodInfo original, string prefix, string postfix, string description)
        {
            if (original == null)
            {
                Logger.LogError("Required game method not found: " + description +
                    ". VFX filtering will not be active; this game version may be incompatible.");
                return;
            }
            PatchMethod(original, prefix, postfix, description);
        }

        private void PatchOptional(MethodInfo original, string prefix, string postfix, string description)
        {
            if (original == null)
            {
                Logger.LogWarning("Optional game method not found: " + description);
                return;
            }
            PatchMethod(original, prefix, postfix, description);
        }

        private void PatchMethod(MethodInfo original, string prefix, string postfix, string description)
        {
            try
            {
                var patchType = typeof(ParticleOptimizerPlugin);
                _harmony.Patch(original,
                    prefix == null ? null : new HarmonyMethod(AccessTools.Method(patchType, prefix)),
                    postfix == null ? null : new HarmonyMethod(AccessTools.Method(patchType, postfix)));
                Logger.LogInfo("Patched " + description);
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to patch " + description + ": " + ex);
            }
        }

        // Runs on EVERY client after Mirror has received the VFX RPC, BEFORE the game
        // gets a particle from its pool. Skipping this method never blocks CmdDigGround,
        // CmdTrySpawnResource, or the server-side RPC to other players.
        private static bool FilterReceivedParticlePrefix(string __0)
        {
            return Instance == null || Instance.ShouldShowParticle(__0);
        }

        private static void DiggingManagerStartedPostfix(DiggingManager __instance)
        {
            if (Instance != null)
                Instance.AddDigMatterIds(__instance);
        }

        // This game method ONLY activates/plays an already allocated wall VFX particle.
        // It does not perform wall damage, physics, loot generation, or chunk removal.
        private static bool WallDestroyFxPrefix()
        {
            return Instance == null || Instance.Permit("#wall-fx", Instance._wallBreakMode.Value);
        }

        private bool ShouldShowParticle(string id)
        {
            if (string.IsNullOrEmpty(id)) return true;
            EffectKind kind;
            if (!_effectIds.TryGetValue(id, out kind)) return true;

            EffectMode mode;
            switch (kind)
            {
                case EffectKind.DigHit: mode = _digHitMode.Value; break;
                case EffectKind.DigBreak: mode = _digBreakMode.Value; break;
                case EffectKind.PropHit: mode = _propHitMode.Value; break;
                default: mode = EffectMode.Disabled; break;
            }

            bool show = Permit(id, mode);
            if (!show && _debugLogging.Value && _loggedIds.Add(id))
                Logger.LogInfo("Suppressed VFX: " + id + " (" + kind + ", " + mode + ")");
            return show;
        }

        // Reduced mode keeps 1 of each 4 spawns (25% spawn events, not emitter density).
        private bool Permit(string id, EffectMode mode)
        {
            if (mode == EffectMode.Vanilla) return true;
            if (mode == EffectMode.Disabled) return false;
            int count;
            _effectCounters.TryGetValue(id, out count);
            _effectCounters[id] = count + 1;
            return (count & 3) == 0;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshKnownEffects();
            StartCoroutine(RefreshAfterLoad());
        }

        private IEnumerator RefreshAfterLoad()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            RefreshKnownEffects();
            yield return new WaitForSecondsRealtime(4f);
            RefreshKnownEffects();
        }

        private void RefreshKnownEffects()
        {
            _effectIds.Clear();
            _effectCounters.Clear();
            _loggedIds.Clear();

            // Additional IDs can be used if a new game build introduces VFX that
            // are not represented by DigMatterProfileSO or DestructibleProp.
            if (!string.IsNullOrEmpty(_extraVfxIds.Value))
            {
                foreach (string id in _extraVfxIds.Value.Split(
                    new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    Register(id.Trim(), EffectKind.Extra);
            }

            try
            {
                AddDigMatterIds(DiggingManager.Instance);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not load mining VFX IDs: " + ex.Message);
            }

            try
            {
                // This is intentionally done only during loading, NEVER every frame.
                // FindObjectsOfTypeAll also covers loaded inactive destructible props.
                var props = Resources.FindObjectsOfTypeAll<DestructibleProp>();
                if (PropParticleIdField != null)
                {
                    foreach (var prop in props)
                    {
                        if (prop == null) continue;
                        Register(PropParticleIdField.GetValue(prop) as string, EffectKind.PropHit);
                    }
                }
                else
                {
                    Logger.LogWarning("DestructibleProp.particleID field not found.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not load destructible prop VFX IDs: " + ex.Message);
            }

            Logger.LogDebug("Registered " + _effectIds.Count + " local VFX IDs for filtering.");
        }

        private void AddDigMatterIds(DiggingManager manager)
        {
            if (manager == null || manager.DiggingSettingsSO == null ||
                manager.DiggingSettingsSO.digMatterProfiles == null)
                return;

            foreach (var profile in manager.DiggingSettingsSO.digMatterProfiles)
            {
                if (profile == null) continue;
                Register(profile.nonDestructionParticleID, EffectKind.DigHit);
                Register(profile.rockImpactParticleID, EffectKind.DigBreak);
            }
        }

        private void Register(string id, EffectKind kind)
        {
            if (string.IsNullOrEmpty(id)) return;
            // Keep first category on collisions: dig IDs take precedence only when
            // first registered. Extra IDs always keep their explicit suppression.
            if (!_effectIds.ContainsKey(id))
                _effectIds.Add(id, kind);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (_harmony != null)
                _harmony.UnpatchSelf();
            if (Instance == this) Instance = null;
        }
    }
}
