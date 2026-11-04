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
        const float RefreshInterval = 0.25f;

        static FieldInfo _view;
        static PropertyInfo _titleText;
        static MethodInfo _getRuntime;
        static MethodInfo _totalPoints;
        static float _nextSample;
        static bool _logged;

        // Cached UI state so TalentWindow.Tick's per-frame postfix is nearly free.
        // Layout is applied once per parent; text/color only on change; Unity
        // marks the canvas dirty on a real text change, so nothing extra is needed.
        static Text _label;
        static Text _cachedTitle;
        static Vector2 _lastAnchored;
        static Vector2 _lastSizeDelta;
        static string _lastText;
        static int _lastColorMark;
        static readonly object[] _oneArg = new object[1];
        static readonly object[] _twoArg = new object[2];

        public static void Apply(Harmony harmony)
        {
            Type window = TT.Type("UI.TalentWindow");
            MethodInfo refresh = window == null ? null : AccessTools.DeclaredMethod(window, "Refresh");
            if (refresh == null)
            {
                Plugin.Log("Talent window refresh was not found. The cap line is off.");
                return;
            }

            _view = AccessTools.Field(window, "_view");
            Type view = TT.Type("UI.TalentWindowView");
            _titleText = view == null ? null : AccessTools.Property(view, "TitleText");
            Type service = TT.Type("Core.TalentRuntimeService");
            Type runtime = TT.Type("Core.TalentRuntime");
            Type budget = TT.Type("Core.TalentPointBudgetService");
            _getRuntime = service == null ? null : AccessTools.DeclaredMethod(service, "Get", new[] { typeof(Player) });
            _totalPoints = budget == null || runtime == null
                ? null
                : AccessTools.DeclaredMethod(budget, "GetTotalPoints", new[] { typeof(Player), runtime });
            // Refresh is event-driven (open / rebuild). Tick used to carry this postfix
            // every frame the window was open; the Harmony trampoline itself cost more
            // than the work. The line still updates whenever Talent Tree refreshes.
            harmony.Patch(refresh, postfix: new HarmonyMethod(typeof(CapHint), nameof(AfterRefresh)));
        }

        static void AfterRefresh()
        {
            if (Time.unscaledTime < _nextSample)
            {
                return;
            }

            _nextSample = Time.unscaledTime + RefreshInterval;

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

            Sample(player);

            string hint = Pace.Hint(player);
            if (string.IsNullOrEmpty(hint))
            {
                hint = "No talent cap.";
            }

            // Hint is cached upstream; this reference compare is the whole per-tick cost
            // for the common case where nothing changed. The liveness check matters:
            // Talent Tree tears its window down between opens, and a destroyed label
            // must be rebuilt even when the text is unchanged.
            if (hint == _lastText && _label != null && _label.gameObject.activeInHierarchy)
            {
                return;
            }

            _lastText = hint;
            bool reached = hint.StartsWith("Talent cap reached", StringComparison.Ordinal);
            Text label = Ensure(title);
            label.text = hint;
            int colorMark = reached ? 1 : 0;
            if (colorMark != _lastColorMark)
            {
                _lastColorMark = colorMark;
                label.color = reached
                    ? new Color(1f, 0.55f, 0.2f, 1f)
                    : new Color(1f, 0.82f, 0.35f, 1f);
            }

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
                _oneArg[0] = player;
                object runtime = _getRuntime.Invoke(null, _oneArg);
                if (runtime == null)
                {
                    _oneArg[0] = null;
                    return;
                }

                _twoArg[0] = player;
                _twoArg[1] = runtime;
                _totalPoints.Invoke(null, _twoArg);
                _oneArg[0] = null;
                _twoArg[0] = null;
                _twoArg[1] = null;
            }
            catch (Exception)
            {
            }
        }

        static Text Title()
        {
            if (_cachedTitle != null && _cachedTitle)
            {
                return _cachedTitle;
            }

            if (_view == null || _titleText == null)
            {
                return null;
            }

            object view = _view.GetValue(null);
            _cachedTitle = view == null ? null : _titleText.GetValue(view, null) as Text;
            return _cachedTitle;
        }

        static Text Ensure(Text title)
        {
            Transform parent = title.transform.parent;
            RectTransform titleRt = title.rectTransform;
            float drop = titleRt.rect.height > 1f ? titleRt.rect.height : titleRt.sizeDelta.y;
            Vector2 anchored = titleRt.anchoredPosition + new Vector2(0f, -drop - 2f);
            Vector2 size = new Vector2(Mathf.Max(900f, titleRt.sizeDelta.x), 36f);

            if (_label != null && _label.transform.parent == parent && _lastAnchored == anchored && _lastSizeDelta == size)
            {
                // Same window instance and same layout: reuse the label, zero layout work.
                return _label;
            }

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

            RectTransform rt = label.rectTransform;
            rt.anchorMin = titleRt.anchorMin;
            rt.anchorMax = titleRt.anchorMax;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchored;
            label.fontSize = Mathf.Clamp(Mathf.RoundToInt(title.fontSize * 0.62f), 16, 22);
            label.transform.SetAsLastSibling();

            _label = label;
            _lastAnchored = anchored;
            _lastSizeDelta = size;
            _lastColorMark = -1;
            return label;
        }
    }
}
