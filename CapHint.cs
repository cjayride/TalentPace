using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cjayride.TalentPace
{
    /// <summary>
    /// Shows the talent point cap on the Talent Tree window, on the left, under the unspent
    /// points counter (TalentWindowView.PointsText), plus a free respec button when the player
    /// is over the cap.
    ///
    /// Layout: one TextMeshProUGUI row per hint line, placed by our own math. The hint text
    /// carries '\n', but Valheim's shipped Valheim-AveriaSansLibre is a fallback-style font
    /// asset whose face metrics can give TMP a zero line height, which stacks every line on
    /// the same baseline ("all the text mashed together on one line"). Row-per-line placement
    /// does not depend on the font's line metrics at all, so that failure mode cannot happen.
    ///
    /// Visibility: the rows and the button live on a child Canvas with overrideSorting at
    /// rootCanvas.sortingOrder + 1 — the same pattern SeneaL UI itself uses (HudRoot.cs) — so
    /// SeneaL's own canvases (fixed orders 28000-32600) cannot cover them. All elements are
    /// clamped inside the window panel.
    /// </summary>
    static class CapHint
    {
        const string OverlayName = "TalentPaceOverlay";
        const string BlockName = "TalentPaceCap";
        const string RowName = "TalentPaceCapLine";
        const string FreeRespecButtonName = "TalentPaceFreeRespec";
        const string FontAssetName = "Valheim-AveriaSansLibre";
        const float RefreshInterval = 0.25f;
        const float LabelSize = 17f;
        const float RowGap = 3f;
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
        static RectTransform _block;
        static readonly List<TextMeshProUGUI> _rows = new List<TextMeshProUGUI>();
        static Button _freeRespecButton;
        static Component _anchorComponent;
        static string _lastSignature;
        static int _lastColorMark = -1;
        static TMP_FontAsset _fontAsset;
        static int _fontState;          // 0 = unresolved, 1 = validated asset, 2 = rejected (use fallback)
        static double _nextFontScan;
        static FieldInfo _faceInfoField;
        static FieldInfo _lineHeightField;
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

            string[] lines = hint.Split('\n');
            bool needsFree = Pace.NeedsFreeRespec(player);
            int colorMark = needsFree ? 2 : 1;
            Vector2 anchorTopLeft = TopLeftIn(anchor, _overlay.parent as RectTransform);
            string signature = hint + '|' + needsFree + '|' + colorMark
                + '|' + anchorTopLeft.x.ToString("0.0") + '|' + anchorTopLeft.y.ToString("0.0");
            if (signature == _lastSignature && _block != null && _block.gameObject.activeInHierarchy)
            {
                return;
            }

            _lastSignature = signature;

            Rect bounds = HostBounds(_overlay.parent as RectTransform);
            float width = LabelWidth(anchor, host, bounds);
            float top = anchorTopLeft.y - Gap;
            float x = Mathf.Max(bounds.xMin + SideMargin, anchorTopLeft.x);

            TMP_FontAsset font = PickFont();
            float[] heights = new float[lines.Length];
            float total = 0f;
            for (int i = 0; i < lines.Length; i++)
            {
                TextMeshProUGUI row = EnsureRow(i, font);
                if (colorMark != _lastColorMark)
                {
                    row.color = needsFree
                        ? new Color(1f, 0.5f, 0.42f, 1f)
                        : new Color(1f, 0.88f, 0.55f, 1f);
                }

                string text = lines[i].TrimEnd('\r');
                if (row.text != text)
                {
                    row.SetText(text);
                }

                heights[i] = RowHeight(row, width);
                total += heights[i];
            }

            if (lines.Length > 0)
            {
                total += RowGap * (lines.Length - 1);
            }

            for (int i = lines.Length; i < _rows.Count; i++)
            {
                _rows[i].gameObject.SetActive(false);
            }

            if (colorMark != _lastColorMark)
            {
                _lastColorMark = colorMark;
            }

            float buttonRoom = needsFree ? ButtonHeight + 8f : 0f;
            float block = total + buttonRoom;

            // Keep the whole block (rows + button) inside the panel.
            float minTop = bounds.yMin + SideMargin + block;
            if (top < minTop)
            {
                top = minTop;
            }

            RectTransform blockRt = _block;
            blockRt.anchorMin = new Vector2(0f, 1f);
            blockRt.anchorMax = new Vector2(0f, 1f);
            blockRt.pivot = new Vector2(0f, 1f);
            blockRt.sizeDelta = new Vector2(width, total);
            blockRt.anchoredPosition = new Vector2(x - bounds.xMin, top - bounds.yMax);

            float y = 0f;
            for (int i = 0; i < lines.Length; i++)
            {
                RectTransform rowRt = _rows[i].rectTransform;
                rowRt.anchorMin = new Vector2(0f, 1f);
                rowRt.anchorMax = new Vector2(0f, 1f);
                rowRt.pivot = new Vector2(0f, 1f);
                rowRt.sizeDelta = new Vector2(width, heights[i]);
                rowRt.anchoredPosition = new Vector2(0f, -y);
                y += heights[i] + RowGap;
            }

            EnsureFreeRespecButton(x, top - total - 8f, bounds, needsFree);

            if (!_logged)
            {
                _logged = true;
                Plugin.Log("Talent window cap line active (row layout, overlay canvas, font: "
                    + (font != null ? font.name : "default") + ").");
            }
        }

        static float LabelWidth(RectTransform anchor, RectTransform host, Rect bounds)
        {
            float fromPanel = host.rect.width - SideMargin * 2f;
            return Mathf.Clamp(Mathf.Max(420f, anchor.rect.width * 1.15f), 420f, Mathf.Max(420f, fromPanel, bounds.width - SideMargin * 2f));
        }

        static float RowHeight(TextMeshProUGUI row, float width)
        {
            float min = LabelSize * 1.15f;
            float preferred = 0f;
            try
            {
                preferred = row.GetPreferredValues(row.text, width, 0f).y;
            }
            catch (Exception)
            {
                preferred = 0f;
            }

            if (preferred <= 1f)
            {
                preferred = row.preferredHeight;
            }

            if (preferred <= 1f)
            {
                // Font gave us no usable metrics: a fixed row height keeps lines separated.
                preferred = LabelSize * 1.35f;
            }

            return Mathf.Max(min, Mathf.Ceil(preferred) + 2f);
        }

        static TextMeshProUGUI EnsureRow(int index, TMP_FontAsset font)
        {
            while (_rows.Count <= index)
            {
                int n = _rows.Count;
                GameObject go = new GameObject(RowName + n);
                go.transform.SetParent(_block, false);
                TextMeshProUGUI row = go.AddComponent<TextMeshProUGUI>();
                row.alignment = TextAlignmentOptions.TopLeft;
                row.overflowMode = TextOverflowModes.Overflow;
                row.textWrappingMode = TextWrappingModes.Normal;
                row.enableAutoSizing = false;
                row.raycastTarget = false;
                row.richText = false;
                row.fontSize = LabelSize;
                row.lineSpacing = 1.08f;
                row.characterSpacing = 0f;
                row.wordSpacing = 0f;
                row.paragraphSpacing = 0f;
                row.extraPadding = true;
                row.outlineWidth = 0.18f;
                row.outlineColor = new Color32(0, 0, 0, 225);
                row.color = new Color(1f, 0.88f, 0.55f, 1f);
                _rows.Add(row);
            }

            TextMeshProUGUI target = _rows[index];
            target.gameObject.SetActive(true);
            if (font != null && !ReferenceEquals(target.font, font))
            {
                target.font = font;
            }

            return target;
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

            Transform blockT = overlay.Find(BlockName);
            GameObject blockGo = blockT != null ? blockT.gameObject : null;
            if (blockGo == null)
            {
                blockGo = new GameObject(BlockName);
                blockGo.transform.SetParent(overlay, false);
            }

            RectTransform block = blockGo.GetComponent<RectTransform>();
            if (block == null)
            {
                block = blockGo.AddComponent<RectTransform>();
            }

            _overlay = overlay;
            _block = block;
            _rows.Clear();
            Transform[] children = block.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child == block)
                {
                    continue;
                }

                TextMeshProUGUI row = child.GetComponent<TextMeshProUGUI>();
                if (row != null && child.name.StartsWith(RowName, StringComparison.Ordinal))
                {
                    _rows.Add(row);
                }
            }

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
        /// Resolve a font for the rows: Valheim-AveriaSansLibre (the name the user asked for)
        /// ONLY if it is usable — it must carry the space and 'A' glyphs and report a positive
        /// face line height. Valheim's shipped Averia asset is a fallback-style asset; a
        /// degenerate face (zero line height) is what stacks text into one unreadable pile.
        /// Rejected: the counter's own TMP font, then TMP's default asset, then no change.
        /// </summary>
        static TMP_FontAsset PickFont()
        {
            if (_fontState == 1)
            {
                return _fontAsset;
            }

            if (_fontState == 2)
            {
                return FallbackFont();
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

            TMP_FontAsset exact = null;
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
                    exact = candidate;
                    break;
                }

                if (prefix == null
                    && candidate.name != null
                    && candidate.name.StartsWith(FontAssetName, StringComparison.OrdinalIgnoreCase))
                {
                    prefix = candidate;
                }
            }

            TMP_FontAsset chosen = exact ?? prefix;
            if (chosen == null)
            {
                return FallbackFont();
            }

            if (!FontIsUsable(chosen))
            {
                _fontState = 2;
                Plugin.Log("Font '" + chosen.name + "' is not safe for multi-line text; using the window's own font.");
                return FallbackFont();
            }

            _fontAsset = chosen;
            _fontState = 1;
            return chosen;
        }

        static TMP_FontAsset FallbackFont()
        {
            TMP_Text text = _anchorComponent as TMP_Text;
            if (text != null && text.font != null && FontIsUsable(text.font))
            {
                return text.font;
            }

            TMP_FontAsset def = TMP_Settings.defaultFontAsset;
            if (def != null && FontIsUsable(def))
            {
                return def;
            }

            return null;
        }

        static bool FontIsUsable(TMP_FontAsset asset)
        {
            if (asset == null)
            {
                return false;
            }

            bool hasSpace;
            bool hasA;
            try
            {
                hasSpace = asset.HasCharacter(' ');
                hasA = asset.HasCharacter('A');
            }
            catch (Exception)
            {
                return true; // cannot introspect: trust the asset
            }

            if (!hasSpace || !hasA)
            {
                return false;
            }

            _faceInfoField ??= AccessTools.Field(typeof(TMP_FontAsset), "m_FaceInfo");
            if (_faceInfoField == null)
            {
                return true;
            }

            object face;
            try
            {
                face = _faceInfoField.GetValue(asset);
            }
            catch (Exception)
            {
                return true;
            }

            if (face == null)
            {
                return true;
            }

            Type faceType = face.GetType();
            if (_lineHeightField == null)
            {
                _lineHeightField = AccessTools.Field(faceType, "LineHeight")
                    ?? AccessTools.Field(faceType, "lineHeight");
            }

            if (_lineHeightField == null)
            {
                return true;
            }

            try
            {
                object value = _lineHeightField.GetValue(face);
                float lineHeight = Convert.ToSingle(value);
                return lineHeight > 0f;
            }
            catch (Exception)
            {
                return true;
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
                TMP_FontAsset font = PickFont();
                if (font != null)
                {
                    label2.font = font;
                }

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
