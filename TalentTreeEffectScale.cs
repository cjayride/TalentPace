using System;
using System.Reflection;
using HarmonyLib;

namespace Cjayride.TalentPace
{
    internal static class TalentTreeEffectScale
    {
        static Assembly _talentTreeAssembly;
        static MethodInfo _cacheGetFloat;
        static MethodInfo _cacheHasFlag;
        static bool _reflected;
        static bool _reflectFailed;

        // Damage hits run this code many times per second. The Talent Tree cache is
        // read through delegates bound to the current cache object, so the steady
        // state path does no reflection invoke and allocates nothing.
        static object _cacheOwner;
        static Func<string, bool> _hasFlag;
        static Func<string, float, float> _getFloat;

        public static void Apply(Harmony harmony)
        {
            Type passives = TT.Type("Core.SE_TalentPassives");
            if (passives == null)
            {
                Plugin.Log("Talent Tree passives were not found. Effect scaling is off.");
                return;
            }

            _talentTreeAssembly = passives.Assembly;

            MethodInfo sync = AccessTools.DeclaredMethod(passives, "SyncFromCache");
            MethodInfo attack = AccessTools.DeclaredMethod(passives, "ModifyAttack");
            MethodInfo setup = AccessTools.DeclaredMethod(typeof(SE_Stats), nameof(SE_Stats.Setup));

            if (sync != null)
            {
                harmony.Patch(sync, postfix: new HarmonyMethod(typeof(TalentTreeEffectScale), nameof(AfterSyncFromCache)));
            }

            // Kept for behaviour parity: SE_Stats.Setup runs on every status-effect
            // add. The postfix is now two cheap guards (config bool, then assembly
            // identity) before any real work, and scaling is off by default.
            if (setup != null)
            {
                harmony.Patch(setup, postfix: new HarmonyMethod(typeof(TalentTreeEffectScale), nameof(AfterSetup)));
            }

            TryPatch(harmony, "Talents.Warcaller.Effects.SE_WarcallerMarchBuff", "SetLevel", nameof(AfterTalentSetLevel));
            TryPatch(harmony, "Talents.Warcaller.Effects.SE_WarcallerSeafarerCurrents", "SetLevel", nameof(AfterTalentSetLevel));

            if (attack != null)
            {
                harmony.Patch(
                    attack,
                    prefix: new HarmonyMethod(typeof(TalentTreeEffectScale), nameof(BeforeModifyAttack)),
                    postfix: new HarmonyMethod(typeof(TalentTreeEffectScale), nameof(AfterModifyAttack)));
            }

            TryPatch(harmony, "Talents.Berserker.Patches.BerserkerGlassCannonPatch", "OnCharacterDamagePrefix", nameof(AfterGlassCannonDamage));
            TryPatch(harmony, "Talents.Berserker.Patches.BerserkerExecutionPatch", "OnCharacterDamagePrefix", nameof(AfterExecutionDamage));
        }

        static void TryPatch(Harmony harmony, string typeName, string methodName, string postfix)
        {
            Type type = TT.Type(typeName);
            MethodInfo method = type == null ? null : AccessTools.DeclaredMethod(type, methodName);
            if (method == null)
            {
                return;
            }

            harmony.Patch(method, postfix: new HarmonyMethod(typeof(TalentTreeEffectScale), postfix));
        }

        static bool IsTalentTreeEffect(SE_Stats stats)
        {
            return _talentTreeAssembly != null && stats.GetType().Assembly == _talentTreeAssembly;
        }

        static void AfterSyncFromCache(SE_Stats __instance)
        {
            ScaleMovementFields(__instance);
        }

        static void AfterSetup(SE_Stats __instance)
        {
            ScaleMovementFields(__instance);
        }

        static void AfterTalentSetLevel(SE_Stats __instance)
        {
            ScaleMovementFields(__instance);
        }

        static void ScaleMovementFields(SE_Stats stats)
        {
            if (stats == null || !Plugin.EnableEffectScaling.Value || !IsTalentTreeEffect(stats))
            {
                return;
            }

            float scale = Plugin.MovementEffectScale.Value;
            if (scale >= 0.999f)
            {
                return;
            }

            if (stats.m_speedModifier > 0f)
            {
                stats.m_speedModifier *= scale;
            }

            if (stats.m_swimSpeedModifier > 0f)
            {
                stats.m_swimSpeedModifier *= scale;
            }

            if (stats.m_runStaminaDrainModifier < 0f)
            {
                stats.m_runStaminaDrainModifier *= scale;
            }
        }

