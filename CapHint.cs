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
        const string FreeRespecButtonName = "TalentPaceFreeRespec";
        const float RefreshInterval = 0.25f;

        static FieldInfo _view;
        static PropertyInfo _titleText;
        static MethodInfo _getRuntime;
        static MethodInfo _totalPoints;
        static MethodInfo _windowRefresh;
        static float _nextSample;
        static bool _logged;

        static Text _label;
        static Button _freeRespecButton;
        static Text _cachedTitle;
        static Vector2 _lastAnchored;
        static Vector2 _lastSizeDelta;
        static string _lastText;
        static bool _lastFreeRespecVisible;
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

            _windowRefresh = refresh;
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

            bool needsFree = Pace.NeedsFreeRespec(player);
            if (hint == _lastText && needsFree == _lastFreeRespecVisible && _label != null && _label.gameObject.activeInHierarchy)
            {
                return;
            }

            _lastText = hint;
            _lastFreeRespecVisible = needsFree;
            bool overCap = needsFree;
            Text label = Ensure(title);
            label.text = hint;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            int colorMark = overCap ? 2 : 1;
            if (colorMark != _lastColorMark)
            {
                _lastColorMark = colorMark;
                label.color = overCap
                    ? new Color(1f, 0.45f, 0.35f, 1f)
                    : new Color(1f, 0.82f, 0.35f, 1f);
            }

            if (!label.gameObject.activeSelf)
            {
                label.gameObject.SetActive(true);
            }

            EnsureFreeRespecButton(title, needsFree);

            if (!_logged)
            {
                _logged = true;
                Plugin.Log("Talent window cap line active.");
            }
        }

        static void OnFreeRespecClicked()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            if (!Pace.TryFreeRespec(player))
            {
                return;
            }

            Pace.InvalidateHintCache(player);
            _lastText = null;
            _hintPlayerCleared();
            if (_windowRefresh != null)
            {
                _windowRefresh.Invoke(null, null);
            }
        }

        static void _hintPlayerCleared()
        {
            _nextSample = 0f;
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
            Vector2 size = new Vector2(Mathf.Max(920f, titleRt.sizeDelta.x), 120f);

            if (_label != null && _label.transform.parent == parent && _lastAnchored == anchored && _lastSizeDelta == size)
            {
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
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
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
            label.fontSize = Mathf.Clamp(Mathf.RoundToInt(title.fontSize * 0.58f), 15, 20);
            label.transform.SetAsLastSibling();

            _label = label;
            _lastAnchored = anchored;
            _lastSizeDelta = size;
            _lastColorMark = -1;
            return label;
        }

        static void EnsureFreeRespecButton(Text title, bool visible)
        {
            if (_freeRespecButton != null && _freeRespecButton.gameObject.activeSelf == visible)
            {
                return;
            }

            if (_label == null)
            {
                return;
            }

            Transform parent = _label.transform.parent;
            Transform existing = parent.Find(FreeRespecButtonName);
            Button button = existing != null ? existing.GetComponent<Button>() : null;
            if (button == null && visible)
            {
                GameObject go = new GameObject(FreeRespecButtonName);
                go.transform.SetParent(parent, false);
                Image image = go.AddComponent<Image>();
                image.color = new Color(0.15f, 0.35f, 0.12f, 0.95f);
                button = go.AddComponent<Button>();
                GameObject textGo = new GameObject("Text");
                textGo.transform.SetParent(go.transform, false);
                Text label = textGo.AddComponent<Text>();
                label.font = title.font;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.fontSize = Mathf.Clamp(Mathf.RoundToInt(title.fontSize * 0.55f), 14, 18);
                label.text = "Free respec (over cap)";
                RectTransform textRt = label.rectTransform;
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = Vector2.zero;
                textRt.offsetMax = Vector2.zero;
                button.onClick.AddListener(OnFreeRespecClicked);
                _freeRespecButton = button;
            }

            if (button == null)
            {
                return;
            }

            RectTransform capRt = _label.rectTransform;
            RectTransform rt = button.GetComponent<RectTransform>();
            rt.anchorMin = capRt.anchorMin;
            rt.anchorMax = capRt.anchorMax;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(280f, 32f);
            rt.anchoredPosition = capRt.anchoredPosition + new Vector2(0f, -capRt.sizeDelta.y - 6f);
            button.gameObject.SetActive(visible);
            if (visible)
            {
                button.transform.SetAsLastSibling();
            }
        }
    }
}
