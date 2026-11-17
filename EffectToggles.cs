using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Cjayride.TalentPace
{
    /// <summary>
    /// Master switches so players can tame without talent procs or EpicLoot shardstone
    /// bonuses killing the creature. Both default on. The Talent Tree window buttons
    /// write the same config keys as F1.
    /// </summary>
    internal static class EffectToggles
    {
        internal static ConfigEntry<bool> TalentEffects;
        internal static ConfigEntry<bool> ShardstoneEffects;

        static bool _epicLoot;
        static MethodInfo _syncToPlayer;
        static MethodInfo _getRuntime;
        static MethodInfo _resetToDefault;

        internal static bool EpicLootPresent
        {
            get { return _epicLoot; }
        }

        internal static bool TalentEffectsOn
        {
            get { return TalentEffects == null || TalentEffects.Value; }
        }

        internal static bool ShardstoneEffectsOn
        {
            get { return ShardstoneEffects == null || ShardstoneEffects.Value; }
        }

        public static void Bind(ConfigFile config)
        {
            TalentEffects = config.Bind(
                "Effect toggles",
                "Enable talent effects",
                true,
                "When false, Talent Tree combat and aura effects do not apply. Use this while taming so talent procs do not kill the creature. Points and the tree itself stay as they are. On by default.");
            ShardstoneEffects = config.Bind(
                "Effect toggles",
                "Enable shardstone effects",
                true,
                "When false, EpicLoot socketed shardstone bonuses do not apply (item enchants are unchanged). The button in the talent window is shown only if EpicLoot is installed. On by default.");
        }

        public static void Apply(Harmony harmony)
        {
            _epicLoot = AccessTools.TypeByName("EpicLoot.MagicItem") != null;

            Type passives = TT.Type("Core.SE_TalentPassives");
            Type passivesService = TT.Type("Core.TalentPassivesService");
            Type runtimeService = TT.Type("Core.TalentRuntimeService");
            Type runtime = TT.Type("Core.TalentRuntime");
            _resetToDefault = passives == null ? null : AccessTools.DeclaredMethod(passives, "ResetToDefault");
            _syncToPlayer = passivesService == null || runtime == null
                ? null
                : AccessTools.DeclaredMethod(passivesService, "SyncToPlayer", new[] { runtime });
            _getRuntime = runtimeService == null
                ? null
                : AccessTools.DeclaredMethod(runtimeService, "Get", new[] { typeof(Player) });

            if (passives != null)
            {
                PatchIfFound(harmony, passives, "SyncFromCache", nameof(AfterSyncFromCache), postfix: true);
                PatchIfFound(harmony, passives, "ModifyAttack", nameof(KeepIfTalentsOn), postfix: false);
                PatchIfFound(harmony, passives, "UpdateStatusEffect", nameof(KeepIfTalentsOn), postfix: false);
                PatchIfFound(harmony, passives, "ModifyAttackStaminaUsage", nameof(KeepIfTalentsOn), postfix: false);
            }

            if (_syncToPlayer != null)
            {
                harmony.Patch(_syncToPlayer, postfix: new HarmonyMethod(typeof(EffectToggles), nameof(AfterSyncToPlayer)));
            }

            GateTalentHub(harmony, "Patches.CharacterDamagePatches");
            // ApplyDamage hub Prefix returns bool. Skipping it with KeepIfTalentsOn
            // made Harmony treat the prefix result as false and skip Character.ApplyDamage
            // entirely (0 weapon damage). Force __result=true so base damage still applies.
            GateTalentHub(harmony, "Patches.CharacterApplyDamagePatches", boolPrefixAllowsOriginal: true);
            GateTalentHub(harmony, "Patches.CharacterHealPatches");
            GateTalentHub(harmony, "Patches.PlayerUpdatePatches");
            GateTalentHub(harmony, "Patches.HumanoidBlockAttackPatches");
            GateTalentHub(harmony, "Patches.PlayerGetTotalFoodValuePatches");
            // Direct hook: Immolation ticks from Player.Update, not Character.Damage.
            Type immolation = TT.Type("Talents.Bulwark.BulwarkImmolationPatch");
            if (immolation != null)
            {
                PatchDeclared(harmony, immolation, "OnPlayerUpdatePostfix", nameof(KeepIfTalentsOn));
            }
            else
            {
                Plugin.Log("Talent Pace: Bulwark Immolation patch was not found; Player.Update hub is still gated.");
            }

            MethodInfo[] addSe = typeof(SEMan).GetMethods(BindingFlags.Instance | BindingFlags.Public);
            for (int i = 0; i < addSe.Length; i++)
            {
                if (addSe[i].Name != "AddStatusEffect")
                {
                    continue;
                }

                ParameterInfo[] args = addSe[i].GetParameters();
                if (args.Length > 0 && args[0].ParameterType == typeof(StatusEffect))
                {
                    harmony.Patch(addSe[i], prefix: new HarmonyMethod(typeof(EffectToggles), nameof(BeforeAddStatusEffect)));
                }
                else if (args.Length > 0 && args[0].ParameterType == typeof(int))
                {
                    harmony.Patch(addSe[i], prefix: new HarmonyMethod(typeof(EffectToggles), nameof(BeforeAddStatusEffectHash)));
                }
            }

            if (_epicLoot)
            {
                Type magicItem = AccessTools.TypeByName("EpicLoot.MagicItem");
                if (magicItem != null)
                {
                    MethodInfo hasEffect = AccessTools.DeclaredMethod(magicItem, "HasEffect");
                    MethodInfo sum = AccessTools.DeclaredMethod(magicItem, "SumEffectValue");
                    MethodInfo getEffects = AccessTools.DeclaredMethod(magicItem, "GetEffects");
                    MethodInfo total = AccessTools.DeclaredMethod(magicItem, "GetTotalEffectValue");
                    HarmonyMethod strip = new HarmonyMethod(typeof(EffectToggles), nameof(StripSocketedFlag));
                    if (hasEffect != null)
                    {
                        harmony.Patch(hasEffect, prefix: strip);
                    }

                    if (sum != null)
                    {
                        harmony.Patch(sum, prefix: strip);
                    }

                    if (getEffects != null)
                    {
                        harmony.Patch(getEffects, prefix: strip);
                    }

                    if (total != null)
                    {
                        harmony.Patch(total, prefix: strip);
                    }

                    Plugin.Log("Talent Pace: EpicLoot found — shardstone effect toggle is available.");
                }
            }
            else
            {
                Plugin.Log("Talent Pace: EpicLoot not installed — shardstone effect button is hidden.");
            }
        }

        static void PatchIfFound(Harmony harmony, Type type, string method, string patch, bool postfix)
        {
            MethodInfo target = AccessTools.DeclaredMethod(type, method);
            if (target == null)
            {
                return;
            }

            HarmonyMethod hook = new HarmonyMethod(typeof(EffectToggles), patch);
            if (postfix)
            {
                harmony.Patch(target, postfix: hook);
            }
            else
            {
                harmony.Patch(target, prefix: hook);
            }
        }

        static void GateTalentHub(Harmony harmony, string typeName, bool boolPrefixAllowsOriginal = false)
        {
            Type type = TT.Type(typeName);
            if (type == null)
            {
                Plugin.Log("Talent Pace: talent hub " + typeName + " was not found.");
                return;
            }

            // Bool-returning Prefixes (e.g. Character.ApplyDamage) must not be skipped
            // with a bare false — Harmony would skip the original game method too.
            string prefix = boolPrefixAllowsOriginal
                ? nameof(KeepBoolPrefixAllowOriginalIfTalentsOff)
                : nameof(KeepIfTalentsOn);
            PatchDeclared(harmony, type, "Prefix", prefix);
            PatchDeclared(harmony, type, "Postfix", nameof(KeepIfTalentsOn));
        }

        static void PatchDeclared(Harmony harmony, Type type, string method, string prefix)
        {
            if (type == null)
            {
                return;
            }

            MethodInfo target = AccessTools.DeclaredMethod(type, method);
            if (target == null)
            {
                return;
            }

            harmony.Patch(target, prefix: new HarmonyMethod(typeof(EffectToggles), prefix));
        }

        static bool KeepIfTalentsOn()
        {
            return TalentEffectsOn;
        }

        // Used only on bool Prefix hubs. When talents are off, skip the talent logic
        // but report true so Character.ApplyDamage (etc.) still runs.
        static bool KeepBoolPrefixAllowOriginalIfTalentsOff(ref bool __result)
        {
            if (TalentEffectsOn)
            {
                return true;
            }

            __result = true;
            return false;
        }

        static void AfterSyncFromCache(object __instance)
        {
            if (!TalentEffectsOn)
            {
                ResetPassives(__instance);
            }
        }

        static void AfterSyncToPlayer()
        {
            if (!TalentEffectsOn)
            {
                ApplyToLocalPlayer();
            }
        }

        static bool BeforeAddStatusEffect(StatusEffect statusEffect)
        {
            if (TalentEffectsOn || statusEffect == null || TT.Assembly == null)
            {
                return true;
            }

            if (statusEffect.GetType().Assembly != TT.Assembly)
            {
                return true;
            }

            return statusEffect.name == "TT_TalentPassives";
        }

        static bool BeforeAddStatusEffectHash(int nameHash)
        {
            if (TalentEffectsOn || ObjectDB.instance == null)
            {
                return true;
            }

            StatusEffect se = ObjectDB.instance.GetStatusEffect(nameHash);
            if (se == null)
            {
                return true;
            }

            if (se.name == "TT_TalentPassives")
            {
                return true;
            }

            return TT.Assembly == null || se.GetType().Assembly != TT.Assembly;
        }

        // EpicLoot MagicItem methods take includeSocketed as a named bool. Forcing it
        // false while the toggle is off drops shardstone sockets and leaves enchants.
        static void StripSocketedFlag(ref bool includeSocketed)
        {
            if (!ShardstoneEffectsOn)
            {
                includeSocketed = false;
            }
        }

        internal static void SetTalentEffects(bool on)
        {
            if (TalentEffects == null)
            {
                Plugin.Log("Talent Pace: Enable talent effects config is missing.");
                return;
            }

            TalentEffects.Value = on;
            try
            {
                Plugin.Instance.Config.Save();
            }
            catch (Exception)
            {
            }

            try
            {
                ApplyToLocalPlayer();
            }
            catch (Exception ex)
            {
                Plugin.Log("Talent Pace: applying talent toggle failed: " + ex.Message);
            }
        }

        internal static void SetShardstoneEffects(bool on)
        {
            if (ShardstoneEffects == null)
            {
                Plugin.Log("Talent Pace: Enable shardstone effects config is missing.");
                return;
            }

            ShardstoneEffects.Value = on;
            try
            {
                Plugin.Instance.Config.Save();
            }
            catch (Exception)
            {
            }
        }

        internal static void ApplyToLocalPlayer()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            if (TalentEffectsOn)
            {
                if (_syncToPlayer != null && _getRuntime != null)
                {
                    object rt = _getRuntime.Invoke(null, new object[] { player });
                    if (rt != null)
                    {
                        _syncToPlayer.Invoke(null, new object[] { rt });
                    }
                }

                return;
            }

            StripTalentStatus(player);
            List<Character> others = new List<Character>();
            Character.GetCharactersInRange(player.transform.position, 80f, others);
            for (int i = 0; i < others.Count; i++)
            {
                Character ch = others[i];
                if (ch != null && ch != player)
                {
                    StripTalentStatus(ch);
                }
            }
        }

        static void StripTalentStatus(Character character)
        {
            if (character == null || TT.Assembly == null)
            {
                return;
            }

            SEMan seman = character.GetSEMan();
            if (seman == null)
            {
                return;
            }

            List<StatusEffect> effects = seman.GetStatusEffects();
            if (effects == null)
            {
                return;
            }

            for (int i = effects.Count - 1; i >= 0; i--)
            {
                StatusEffect se = effects[i];
                if (se == null || se.GetType().Assembly != TT.Assembly)
                {
                    continue;
                }

                if (se.name == "TT_TalentPassives")
                {
                    ResetPassives(se);
                    continue;
                }

                seman.RemoveStatusEffect(se, true);
            }
        }

        static void ResetPassives(object passives)
        {
            if (passives == null || _resetToDefault == null)
            {
                return;
            }

            try
            {
                _resetToDefault.Invoke(passives, null);
            }
            catch (Exception)
            {
            }
        }
    }
}
