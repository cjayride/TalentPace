using System;
using System.Reflection;
using HarmonyLib;

namespace Cjayride.TalentPace
{
    internal static class TalentTreeEffectScale
    {
        static Assembly _talentTreeAssembly;
        static MethodInfo _runtimeGet;
        static PropertyInfo _cacheProp;
        static MethodInfo _cacheGetFloat;
        static MethodInfo _cacheHasFlag;
        static bool _reflected;
        static bool _reflectFailed;

        public static void Apply(Harmony harmony)
        {
            Type passives = AccessTools.TypeByName("Core.SE_TalentPassives");
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
            Type type = AccessTools.TypeByName(typeName);
            MethodInfo method = type == null ? null : AccessTools.DeclaredMethod(type, methodName);
            if (method == null)
            {
                return;
            }

            harmony.Patch(method, postfix: new HarmonyMethod(typeof(TalentTreeEffectScale), postfix));
        }

        static bool ScalingOn()
        {
            return Plugin.EnableEffectScaling.Value;
        }

        static bool IsTalentTreeEffect(StatusEffect effect)
        {
            return effect != null && _talentTreeAssembly != null && effect.GetType().Assembly == _talentTreeAssembly;
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
            if (!ScalingOn() || stats == null || !IsTalentTreeEffect(stats))
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

        static void BeforeModifyAttack(ref HitData hitData, out float __state)
        {
            __state = hitData != null ? hitData.GetTotalDamage() : 0f;
        }

        static void AfterModifyAttack(ref HitData hitData, float __state)
        {
            if (!ScalingOn() || hitData == null)
            {
                return;
            }

            float scale = Plugin.DamageEffectScale.Value;
            if (scale >= 0.999f)
            {
                return;
            }

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
            if (!ScalingOn() || hit == null)
            {
                return;
            }

            float scale = Plugin.DamageEffectScale.Value;
            if (scale >= 0.999f)
            {
                return;
            }

            Player player = hit.GetAttacker() as Player;
            if (player == null || !HasFlag(player, flag))
            {
                return;
            }

            float bonus = CacheFloat(player, key);
            if (bonus <= 0f)
            {
                return;
            }

            hit.ApplyModifier((1f + bonus * scale) / (1f + bonus));
        }

        static bool HasFlag(Player player, string flag)
        {
            object cache = PlayerCache(player);
            return cache != null && (bool)_cacheHasFlag.Invoke(cache, new object[] { flag });
        }

        static float CacheFloat(Player player, string key)
        {
            object cache = PlayerCache(player);
            return cache == null ? 0f : (float)_cacheGetFloat.Invoke(cache, new object[] { key, 0f });
        }

        static object PlayerCache(Player player)
        {
            if (!EnsureCache() || player == null)
            {
                return null;
            }

            object runtime = _runtimeGet.Invoke(null, new object[] { player });
            return runtime == null ? null : _cacheProp.GetValue(runtime, null);
        }

        static bool EnsureCache()
        {
            if (_reflected)
            {
                return !_reflectFailed;
            }

            _reflected = true;
            Type service = AccessTools.TypeByName("Core.TalentRuntimeService");
            Type runtime = AccessTools.TypeByName("Core.TalentRuntime");
            Type cache = AccessTools.TypeByName("Core.TalentCache");
            _runtimeGet = service == null ? null : AccessTools.DeclaredMethod(service, "Get", new[] { typeof(Player) });
            _cacheProp = runtime == null ? null : AccessTools.DeclaredProperty(runtime, "Cache");
            _cacheGetFloat = cache == null ? null : AccessTools.DeclaredMethod(cache, "GetFloat", new[] { typeof(string), typeof(float) });
            _cacheHasFlag = cache == null ? null : AccessTools.DeclaredMethod(cache, "HasFlag", new[] { typeof(string) });
            _reflectFailed = _runtimeGet == null || _cacheProp == null || _cacheGetFloat == null || _cacheHasFlag == null;
            if (_reflectFailed)
            {
                Plugin.Log("Talent Pace could not bind Talent Tree cache. Extra damage scaling hooks are off.");
            }

            return !_reflectFailed;
        }
    }
}
