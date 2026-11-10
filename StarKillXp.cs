using System;
using System.Collections;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Cjayride.TalentPace
{
    // Star-level kill XP bonus.
    // High-star mobs (Smoothbrain Creature Level and Loot Control) can be brutal in
    // over-leveled zones, while the SkillsReworked zone floor shrinks their XP to
    // near zero. This adds a percentage bonus to the kill's base XP for mobs more
    // than N stars, so the reward scales with how hard the mob was to kill.
    // Patched by name: with SkillsReworked absent, this stays silently off.
    internal static class StarKillXp
    {
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<int> StarThreshold;
        internal static ConfigEntry<float> PercentPerStar;
        internal static ConfigEntry<float> PercentCap;

        private static bool s_patched;

        internal static void Bind(BepInEx.Configuration.ConfigFile config)
        {
            Enabled = config.Bind(
                "Star kill XP",
                "Enable star kill XP bonus",
                true,
                "With SkillsReworked installed, kills of high-star mobs (Creature Level and Loot Control) pay a percentage XP bonus on top of their normal XP, so dangerous spawns are worth the fight even in over-leveled zones.");
            StarThreshold = config.Bind(
                "Star kill XP",
                "Bonus starts above (stars)",
                5,
                new ConfigDescription(
                    "Mobs with more stars than this get the bonus. 5 = a 6-star mob is the first to pay extra.",
                    new AcceptableValueRange<int>(1, 20)));
            PercentPerStar = config.Bind(
                "Star kill XP",
                "XP bonus percent per star over",
                25f,
                new ConfigDescription(
                    "Percent of the kill's base XP added per star above the threshold. 25 = a 6-star mob pays +25%, an 8-star mob pays +75%.",
                    new AcceptableValueRange<float>(0f, 200f)));
            PercentCap = config.Bind(
                "Star kill XP",
                "XP bonus percent cap",
                150f,
                new ConfigDescription(
                    "The total star bonus on one kill never exceeds this percent of its base XP, so extreme star counts cannot farm XP.",
                    new AcceptableValueRange<float>(0f, 500f)));
        }

        internal static void Apply(Harmony harmony, Plugin plugin)
        {
            if (!Enabled.Value)
            {
                return;
            }

            if (PatchNow(harmony))
            {
                return;
            }

            plugin.StartCoroutine(RetryPatch(harmony));
        }

        private static IEnumerator RetryPatch(Harmony harmony)
        {
            for (int i = 0; i < 60 && !s_patched; i++)
            {
                yield return new WaitForSeconds(1f);
                PatchNow(harmony);
            }

            if (!s_patched)
            {
                Plugin.Log("Talent Pace: SkillsReworked kill rewards were not found, so the star kill XP bonus is off.");
            }
        }

        private static bool PatchNow(Harmony harmony)
        {
            if (s_patched)
            {
                return true;
            }

            Type flow = AccessTools.TypeByName("SkillsReworked.Systems.Rewards.KillRewardFlow");
            if (flow == null)
            {
                return false;
            }

            MethodInfo method = AccessTools.Method(flow, "AnnounceDeath");
            if (method == null)
            {
                return false;
            }

            MethodInfo prefix = AccessTools.Method(typeof(StarKillXpPatch), "Prefix");
            harmony.Patch(method, new HarmonyMethod(prefix));
            s_patched = true;
            Plugin.Log(
                "Talent Pace: star kill XP bonus active, +" + PercentPerStar.Value.ToString("0.#") +
                "% per star over " + StarThreshold.Value + ", capped at +" + PercentCap.Value.ToString("0.#") + "%.");
            return true;
        }
    }

    internal static class StarKillXpPatch
    {
        // Runs on the mob's owner (host/server) before the death RPC, so the bonus
        // rides the normal kill-XP path to the killer and to party members.
        private static void Prefix(Character victim, ref float baseXp, bool isBoss)
        {
            if (victim == null || isBoss || baseXp <= 0f)
            {
                return;
            }

            int stars = victim.GetLevel() - 1;
            int over = stars - StarKillXp.StarThreshold.Value;
            if (over <= 0)
            {
                return;
            }

            float percent = Math.Min(over * StarKillXp.PercentPerStar.Value, StarKillXp.PercentCap.Value);
            baseXp += baseXp * (percent * 0.01f);
        }
    }
}
