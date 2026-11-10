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
        // The description block sits this far below the unspent-points counter.
        const float Gap = 18f;
        const int OverlayOrder = 1;
        const int StatsOrder = 2;

        static FieldInfo _view;
        static FieldInfo _statsPanel;
        static PropertyInfo _statsRoot;
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

        // Clean sans-serif fonts, in preference order; Unity picks the first installed one.
        // Used when the config entry is blank, and as the fallback when a configured list
        // names no installed font.
        static readonly string[] DefaultReadFontNames = { "Verdana", "Segoe UI", "Arial", "Tahoma" };

        // Bound in Plugin.Awake; exposed in the config file.
        internal static BepInEx.Configuration.ConfigEntry<string> FontNames;
        internal static BepInEx.Configuration.ConfigEntry<UnityEngine.FontStyle> FontStyle;
        internal static BepInEx.Configuration.ConfigEntry<int> FontSize;
        internal static BepInEx.Configuration.ConfigEntry<bool> ListInstalledFonts;

        public static void Apply(Harmony harmony)
        {
            if (ListInstalledFonts != null && ListInstalledFonts.Value)
            {
                LogAvailableFonts();
            }

            Type window = TT.Type("UI.TalentWindow");
            MethodInfo refresh = window == null ? null : AccessTools.DeclaredMethod(window, "Refresh");
            if (refresh == null)
            {
                Plugin.Log("Talent window refresh was not found. The cap line is off.");
                return;
            }

            _windowRefresh = refresh;
            _view = AccessTools.Field(window, "_view");
            _statsPanel = AccessTools.DeclaredField(window, "_statsPanel");
            Type statsView = TT.Type("UI.StatsPanelView");
            _statsRoot = statsView == null ? null : AccessTools.Property(statsView, "Root");
            // The Stats button shows the panel through StatsPanelView.SetVisible(true) plus
            // its own Refresh; UI.TalentWindow.Refresh does NOT run for that click, so the
            // reparent must hook the show itself, in the same frame.
            MethodInfo statsShow = statsView == null ? null : AccessTools.DeclaredMethod(statsView, "SetVisible");
            if (statsShow != null)
            {
                harmony.Patch(statsShow, postfix: new HarmonyMethod(typeof(CapHint), nameof(AfterStatsShow)));
            }
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
            // Runs on EVERY refresh, not on the text throttle: tree zoom/pan rebuilds the
            // stats panel back into the base window canvas, and a throttled re-assert left
            // it under the overlay text for one tick after a Stats click.
            EnsureStatsOnTop();

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
                + '|' + anchorBottomLeft.x.ToString("0.0") + '|' + anchorBottomLeft.y.ToString("0.0")
                + '|' + (FontNames == null ? "" : FontNames.Value)
                + '|' + (FontStyle == null ? "" : FontStyle.Value.ToString())
                + '|' + (FontSize == null ? "0" : FontSize.Value.ToString());
            if (signature == _lastSignature && _label != null && _label.gameObject.activeInHierarchy)
            {
                return;
            }

            _lastSignature = signature;

            Text label = EnsureLabel();
            // Re-apply the font on every signature change, including config-only changes:
            // EnsureLabel keeps the existing label, so font/style/size updates from the
            // F1 menu must be pushed here to take effect while the window stays open.
            ApplyWindowFont(label);
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
                label.supportRichText = true;
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
        /// Font for this text, chosen by the config list (first match wins):
        /// "Stats*"   = the SAME font the Talent Tree Stats window labels use (game asset
        ///              AveriaSerifBold, read live from the panel so it follows the mod).
        /// "Valheim-*"= the Talent Tree window title's own face.
        /// otherwise  = Windows font list (first installed one wins).
        /// Fallbacks keep the text readable if a token's panel is not built yet. Size:
        /// config value, or auto from the title. Style: config (Normal/Bold/...).
        /// </summary>
        static void ApplyWindowFont(Text label)
        {
            Text title = TitleText();
            int configSize = FontSize == null ? 0 : FontSize.Value;
            int size = configSize > 0
                ? configSize
                : (title != null ? Mathf.Clamp(Mathf.RoundToInt(title.fontSize * 0.58f), 15, 20) : 17);

            Font font = null;
            if (StatsFontRequested())
            {
                font = StatsFontAsset();
            }

            if (font == null && WindowFontRequested())
            {
                font = TitleFontAsset();
            }

            if (font == null)
            {
                font = ReadFont(size);
            }

            if (font == null)
            {
                font = TitleFontAsset();
            }

            if (font != null && !ReferenceEquals(label.font, font))
            {
                label.font = font;
                if (!_loggedFont)
                {
                    _loggedFont = true;
                    Plugin.Log("Talent Pace: description text font = " + font.name + " (requested: " + (FontNames == null ? "" : FontNames.Value) + ").");
                }
            }

            if (label.fontSize != size)
            {
                label.fontSize = size;
            }

            UnityEngine.FontStyle style = FontStyle == null ? UnityEngine.FontStyle.Normal : FontStyle.Value;
            if (label.fontStyle != style)
            {
                label.fontStyle = style;
            }
        }

        static Font _readFont;
        static string _readFontKey;
        static bool _loggedFont;

        /// <summary>
        /// Any entry starting with "Valheim-" = the Talent Tree window's own font asset
        /// (the window title's face), not a Windows font. Legacy Text + that Font renders
        /// like the window title (the TMP "Valheim-*" atlas asset must NOT be used).
        /// </summary>
        static bool WindowFontRequested()
        {
            return NamesRequested("Valheim-");
        }

        /// <summary>
        /// Any entry starting with "Stats" = the SAME font the Talent Tree Stats panel
        /// labels use (the game's AveriaSerifBold asset, read from the live panel so it
        /// follows the mod's own choice).
        /// </summary>
        static bool StatsFontRequested()
        {
            return NamesRequested("Stats");
        }

        static bool NamesRequested(string prefix)
        {
            string raw = FontNames == null ? "" : FontNames.Value;
            foreach (string part in raw.Split(','))
            {
                if (part.Trim().StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        static Font _statsFont;

        static Font StatsFontAsset()
        {
            Font font = null;
            object panel = _statsPanel == null ? null : _statsPanel.GetValue(null);
            object rootObj = panel == null || _statsRoot == null ? null : _statsRoot.GetValue(panel, null);
            Transform tr = rootObj as Transform;
            GameObject go = tr != null ? tr.gameObject : rootObj as GameObject;
            if (go != null)
            {
                // The panel is built hidden, so include inactive children. Runs only when
                // the label signature changes (font config edits), not per frame.
                Text[] texts = go.GetComponentsInChildren<Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    Font found = texts[i].font;
                    if (found != null)
                    {
                        font = found;
                        break;
                    }
                }
            }

            if (font != null)
            {
                _statsFont = font;
                return font;
            }

            return _statsFont;
        }

        static Font _titleFont;

        static Font TitleFontAsset()
        {
            Text title = TitleText();
            Font font = title != null ? title.font : null;
            if (font != null)
            {
                _titleFont = font;
            }

            return font;
        }

        static Font ReadFont(int size)
        {
            string[] names = FontNameList();
            string key = string.Join("|", names) + "@" + size;
            if (_readFont != null && _readFontKey == key)
            {
                return _readFont;
            }

            Font font = null;
            if (names.Length > 0)
            {
                try
                {
                    font = Font.CreateDynamicFontFromOSFont(names, size);
                }
                catch (Exception)
                {
                    font = null;
                }
            }

            if (font != null)
            {
                _readFont = font;
                _readFontKey = key;
            }

            return font;
        }

        static string[] FontNameList()
        {
            string raw = FontNames == null ? "" : FontNames.Value;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return DefaultReadFontNames;
            }

            string[] parts = raw.Split(',');
            List<string> clean = new List<string>();
            foreach (string part in parts)
            {
                string name = part.Trim();
                if (name.Length > 0)
                {
                    clean.Add(name);
                }
            }

            return clean.Count > 0 ? clean.ToArray() : DefaultReadFontNames;
        }

        /// <summary>
        /// One-time log of the fonts this machine offers, so the config list can be filled in.
        /// </summary>
        static void LogAvailableFonts()
        {
            try
            {
                string[] paths = Font.GetPathsToOSFonts();
                if (paths == null || paths.Length == 0)
                {
                    Plugin.Log("Talent Pace: no OS font files were reported; the description text falls back to the window font.");
                    return;
                }

                List<string> names = new List<string>();
                foreach (string path in paths)
                {
                    string name = System.IO.Path.GetFileNameWithoutExtension(path);
                    if (!names.Contains(name))
                    {
                        names.Add(name);
                    }
                }

                Plugin.Log("Talent Pace: available fonts (" + names.Count + "): " + string.Join(", ", names.ToArray()));
            }
            catch (Exception ex)
            {
                Plugin.Log("Talent Pace: the font list could not be read: " + ex.Message);
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
            int order = root != null ? root.sortingOrder + 1 : OverlayOrder;
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

        /// <summary>
        /// The Talent Tree "Stats" panel is a plain Image+Text panel, a sibling of the window
        /// content inside the window canvas; Talent Tree's ApplyZOrder orders it purely by
        /// sibling index (SetAsLastSibling). Unity draws same-canvas siblings in one pass, so a
        /// sorting-order bump on a nested canvas is unreliable against our overlay canvas.
        /// Proven pattern instead: move the stats root INSIDE our overlay and re-assert it as
        /// the LAST sibling every refresh — same canvas, last sibling, always on top of the
        /// text. The overlay stretches over the panel rect, so the stats layout is unchanged.
        /// </summary>
        static void AfterStatsShow(bool visible)
        {
            if (visible)
            {
                EnsureStatsOnTop();
            }
        }

        /// <summary>
        /// Keep the overlay canvas directly above the window's canvas. Opening the map
        /// re-focuses the talent window and the game RAISES its canvas sortingOrder; an
        /// order captured once at creation goes stale, and then the whole base canvas
        /// (stats panel included) draws on top of the overlay. Re-derive every refresh.
        /// </summary>
        static void EnsureOverlayOrder()
        {
            if (_overlay == null)
            {
                return;
            }

            Canvas canvas = _overlay.GetComponent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            Canvas root = canvas.rootCanvas;
            int order = root != null ? root.sortingOrder + 1 : OverlayOrder;
            if (canvas.sortingOrder != order)
            {
                canvas.sortingOrder = order;
            }
        }

        static void EnsureStatsOnTop()
        {
            if (_statsPanel == null || _statsRoot == null || _overlay == null)
            {
                return;
            }

            EnsureOverlayOrder();

            object panel = _statsPanel.GetValue(null);
            if (panel == null)
            {
                return;
            }

            object rootObj = _statsRoot.GetValue(panel, null);
            Transform tr = rootObj as Transform;
            GameObject go = tr != null ? tr.gameObject : rootObj as GameObject;
            if (go == null)
            {
                return;
            }

            if (go.transform.parent != _overlay)
            {
                go.transform.SetParent(_overlay, false);
            }

            if (go.transform.GetSiblingIndex() != _overlay.childCount - 1)
            {
                go.transform.SetAsLastSibling();
            }

            // Fully opaque background: Talent Tree builds the panel with the translucent
            // HoverTooltipBackground color, so tree elements show through and make the
            // rows hard to read. Keep the theme RGB, force alpha to 1. The panel is
            // rebuilt with the translucent color every window open, so re-assert it
            // on every refresh (idempotent, no allocation when already opaque).
            Image bg = go.GetComponent<Image>();
            if (bg != null)
            {
                Color c = bg.color;
                if (c.a < 0.999f)
                {
                    c.a = 1f;
                    bg.color = c;
                }
            }
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
                label.supportRichText = true;
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
