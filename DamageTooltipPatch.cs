using System.Text.RegularExpressions;
using HarmonyLib;
using UnityEngine;

namespace Cjayride.TalentPace
{
    internal static class DamageTooltipPatch
    {
        internal static void Apply(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(HitData.DamageTypes), "GetTooltipString", new[] { typeof(Skills.SkillType) });
            if (original != null)
                harmony.Patch(original, postfix: new HarmonyMethod(typeof(DamageTooltipPatch), nameof(Postfix)));
        }

        static void Postfix(HitData.DamageTypes __instance, Skills.SkillType skillType, ref string __result)
        {

            if (Player.m_localPlayer == null)
            return;

            // ← put the log here, as the very first line
            Plugin.Log($"DamageTooltipFix ran. Result length: {__result?.Length ?? 0}");
            

            if (string.IsNullOrEmpty(__result) || Player.m_localPlayer == null)
                return;

            float skillFactor = Player.m_localPlayer.GetSkillFactor(skillType);
            float ttFactor = 0.4f + 0.3f * skillFactor;

            // Fix orange numbers that Talent Tree reduced to ~70%
            __result = Regex.Replace(__result, @"<color=orange>(\d+)</color>", match =>
            {
                // We can’t perfectly reverse every number, so we rebuild the whole damage section
                return match.Value; // temporary – see full rebuild below
            });

            // Safer: rebuild only the damage lines and replace the whole result
            // (workbench seems to accept a pure damage string)
            var sb = new System.Text.StringBuilder();

            void Add(string token, float listed)
            {
                if (listed <= 0f) return;
                int orange = Mathf.RoundToInt(listed);
                int yellow = Mathf.RoundToInt(listed * ttFactor);
                sb.Append($"\n{token}: <color=orange>{orange}</color> <color=yellow>({yellow})</color>");
            }

            Add("$inventory_blunt", __instance.m_blunt);
            Add("$inventory_slash", __instance.m_slash);
            Add("$inventory_pierce", __instance.m_pierce);
            Add("$inventory_chop", __instance.m_chop);
            Add("$inventory_pickaxe", __instance.m_pickaxe);
            Add("$inventory_fire", __instance.m_fire);
            Add("$inventory_frost", __instance.m_frost);
            Add("$inventory_lightning", __instance.m_lightning);
            Add("$inventory_poison", __instance.m_poison);
            Add("$inventory_spirit", __instance.m_spirit);
            Add("$inventory_damage", __instance.m_damage);

            if (sb.Length > 0)
                __result = sb.ToString().TrimStart('\n');
        }
    }
}