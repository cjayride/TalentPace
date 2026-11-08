using System;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cjayride.TalentPace
{
    static class CapHint
    {
        const string OverlayName = "TalentPaceOverlay";
        const string LabelName = "TalentPaceCap";
        const string FreeRespecButtonName = "TalentPaceFreeRespec";
        const string FontAssetName = "Valheim-AveriaSansLibre";
        const float RefreshInterval = 0.25f;
        const float LabelSize = 17f;
        const float ButtonWidth = 320f;
        const float ButtonHeight = 34f;
        const float SideMargin = 10f;
        const float Gap = 6f;

        static FieldInfo _view;
        static PropertyInfo _titleText;
        static PropertyInfo _pointsText;
        static PropertyInfo _panelRect;
        static MethodInfo _getRuntime;
        static MethodInfo _totalPoints;
        static MethodInfo _windowRefresh;
        static float _nextSample;
        static bool _logged;

        static RectTransform _overlay;
        static Canvas _overlayCanvas;
        static TextMeshProUGUI _label;
        static Button _freeRespecButton;
        static Component _anchorComponent;
        static string _lastSignature;
        static int _lastColorMark = -1;
        static TMP_FontAsset _fontAsset;
        static double _nextFontScan;
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
            _pointsText = view == null ? null : AccessTools.Property(view, "PointsText");
            _panelRect = view == null ? null : AccessTools.Property(view, "PanelRect");
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

            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            RectTransform anchor = Anchor();
            RectTransform host = Host();
            if (anchor == null || host == null || !anchor.gameObject.activeInHierarchy || !host.gameObject.activeInHierarchy)
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
            int colorMark = needsFree ? 2 : 1;
            Vector2 anchorTopLeft = TopLeftIn(anchor, _overlay.parent as RectTransform);
            string signature = hint + '|' + needsFree + '|' + colorMark
                + '|' + anchorTopLeft.x.ToString("0.0") + '|' + anchorTopLeft.y.ToString("0.0");
            if (signature == _lastSignature && _label != null && _label.gameObject.activeInHierarchy)
            {
                return;
            }

            _lastSignature = signature;

            TextMeshProUGUI label = EnsureLabel();
            Rect bounds = HostBounds(_overlay.parent as RectTransform);
            float width = LabelWidth(anchor, host, bounds);
            float top = anchorTopLeft.y - Gap;
            float x = Mathf.Max(bounds.xMin + SideMargin, anchorTopLeft.x);
            // The label grows downward from its top, so the space that matters is below it.
            // Reserve room for the button when it is shown so it always fits inside the panel.
            float buttonRoom = needsFree ? ButtonHeight + 8f : 0f;
            float available = top - bounds.yMin - SideMargin - buttonRoom;
            float height = LabelHeight(label, width, available);

            // Degenerate layouts (counter near the panel bottom): shift the whole
            // label + button block up so nothing leaves the panel.
            float block = height + buttonRoom;
            float minTop = bounds.yMin + SideMargin + block;
            if (top < minTop)
            {
                top = minTop;
            }

            RectTransform labelRt = label.rectTransform;
            labelRt.anchorMin = new Vector2(0f, 1f);
            labelRt.anchorMax = new Vector2(0f, 1f);
            labelRt.pivot = new Vector2(0f, 1f);
            labelRt.sizeDelta = new Vector2(width, height);
            labelRt.anchoredPosition = new Vector2(x - bounds.xMin, top - bounds.yMax);

            label.SetText(hint);
            ApplyFont(label);
            if (colorMark != _lastColorMark)
            {
                _lastColorMark = colorMark;
                label.color = needsFree
                    ? new Color(1f, 0.5f, 0.42f, 1f)
                    : new Color(1f, 0.88f, 0.55f, 1f);
            }

            EnsureFreeRespecButton(label, x, top - height - 8f, bounds, needsFree);

            if (!_logged)
            {
                _logged = true;
                Plugin.Log("Talent window cap line active (overlay canvas, anchored under the point counter).");
            }
        }

        static float LabelWidth(RectTransform anchor, RectTransform host, Rect bounds)
        {
            float fromPanel = host.rect.width - SideMargin * 2f;
            return Mathf.Clamp(Mathf.Max(420f, anchor.rect.width * 1.15f), 420f, Mathf.Max(420f, fromPanel, bounds.width - SideMargin * 2f));
        }

        static float LabelHeight(TextMeshProUGUI label, float width, float availableHeight)
        {
            float preferred = label.GetPreferredValues(label.text, width, 0f).y;
            if (preferred <= 1f)
            {
                preferred = label.preferredHeight;
            }

            float height = Mathf.Ceil(preferred) + 6f;
            if (availableHeight > 30f && height > availableHeight)
            {
                height = availableHeight;
            }

            return Mathf.Max(24f, height);
        }

        static TextMeshProUGUI EnsureLabel()
        {
            Transform existing = _overlay.Find(LabelName);
            TextMeshProUGUI label = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
            if (label == null)
            {
                GameObject go = new GameObject(LabelName);
                go.transform.SetParent(_overlay, false);
                label = go.AddComponent<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.TopLeft;
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.enableAutoSizing = false;
                label.raycastTarget = false;
                label.richText = false;
                label.color = new Color(1f, 0.88f, 0.55f, 1f);
                label.fontSize = LabelSize;
                label.lineSpacing = 1.08f;
                label.extraPadding = true;
                label.outlineWidth = 0.18f;
                label.outlineColor = new Color32(0, 0, 0, 225);
                _lastColorMark = -1;
            }

            _label = label;
            return label;
        }

        /// <summary>
        /// The unspent-point counter (TalentWindowView.PointsText). Falls back to the window title
        /// so the line still shows if that element is renamed by a Talent Tree update.
        /// </summary>
        static RectTransform Anchor()
        {
            if (_anchorComponent != null && _anchorComponent)
            {
                return _anchorComponent.transform as RectTransform;
            }

            object view = ViewObject();
            if (view == null)
            {
                return null;
            }

            _anchorComponent = FindComponent(_pointsText, view) ?? FindComponent(_titleText, view);
            return _anchorComponent == null ? null : _anchorComponent.transform as RectTransform;
        }

        /// <summary>
        /// Overlay root: the window panel when found, else the counter's parent. It carries a
        /// nested Canvas with overrideSorting at rootCanvas order + 1 (the SeneaL-safe pattern),
        /// so SeneaL UI's own canvases cannot bury the cap text or the free-respec button.
        /// </summary>
        static RectTransform Host()
        {
            object view = ViewObject();
            RectTransform host = FindComponent(_panelRect, view)?.transform as RectTransform;
            if (host == null)
            {
                RectTransform anchor = Anchor();
                host = anchor == null ? null : anchor.parent as RectTransform;
            }

            if (host == null)
            {
                return null;
            }

            if (_overlay != null && _overlay && _overlay.parent == host)
            {
                return _overlay;
            }

            Transform existing = host.Find(OverlayName);
            GameObject go = existing != null ? existing.gameObject : null;
            if (go == null)
            {
                go = new GameObject(OverlayName);
                go.transform.SetParent(host, false);
            }

            RectTransform overlay = go.GetComponent<RectTransform>();
            if (overlay == null)
            {
                overlay = go.AddComponent<RectTransform>();
            }

            Canvas canvas = go.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = go.AddComponent<Canvas>();
            }

            if (!canvas.overrideSorting)
            {
                canvas.overrideSorting = true;
            }

            Canvas root = canvas.rootCanvas;
            int order = root != null ? root.sortingOrder + 1 : 1;
            if (canvas.sortingOrder != order)
            {
                canvas.sortingOrder = order;
            }

            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;
            overlay.gameObject.SetActive(true);
            _overlay = overlay;
            _overlayCanvas = canvas;
            _label = null;
            _freeRespecButton = null;
            _lastSignature = null;
            return overlay;
        }

        static object ViewObject()
        {
            return _view == null ? null : _view.GetValue(null);
        }

        static Component FindComponent(PropertyInfo property, object view)
        {
            if (property == null || view == null)
            {
                return null;
            }

            return property.GetValue(view, null) as Component;
        }

        static Rect HostBounds(RectTransform parent)
        {
            if (parent == null)
            {
                return new Rect(-500f, -400f, 1000f, 800f);
            }

            Rect rect = parent.rect;
            return new Rect(rect.x, rect.y, rect.width, rect.height);
        }

        /// <summary>Top-left corner of a rect, in the target's local space.</summary>
        static Vector2 TopLeftIn(RectTransform rt, RectTransform target)
        {
            Vector3 world = rt.TransformPoint(rt.rect.xMin, rt.rect.yMax, 0f);
            if (target == null)
            {
                return world;
            }

            return target.InverseTransformPoint(world);
        }

        /// <summary>
        /// Resolve the game's Valheim-AveriaSansLibre TMP font asset. Exact name first, then a
        /// loaded name that starts with it, rescanning at most twice a second while missing.
        /// </summary>
        static TMP_FontAsset FontAsset()
        {
            if (_fontAsset != null && _fontAsset)
            {
                return _fontAsset;
            }

            double now = Time.unscaledTimeAsDouble;
            if (now < _nextFontScan)
            {
                return FallbackFont();
            }

            _nextFontScan = now + 2.0;
            TMP_FontAsset[] all = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (all == null)
            {
                return FallbackFont();
            }

            TMP_FontAsset prefix = null;
            for (int i = 0; i < all.Length; i++)
            {
                TMP_FontAsset candidate = all[i];
                if (candidate == null)
                {
                    continue;
                }

                if (string.Equals(candidate.name, FontAssetName, StringComparison.OrdinalIgnoreCase))
                {
                    _fontAsset = candidate;
                    return candidate;
                }

                if (prefix == null
                    && candidate.name != null
                    && candidate.name.StartsWith(FontAssetName, StringComparison.OrdinalIgnoreCase))
                {
                    prefix = candidate;
                }
            }

            if (prefix != null)
            {
                _fontAsset = prefix;
                return prefix;
            }

            return FallbackFont();
        }

        static TMP_FontAsset FallbackFont()
        {
            TMP_Text text = _anchorComponent as TMP_Text;
            if (text != null && text.font != null)
            {
                return text.font;
            }

            return TMP_Settings.defaultFontAsset;
        }

        static void ApplyFont(TextMeshProUGUI label)
        {
            TMP_FontAsset asset = FontAsset();
            if (asset == null || label.font == asset)
            {
                return;
            }

            label.font = asset;
        }

        static void EnsureFreeRespecButton(TextMeshProUGUI label, float x, float y, Rect bounds, bool visible)
        {
            Transform existing = _overlay.Find(FreeRespecButtonName);
            Button button = existing != null ? existing.GetComponent<Button>() : null;
            if (button == null)
            {
                if (!visible)
                {
                    return;
                }

                GameObject go = new GameObject(FreeRespecButtonName);
                go.transform.SetParent(_overlay, false);
                Image image = go.AddComponent<Image>();
                image.color = new Color(0.13f, 0.36f, 0.13f, 0.96f);
                button = go.AddComponent<Button>();
                GameObject textGo = new GameObject("Text");
                textGo.transform.SetParent(go.transform, false);
                TextMeshProUGUI label2 = textGo.AddComponent<TextMeshProUGUI>();
                label2.alignment = TextAlignmentOptions.MidlineLeft;
                label2.color = Color.white;
                label2.fontSize = 16f;
                label2.margin = new Vector4(12f, 0f, 8f, 0f);
                label2.overflowMode = TextOverflowModes.Ellipsis;
                label2.textWrappingMode = TextWrappingModes.NoWrap;
                label2.raycastTarget = false;
                label2.text = "Free respec (over talent limit)";
                label2.outlineWidth = 0.15f;
                label2.outlineColor = new Color32(0, 0, 0, 220);
                ApplyFont(label2);
                RectTransform textRt = label2.rectTransform;
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = Vector2.zero;
                textRt.offsetMax = Vector2.zero;
                button.onClick.AddListener(OnFreeRespecClicked);
                _freeRespecButton = button;
            }

            RectTransform rt = button.GetComponent<RectTransform>();
            float width = Mathf.Min(ButtonWidth, Mathf.Max(160f, bounds.width - SideMargin * 2f));
            float by = y;
            float ceiling = bounds.yMax - SideMargin - ButtonHeight;
            float floorY = bounds.yMin + SideMargin;
            if (by > ceiling)
            {
                by = ceiling;
            }

            if (by < floorY)
            {
                by = floorY;
            }

            float bx = Mathf.Min(x, bounds.xMax - width - SideMargin);
            if (bx < bounds.xMin + SideMargin)
            {
                bx = bounds.xMin + SideMargin;
            }
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(width, ButtonHeight);
            rt.anchoredPosition = new Vector2(bx - bounds.xMin, by - bounds.yMax);
            button.gameObject.SetActive(visible);
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
            _lastSignature = null;
            _nextSample = 0f;
            // UI.TalentWindow.Refresh is a static method (verified via reflection).
            if (_windowRefresh != null)
            {
                _windowRefresh.Invoke(null, null);
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
    }
}
