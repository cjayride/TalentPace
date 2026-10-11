using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Cjayride.TalentPace
{
    static class CapHint
    {
        const string LabelName = "TalentPaceCap";

        static FieldInfo _view;
        static PropertyInfo _titleText;
        static MethodInfo _getRuntime;
        static MethodInfo _totalPoints;
        static float _nextSample;
        static bool _logged;

        public static void Apply(Harmony harmony)
        {
            Type window = AccessTools.TypeByName("UI.TalentWindow");
            MethodInfo refresh = window == null ? null : AccessTools.DeclaredMethod(window, "Refresh");
            MethodInfo tick = window == null ? null : AccessTools.DeclaredMethod(window, "Tick");
            if (refresh == null)
            {
                Plugin.Log("Talent window refresh was not found. The cap line is off.");
                return;
            }

            _view = AccessTools.Field(window, "_view");
            Type view = AccessTools.TypeByName("UI.TalentWindowView");
            _titleText = view == null ? null : AccessTools.Property(view, "TitleText");
            Type service = AccessTools.TypeByName("Core.TalentRuntimeService");
            Type runtime = AccessTools.TypeByName("Core.TalentRuntime");
            Type budget = AccessTools.TypeByName("Core.TalentPointBudgetService");
            _getRuntime = service == null ? null : AccessTools.DeclaredMethod(service, "Get", new[] { typeof(Player) });
            _totalPoints = budget == null || runtime == null
                ? null
                : AccessTools.DeclaredMethod(budget, "GetTotalPoints", new[] { typeof(Player), runtime });
            HarmonyMethod after = new HarmonyMethod(typeof(CapHint), nameof(AfterRefresh));
            harmony.Patch(refresh, postfix: after);
            if (tick != null)
            {
                harmony.Patch(tick, postfix: after);
            }
        }

        static void AfterRefresh()
        {
            Text title = Title();
            if (title == null || !title.gameObject.activeInHierarchy)
            {
                return;
            }

            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            if (Time.unscaledTime >= _nextSample)
            {
                _nextSample = Time.unscaledTime + 0.5f;
                Sample(player);
            }

            string hint = Pace.Hint(player);
            if (string.IsNullOrEmpty(hint))
            {
                hint = "No talent cap.";
            }

            Text label = Ensure(title);
            label.text = hint;
            label.color = hint.StartsWith("Talent cap reached", StringComparison.Ordinal)
                ? new Color(1f, 0.55f, 0.2f, 1f)
                : new Color(1f, 0.82f, 0.35f, 1f);
            label.transform.SetAsLastSibling();
            if (!label.gameObject.activeSelf)
            {
                label.gameObject.SetActive(true);
            }

            if (!_logged)
            {
                _logged = true;
                Plugin.Log("Talent window cap line: " + hint);
            }
        }

        static void Sample(Player player)
        {
            if (_getRuntime == null || _totalPoints == null)
            {
                return;
            }

            try
            {
                object runtime = _getRuntime.Invoke(null, new object[] { player });
                if (runtime == null)
                {
                    return;
                }

                _totalPoints.Invoke(null, new object[] { player, runtime });
            }
            catch (Exception)
            {
            }
        }

        static Text Title()
        {
            if (_view == null || _titleText == null)
            {
                return null;
            }

            object view = _view.GetValue(null);
            return view == null ? null : _titleText.GetValue(view, null) as Text;
        }

        static Text Ensure(Text title)
        {
            Transform parent = title.transform.parent;
            Transform existing = parent.Find(LabelName);
            Text label = existing != null ? existing.GetComponent<Text>() : null;
            if (label == null)
            {
                GameObject go = new GameObject(LabelName);
                go.transform.SetParent(parent, false);
                label = go.AddComponent<Text>();
                label.font = title.font;
                label.alignment = TextAnchor.UpperCenter;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                label.raycastTarget = false;
                label.supportRichText = false;
            }

            RectTransform titleRt = title.rectTransform;
            RectTransform rt = label.rectTransform;
            rt.anchorMin = titleRt.anchorMin;
            rt.anchorMax = titleRt.anchorMax;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(Mathf.Max(900f, titleRt.sizeDelta.x), 36f);
            float drop = titleRt.rect.height > 1f ? titleRt.rect.height : titleRt.sizeDelta.y;
            rt.anchoredPosition = titleRt.anchoredPosition + new Vector2(0f, -drop - 2f);
            label.fontSize = Mathf.Clamp(Mathf.RoundToInt(title.fontSize * 0.62f), 16, 22);
            return label;
        }
    }
}
