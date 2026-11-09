using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cjayride.TalentPace
{
    /// <summary>
    /// Shows the talent point cap on the Talent Tree window, on the LEFT, starting BELOW the
    /// unspent points counter (TalentWindowView.PointsText), plus a free respec button when the
    /// player is over the cap.
    ///
    /// Rendering uses the legacy UnityEngine.UI.Text with the window's OWN font (the title's
    /// AveriaSansLibre TrueType font) — exactly the path that rendered correctly from the first
    /// version. The TMP "Valheim-AveriaSansLibre" asset is a fallback atlas (CJK/emoji) whose
    /// material renders plain Latin text as garbled glyphs; it must not be used for this label.
    ///
    /// Visibility: the label and the button live on a child Canvas with overrideSorting at
    /// rootCanvas.sortingOrder + 1 — the same pattern SeneaL UI itself uses (HudRoot.cs) — so
    /// SeneaL's own canvases (fixed orders 28000-32600) cannot cover them. All elements are
    /// clamped inside the window panel.
    /// </summary>
    static class CapHint
    {
        const string OverlayName = "TalentPaceOverlay";
        const string LabelName = "TalentPaceCap";
        const string FreeRespecButtonName = "TalentPaceFreeRespec";
        const float RefreshInterval = 0.25f;
        const float ButtonWidth = 320f;
        const float ButtonHeight = 34f;
        const float SideMargin = 10f;
        const float Gap = 8f;

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
        static Text _label;
        static Button _freeRespecButton;
        static Component _anchorComponent;
        static Text _titleFontSource;
        static string _lastSignature;
        static int _lastColorMark = -1;
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
            // The block starts BELOW the counter: its top = the counter's bottom edge.
            Vector2 anchorBottomLeft = BottomLeftIn(anchor, _overlay.parent as RectTransform);
            string signature = hint + '|' + needsFree
                + '|' + anchorBottomLeft.x.ToString("0.0") + '|' + anchorBottomLeft.y.ToString("0.0");
            if (signature == _lastSignature && _label != null && _label.gameObject.activeInHierarchy)
            {
                return;
            }

            _lastSignature = signature;

            Text label = EnsureLabel();
            Rect bounds = HostBounds(_overlay.parent as RectTransform);
            float width = LabelWidth(anchor, host, bounds);
            float top = anchorBottomLeft.y - Gap;
            float x = Mathf.Max(bounds.xMin + SideMargin, anchorBottomLeft.x);

            label.text = hint;
            if (colorMark != _lastColorMark)
            {
                _lastColorMark = colorMark;
                label.color = needsFree
                    ? new Color(1f, 0.45f, 0.35f, 1f)
                    : new Color(1f, 0.82f, 0.35f, 1f);
            }

            RectTransform labelRt = label.GetComponent<RectTransform>();
            float height = LabelHeight(label);

            // Keep the whole label + button block inside the panel.
            float buttonRoom = needsFree ? ButtonHeight + 8f : 0f;
            float minTop = bounds.yMin + SideMargin + height + buttonRoom;
            if (top < minTop)
            {
                top = minTop;
            }

            labelRt.anchorMin = new Vector2(0f, 1f);
            labelRt.anchorMax = new Vector2(0f, 1f);
            labelRt.pivot = new Vector2(0f, 1f);
            labelRt.sizeDelta = new Vector2(width, height);
            labelRt.anchoredPosition = new Vector2(x - bounds.xMin, top - bounds.yMax);

            EnsureFreeRespecButton(x, top - height - 8f, bounds, needsFree);

            if (!_logged)
            {
                _logged = true;
                Plugin.Log("Talent window cap line active (legacy Text, window font, below the point counter).");
            }
        }

        static float LabelWidth(RectTransform anchor, RectTransform host, Rect bounds)
        {
            float fromPanel = host.rect.width - SideMargin * 2f;
            return Mathf.Clamp(Mathf.Max(420f, anchor.rect.width * 1.15f), 420f, Mathf.Max(420f, fromPanel, bounds.width - SideMargin * 2f));
        }

        static float LabelHeight(Text label)
        {
            float min = label.fontSize * 1.2f;
            float preferred = 0f;
            try
            {
                preferred = label.preferredHeight;
            }
            catch (Exception)
            {
                preferred = 0f;
            }

            if (preferred <= 1f)
            {
                preferred = label.fontSize * 1.4f * 6f; // fall back to counting on the font size
            }

            return Mathf.Max(min, Mathf.Ceil(preferred) + 4f);
        }

        static Text EnsureLabel()
        {
            if (_label != null && _label && _label.transform.parent == _overlay)
            {
                return _label;
            }

            Transform existing = _overlay.Find(LabelName);
            Text label = existing != null ? existing.GetComponent<Text>() : null;
            if (label == null)
            {
                GameObject go = new GameObject(LabelName);
                go.transform.SetParent(_overlay, false);
                label = go.AddComponent<Text>();
                label.alignment = TextAnchor.UpperLeft;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                label.supportRichText = false;
                label.raycastTarget = false;
                label.color = new Color(1f, 0.82f, 0.35f, 1f);
                // Readability comes from the game's own text material (AveriaSansLibre
                // Font Material, Opaque — the one the window title uses), same as the
                // centered version that rendered correctly. No extra shadow component.
            }

            ApplyWindowFont(label);
            _label = label;
            _lastColorMark = -1;
            return label;
        }

        /// <summary>
        /// Copy the font from the window's own title text (the game's AveriaSansLibre TrueType
        /// font, dynamic, real line metrics). This is the rendering that worked from the first
        /// version; no TMP font assets, no outline material.
        /// </summary>
        static void ApplyWindowFont(Text label)
        {
            Text title = TitleText();
            if (title == null)
            {
                return;
            }

            if (title.font != null && !ReferenceEquals(label.font, title.font))
            {
                label.font = title.font;
            }

            // Match the window title's text material (the game's AveriaSansLibre Font
            // Material, Opaque). The first centered version worked with the default
            // material; copying the title's keeps every line the game draws consistent.
            if (title.material != null && !ReferenceEquals(label.material, title.material))
            {
                label.material = title.material;
            }

            int size = Mathf.Clamp(Mathf.RoundToInt(title.fontSize * 0.58f), 15, 20);
            if (label.fontSize != size)
            {
                label.fontSize = size;
            }
        }

        static Text TitleText()
        {
            if (_titleFontSource != null && _titleFontSource)
            {
                return _titleFontSource;
            }

            object view = ViewObject();
            _titleFontSource = FindComponent(_titleText, view) as Text;
            return _titleFontSource;
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
        /// Overlay root: the window panel when found, else the counter's parent. Carries a nested
        /// Canvas with overrideSorting at rootCanvas order + 1 (SeneaL's own pattern).
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
            _label = null;
            _freeRespecButton = null;
            _lastSignature = null;
            _lastColorMark = -1;
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

        /// <summary>Bottom-left corner of a rect, in the target's local space.</summary>
        static Vector2 BottomLeftIn(RectTransform rt, RectTransform target)
        {
            Vector3 world = rt.TransformPoint(rt.rect.xMin, rt.rect.yMin, 0f);
            if (target == null)
            {
                return world;
            }

            return target.InverseTransformPoint(world);
        }

        static void EnsureFreeRespecButton(float x, float y, Rect bounds, bool visible)
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
                image.color = new Color(0.15f, 0.35f, 0.12f, 0.95f);
                button = go.AddComponent<Button>();
                GameObject textGo = new GameObject("Text");
                textGo.transform.SetParent(go.transform, false);
                Text label = textGo.AddComponent<Text>();
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.supportRichText = false;
                label.raycastTarget = false;
                label.text = "Free respec (over talent limit)";
                ApplyWindowFont(label);
                int size = Mathf.Clamp(Mathf.RoundToInt((TitleText()?.fontSize ?? 28f) * 0.55f), 14, 18);
                label.fontSize = size;
                RectTransform textRt = label.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = Vector2.zero;
                textRt.offsetMax = Vector2.zero;
                button.onClick.AddListener(OnFreeRespecClicked);
                _freeRespecButton = button;
            }

            RectTransform rt = button.GetComponent<RectTransform>();
            float width = Mathf.Min(ButtonWidth, Mathf.Max(160f, bounds.width - SideMargin * 2f));
            float bx = Mathf.Min(x, bounds.xMax - width - SideMargin);
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
