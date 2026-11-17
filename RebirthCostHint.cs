using System;
using System.Collections;
using System.Reflection;
using System.Text;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Cjayride.TalentPace
{
    /// <summary>
    /// Shows SkillsReworked Rebirth costs under the Rebirth button and in the confirm dialog.
    /// Works with the vanilla Skills dialog (SR 1.8.x); does not use SeneaL's skills panel.
    /// </summary>
    internal static class RebirthCostHint
    {
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> ShowInConfirmDialog;

        private static bool s_patched;
        private static Text s_costLabel;
        private static Type s_uiType;
        private static Type s_modConfigType;
        private static Type s_itemCostType;
        private static FieldInfo s_rebirthButton;
        private static FieldInfo s_rebirthDialogRoot;
        private static MethodInfo s_getTotalAllocated;
        private static PropertyInfo s_rebirthCostEnabled;
        private static PropertyInfo s_rebirthCostMode;
        private static PropertyInfo s_rebirthLevelLoss;
        private static PropertyInfo s_rebirthItemName;
        private static PropertyInfo s_rebirthItemAmount;
        private static MethodInfo s_getItemDisplayName;

        internal static void Bind(ConfigFile config)
        {
            Enabled = config.Bind(
                "Skills Reworked UI",
                "Show Rebirth cost",
                true,
                "When SkillsReworked is installed, shows the configured Rebirth cost under the Rebirth button on the skills window.");
            ShowInConfirmDialog = config.Bind(
                "Skills Reworked UI",
                "Show Rebirth cost in confirm dialog",
                true,
                "Also adds the cost to the Yes/No Rebirth confirmation text.");
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
                Plugin.Log("Talent Pace: SkillsReworked skill UI was not found, so Rebirth cost hints are off.");
            }
        }

        private static bool PatchNow(Harmony harmony)
        {
            if (s_patched)
            {
                return true;
            }

            if (!EnsureReflection())
            {
                return false;
            }

            MethodInfo refresh = AccessTools.Method(s_uiType, "RefreshRebirthResetButton");
            MethodInfo requested = AccessTools.Method(s_uiType, "OnRebirthRequested");
            if (refresh == null)
            {
                return false;
            }

            harmony.Patch(refresh, postfix: new HarmonyMethod(typeof(RebirthCostHint), nameof(AfterRefreshRebirthButton)));
            if (ShowInConfirmDialog.Value && requested != null)
            {
                harmony.Patch(requested, postfix: new HarmonyMethod(typeof(RebirthCostHint), nameof(AfterRebirthRequested)));
            }

            s_patched = true;
            Plugin.Log("Talent Pace: Rebirth cost hints hooked into SkillsReworked.");
            return true;
        }

        private static bool EnsureReflection()
        {
            if (s_uiType != null)
            {
                return true;
            }

            s_uiType = AccessTools.TypeByName("SkillsReworked.UI.SkillPanel.SkillUiController");
            s_modConfigType = AccessTools.TypeByName("SkillsReworked.Bootstrap.ModConfig");
            s_itemCostType = AccessTools.TypeByName("SkillsReworked.Systems.Costs.ItemCostService");
            if (s_uiType == null || s_modConfigType == null)
            {
                return false;
            }

            s_rebirthButton = AccessTools.Field(s_uiType, "RebirthButton");
            s_rebirthDialogRoot = AccessTools.Field(s_uiType, "_rebirthDialogRoot");
            s_getTotalAllocated = AccessTools.Method(s_uiType, "GetTotalAllocated");
            s_rebirthCostEnabled = AccessTools.Property(s_modConfigType, "RebirthCostEnabled");
            s_rebirthCostMode = AccessTools.Property(s_modConfigType, "RebirthCostModeSetting");
            s_rebirthLevelLoss = AccessTools.Property(s_modConfigType, "RebirthLevelLoss");
            s_rebirthItemName = AccessTools.Property(s_modConfigType, "RebirthItemName");
            s_rebirthItemAmount = AccessTools.Property(s_modConfigType, "RebirthItemAmount");
            if (s_itemCostType != null)
            {
                s_getItemDisplayName = AccessTools.Method(s_itemCostType, "GetItemDisplayName");
            }

            return s_rebirthButton != null
                && s_getTotalAllocated != null
                && s_rebirthCostEnabled != null
                && s_rebirthCostMode != null
                && s_rebirthLevelLoss != null
                && s_rebirthItemName != null
                && s_rebirthItemAmount != null;
        }

        static void AfterRefreshRebirthButton()
        {
            if (!Enabled.Value)
            {
                return;
            }

            RefreshLabel();
        }

        static void AfterRebirthRequested()
        {
            if (!Enabled.Value || !ShowInConfirmDialog.Value)
            {
                return;
            }

            string cost = BuildCostLine();
            if (string.IsNullOrEmpty(cost))
            {
                return;
            }

            GameObject root = s_rebirthDialogRoot == null ? null : s_rebirthDialogRoot.GetValue(null) as GameObject;
            if (root == null)
            {
                return;
            }

            Text label = root.GetComponentInChildren<Text>(true);
            if (label == null)
            {
                return;
            }

            string confirm = label.text ?? string.Empty;
            if (confirm.IndexOf("Cost:", StringComparison.Ordinal) >= 0)
            {
                return;
            }

            label.text = confirm + "\n\n" + cost;
        }

        static void RefreshLabel()
        {
            if (!EnsureReflection())
            {
                return;
            }

            Button button = s_rebirthButton.GetValue(null) as Button;
            if (button == null)
            {
                return;
            }

            Text buttonText = button.GetComponentInChildren<Text>();
            EnsureCostLabel(button, buttonText);

            if (s_costLabel == null)
            {
                return;
            }

            int allocated = (int)s_getTotalAllocated.Invoke(null, null);
            if (allocated > 0)
            {
                s_costLabel.text = string.Empty;
                s_costLabel.gameObject.SetActive(false);
                return;
            }

            string cost = BuildCostLine();
            if (string.IsNullOrEmpty(cost))
            {
                s_costLabel.text = string.Empty;
                s_costLabel.gameObject.SetActive(false);
                return;
            }

            s_costLabel.text = cost;
            s_costLabel.gameObject.SetActive(true);
        }

        static void EnsureCostLabel(Button button, Text buttonText)
        {
            if (s_costLabel != null)
            {
                return;
            }

            Transform parent = button.transform;
            Transform existing = parent.Find("TalentPace_RebirthCost");
            if (existing != null)
            {
                s_costLabel = existing.GetComponent<Text>();
                return;
            }

            GameObject go = new GameObject("TalentPace_RebirthCost", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -2f);
            rect.sizeDelta = new Vector2(110f, 14f);

            Text text = go.AddComponent<Text>();
            text.alignment = TextAnchor.UpperCenter;
            text.fontSize = 11;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.color = new Color(0.85f, 0.65f, 0.35f, 1f);
            if (buttonText != null)
            {
                text.font = buttonText.font;
                text.fontStyle = buttonText.fontStyle;
            }

            s_costLabel = text;
        }

        static string BuildCostLine()
        {
            if (!EnsureReflection())
            {
                return null;
            }

            if (!(s_rebirthCostEnabled.GetValue(null, null) is bool enabled) || !enabled)
            {
                return null;
            }

            string modeName = s_rebirthCostMode.GetValue(null, null)?.ToString() ?? string.Empty;
            bool useLevel = modeName == "LevelOnly" || modeName == "Both";
            bool useItem = modeName == "ItemOnly" || modeName == "Both";

            int levelLoss = s_rebirthLevelLoss.GetValue(null, null) is int levels ? levels : 0;
            int itemAmount = s_rebirthItemAmount.GetValue(null, null) is int amount ? amount : 0;
            string itemPrefab = s_rebirthItemName.GetValue(null, null) as string;

            var parts = new StringBuilder(64);
            if (useLevel && levelLoss > 0)
            {
                parts.Append(levelLoss).Append(" Level");
            }

            if (useItem && itemAmount > 0 && !string.IsNullOrEmpty(itemPrefab))
            {
                string itemName = itemPrefab;
                if (s_getItemDisplayName != null)
                {
                    object display = s_getItemDisplayName.Invoke(null, new object[] { itemPrefab });
                    if (display is string localized && !string.IsNullOrEmpty(localized))
                    {
                        itemName = localized;
                    }
                }

                if (parts.Length > 0)
                {
                    parts.Append(", ");
                }

                parts.Append(itemAmount).Append(' ').Append(itemName);
            }

            if (parts.Length == 0)
            {
                return null;
            }

            return "Cost: " + parts;
        }
    }
}