using System;
using System.Reflection;
using HarmonyLib;

namespace Cjayride.TalentPace
{
    // Talent Tree 2.3.5 tooltip Prefixes that lie about item cards:
    // - AddBlockTooltip: orange = block at 100 Blocking (base × 1.5)
    // - GetTooltipString: orange = 70% of listed weapon damage (their nerf cap)
    // Skip those Prefixes so vanilla runs. Combat skill-damage nerf patches stay.
    internal static class BlockTooltipPatch
    {
        static readonly string[] TooltipPrefixTypes =
        {
            "TalentTree.ItemData_AddBlockTooltip_Patch",
            "ItemData_AddBlockTooltip_Patch",
            "TalentTree.DamageTypes_GetTooltipString_Patch",
            "DamageTypes_GetTooltipString_Patch"
        };

        internal static void Apply(Harmony harmony, BepInEx.Logging.ManualLogSource log)
        {
            int n = 0;
            for (int i = 0; i < TooltipPrefixTypes.Length; i++)
            {
                Type type = TT.Type(TooltipPrefixTypes[i]) ?? AccessTools.TypeByName(TooltipPrefixTypes[i]);
                MethodInfo prefix = type == null ? null : AccessTools.DeclaredMethod(type, "Prefix");
                if (prefix == null)
                {
                    continue;
                }

                harmony.Patch(prefix, prefix: new HarmonyMethod(typeof(BlockTooltipPatch), nameof(SkipWhenEnabled)));
                n++;
            }

            if (n == 0)
            {
                log.LogWarning("Talent Pace: Talent Tree tooltip patches were not found.");
                return;
            }

            log.LogInfo("Talent Pace: skipped " + n + " Talent Tree item-tooltip Prefix(es) (block + weapon damage). Combat nerf unchanged.");
        }

        static bool SkipWhenEnabled()
        {
            return Plugin.RestoreVanillaBlockTooltip == null || !Plugin.RestoreVanillaBlockTooltip.Value;
        }
    }
}