        // __state < 0 means "scaling off", which keeps the postfix from doing any work
        // and skips the damage read entirely when the feature is disabled.
        static void BeforeModifyAttack(ref HitData hitData, out float __state)
        {
            if (hitData == null || !Plugin.EnableEffectScaling.Value
                || Plugin.DamageEffectScale.Value >= 0.999f)
            {
                __state = -1f;
                return;
            }

            __state = hitData.GetTotalDamage();
        }

        static void AfterModifyAttack(ref HitData hitData, float __state)
        {
            if (__state < 0f || hitData == null)
            {
                return;
            }

            float scale = Plugin.DamageEffectScale.Value;
            float now = hitData.GetTotalDamage();
            float extra = now - __state;
            if (extra <= 0.0001f || now <= 0.0001f)
            {
                return;
            }

            hitData.m_damage.Modify((__state + extra * scale) / now);
        }

        static void AfterGlassCannonDamage(HitData hit)
        {
            RescaleBonus(hit, "berserker_glass_cannon", "berserker_glass_cannon_dmg_pct");
        }

        static void AfterExecutionDamage(Character instance, HitData hit)
        {
            if (instance == null || instance.IsPlayer() || instance.GetHealthPercentage() >= 0.3f)
            {
                return;
            }

            RescaleBonus(hit, "berserker_execution", "berserker_execution_dmg_pct");
        }

        static void RescaleBonus(HitData hit, string flag, string key)
        {
            if (hit == null || !Plugin.EnableEffectScaling.Value)
            {
                return;
            }

            float scale = Plugin.DamageEffectScale.Value;
            if (scale >= 0.999f)
            {
                return;
            }

            Player player = hit.GetAttacker() as Player;
            if (player == null)
            {
                return;
            }

            Func<string, bool> hasFlag;
            Func<string, float, float> getFloat;
            if (!TryCacheAccess(player, out hasFlag, out getFloat))
            {
                return;
            }

            if (!hasFlag(flag))
            {
                return;
            }

            float bonus = getFloat(key, 0f);
            if (bonus <= 0f)
            {
                return;
            }

            hit.ApplyModifier((1f + bonus * scale) / (1f + bonus));
        }

        // Bound delegates are rebuilt only when the Talent Tree swaps its cache
        // object, which happens on a talent rebuild, not per hit.
        static bool TryCacheAccess(Player player, out Func<string, bool> hasFlag, out Func<string, float, float> getFloat)
        {
            hasFlag = null;
            getFloat = null;
            if (!EnsureCache())
            {
                return false;
            }

            object cache = TT.CacheFor(player);
            if (cache == null)
            {
                return false;
            }

            if (cache != _cacheOwner)
            {
                _hasFlag = (Func<string, bool>)Delegate.CreateDelegate(typeof(Func<string, bool>), cache, _cacheHasFlag);
                _getFloat = (Func<string, float, float>)Delegate.CreateDelegate(typeof(Func<string, float, float>), cache, _cacheGetFloat);
                _cacheOwner = cache;
            }

            if (_hasFlag == null || _getFloat == null)
            {
                return false;
            }

            hasFlag = _hasFlag;
            getFloat = _getFloat;
            return true;
        }

        static bool EnsureCache()
        {
            if (_reflected)
            {
                return !_reflectFailed;
            }

            _reflected = true;
            Type cache = TT.Type("Core.TalentCache");
            _cacheGetFloat = cache == null ? null : AccessTools.DeclaredMethod(cache, "GetFloat", new[] { typeof(string), typeof(float) });
            _cacheHasFlag = cache == null ? null : AccessTools.DeclaredMethod(cache, "HasFlag", new[] { typeof(string) });
            _reflectFailed = _cacheGetFloat == null || _cacheHasFlag == null;
            if (_reflectFailed)
            {
                Plugin.Log("Talent Pace could not bind Talent Tree cache. Extra damage scaling hooks are off.");
            }

            return !_reflectFailed;
        }
    }
}
