
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace Cjayride.TalentPace
{
    internal static class BlockTooltipPatch
    {
        internal static void Apply(Harmony harmony)
        {
            Type itemDataType = typeof(ItemDrop.ItemData);

            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(itemDataType))
            {
                if (method.Name != "GetTooltip" ||
                    !method.IsStatic ||
                    method.ReturnType != typeof(string))
                    continue;

                ParameterInfo[] p = method.GetParameters();

                if ((p.Length != 3 && p.Length != 6) ||
                    p[0].ParameterType != itemDataType ||
                    p[1].ParameterType != typeof(int) ||
                    p[2].ParameterType != typeof(bool))
                    continue;

                harmony.Patch(
                    method,
                    postfix: new HarmonyMethod(
                        typeof(BlockTooltipPatch),
                        nameof(TooltipPostfix))
                    {
                        priority = Priority.Last
                    });
            }

            Type auga = AccessTools.TypeByName("EpicLoot.AugaTooltip");
            MethodInfo preprocess = auga == null
                ? null
                : AccessTools.Method(auga, "PreprocessTooltipStat");

            if (preprocess != null)
            {
                harmony.Patch(
                    preprocess,
                    postfix: new HarmonyMethod(
                        typeof(BlockTooltipPatch),
                        nameof(AugaPostfix))
                    {
                        priority = Priority.Last
                    });
            }
        }

        private static void TooltipPostfix(
            object[] __args,
            ref string __result)
        {
            if (!Plugin.RestoreVanillaBlockTooltip.Value ||
                __args == null ||
                __args.Length < 3 ||
                !Convert.ToBoolean(__args[2]))
                return;

            ItemDrop.ItemData item = __args[0] as ItemDrop.ItemData;

            if (item?.m_shared == null ||
                item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Shield)
                return;

            Restore(ref __result, item, (int)__args[1]);
        }

        private static void AugaPostfix(
            ItemDrop.ItemData item,
            string label,
            ref Tuple<string, string> __result)
        {
            if (!Plugin.RestoreVanillaBlockTooltip.Value ||
                item?.m_shared == null ||
                item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Shield ||
                __result == null)
                return;

            string statLabel = (label ?? "") + " " + (__result.Item1 ?? "");

            if (statLabel.IndexOf("block", StringComparison.OrdinalIgnoreCase) < 0 &&
                statLabel.IndexOf("$item_blockarmor", StringComparison.OrdinalIgnoreCase) < 0)
                return;

            string block = item.GetBaseBlockPower(item.m_quality).ToString("0");
            __result = Tuple.Create(__result.Item1, block);
        }

        private static void Restore(
            ref string tooltip,
            ItemDrop.ItemData item,
            int quality)
        {
            if (string.IsNullOrEmpty(tooltip))
                return;

            string block = item.GetBaseBlockPower(quality).ToString("0");
            string force = item.m_shared.m_deflectionForce.ToString("0");

            tooltip = Regex.Replace(
                tooltip,
                @"\$item_blockarmor:.*",
                "$item_blockarmor: <color=orange>" +
                block + "</color> <color=yellow>(" +
                block + ")</color>");

            tooltip = Regex.Replace(
                tooltip,
                @"\$item_blockforce:.*",
                "$item_blockforce: <color=orange>" +
                force + "</color>");
        }
    }
}
