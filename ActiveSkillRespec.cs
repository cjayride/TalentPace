using System;
using System.Collections;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Cjayride.TalentPace
{
    /// <summary>
    /// When SkillsReworked earned points drop below spent (config nerf), refund Active Skills
    /// without Rebirth cost and without wiping the talent tree.
    /// </summary>
    internal static class ActiveSkillRespec
    {
        internal static ConfigEntry<bool> EnableRespec;
        internal static ConfigEntry<bool> AutoRefund;

        static Type _pointService;
        static Type _store;
        static Type _reset;
        static Type _ui;
        static MethodInfo _getTotal;
        static MethodInfo _getSpent;
        static MethodInfo _resetActives;
        static MethodInfo _resetSpent;
        static MethodInfo _refreshUi;
        static FieldInfo _rebirthButton;
        static bool _patched;
        static Button _button;

        internal static void Bind(ConfigFile config)
        {
            EnableRespec = config.Bind(
                "Skills Reworked UI",
                "Free Active Skill respec when over budget",
                true,
                "When spent Active Skill points exceed what the current SkillsReworked config grants, show a free respec on the skills window. Refunds Active Skills only (not Level, Prestige, or Talent Tree). No coin or level cost.");
            AutoRefund = config.Bind(
                "Skills Reworked UI",
                "Auto refund over-budget Active Skills",
                true,
                "On spawn, if spent Active Skill points exceed the current budget, refund them automatically (same as the free respec button).");
        }

        internal static void Apply(Harmony harmony, Plugin plugin)
        {
            if (!EnableRespec.Value && !AutoRefund.Value)
            {
                return;
            }

            if (PatchNow(harmony))
            {
                return;
            }

            plugin.StartCoroutine(RetryPatch(harmony));
        }

        static IEnumerator RetryPatch(Harmony harmony)
        {
            for (int i = 0; i < 60 && !_patched; i++)
            {
                yield return new WaitForSeconds(1f);
                PatchNow(harmony);
            }
        }

        static bool PatchNow(Harmony harmony)
        {
            if (_patched)
            {
                return true;
            }

            if (!EnsureReflection())
            {
                return false;
            }

            MethodInfo refresh = SR.Method(_ui, "RefreshRebirthResetButton");
            if (refresh != null && EnableRespec.Value)
            {
                harmony.Patch(refresh, postfix: new HarmonyMethod(typeof(ActiveSkillRespec), nameof(AfterRefreshButton)));
            }

            MethodInfo spawned = AccessTools.DeclaredMethod(typeof(Player), "OnSpawned", new[] { typeof(bool) });
            if (spawned != null && AutoRefund.Value)
            {
                harmony.Patch(spawned, postfix: new HarmonyMethod(typeof(ActiveSkillRespec), nameof(AfterSpawned)));
            }

            _patched = true;
            Plugin.Log("Talent Pace: Active Skill over-budget respec hooked.");
            return true;
        }

        static bool EnsureReflection()
        {
            if (_getTotal != null)
            {
                return true;
            }

            _pointService = SR.Type("SkillsReworked.Systems.Progression.SkillPointService");
            _store = SR.Type("SkillsReworked.Persistence.PlayerProgressionStore");
            _reset = SR.Type("SkillsReworked.Systems.Reset.ProgressionResetService");
            _ui = SR.Type("SkillsReworked.UI.SkillPanel.SkillUiController");
            if (_pointService == null || _store == null || _reset == null)
            {
                return false;
            }

            _getTotal = SR.Method(_pointService, "GetTotalPoints", typeof(Player));
            _getSpent = SR.Method(_store, "GetSpentSkillPoints", typeof(Player));
            _resetActives = SR.Method(_reset, "ResetActiveSkills", typeof(Skills));
            _resetSpent = SR.Method(_reset, "ResetSpentPoints", typeof(Player));
            _refreshUi = _ui == null ? null : SR.Method(_ui, "RefreshAfterProgressionChange", typeof(Player));
            _rebirthButton = _ui == null ? null : SR.Field(_ui, "RebirthButton");
            return _getTotal != null && _getSpent != null && _resetActives != null && _resetSpent != null;
        }

        internal static bool NeedsRespec(Player player)
        {
            if (player == null || !EnsureReflection())
            {
                return false;
            }

            try
            {
                int total = Convert.ToInt32(_getTotal.Invoke(null, new object[] { player }));
                int spent = Convert.ToInt32(_getSpent.Invoke(null, new object[] { player }));
                return spent > total;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static bool TryRefund(Player player)
        {
            if (player == null || !NeedsRespec(player))
            {
                return false;
            }

            Skills skills = player.GetSkills();
            if (skills == null)
            {
                return false;
            }

            try
            {
                _resetActives.Invoke(null, new object[] { skills });
                _resetSpent.Invoke(null, new object[] { player });
            }
            catch (Exception ex)
            {
                Plugin.Log("Talent Pace: Active Skill refund failed: " + ex.Message);
                return false;
            }

            try
            {
                if (_refreshUi != null)
                {
                    _refreshUi.Invoke(null, new object[] { player });
                }
            }
            catch (Exception)
            {
            }

            player.Message(MessageHud.MessageType.Center, "Active Skill points refunded (over new budget). Spend them again.", 0, null);
            Plugin.Log("Talent Pace: refunded over-budget Active Skills for " + player.GetPlayerName());
            return true;
        }

        static void AfterSpawned(Player __instance, bool spawnValkyrie)
        {
            if (!AutoRefund.Value || __instance == null || __instance != Player.m_localPlayer)
            {
                return;
            }

            TryRefund(__instance);
        }

        static void AfterRefreshButton()
        {
            if (!EnableRespec.Value || _rebirthButton == null)
            {
                return;
            }

            Button rebirth = _rebirthButton.GetValue(null) as Button;
            if (rebirth == null)
            {
                return;
            }

            Player player = Player.m_localPlayer;
            bool show = player != null && NeedsRespec(player);
            EnsureButton(rebirth.transform.parent);
            if (_button != null)
            {
                _button.gameObject.SetActive(show);
            }
        }

        static void EnsureButton(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            Transform existing = parent.Find("TalentPace_ActiveRespec");
            if (existing != null)
            {
                _button = existing.GetComponent<Button>();
                return;
            }

            GameObject go = new GameObject("TalentPace_ActiveRespec", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 8f);
            rt.sizeDelta = new Vector2(220f, 28f);

            Image image = go.AddComponent<Image>();
            image.color = new Color(0.15f, 0.35f, 0.2f, 0.92f);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => TryRefund(Player.m_localPlayer));

            GameObject textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            Text text = textGo.AddComponent<Text>();
            text.text = "Free respec (over Active points)";
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 12;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _button = button;
        }
    }
}
