# TalentPace 1.2.7

## 1.2.7 (2026-11-09, this turn) — FIX: cryptic font + text too high
**User report:** "displaying funny… also too high, needs to start below the number of the unspent talent points. The font looks cryptic, very strange… the text was displaying correctly the very first attempt when you had the text centered."

**Diagnosis (user's clue was the key):** the first version rendered with **legacy `UnityEngine.UI.Text` + `title.font`** (the window title's AveriaSansLibre TrueType font) — no TMP asset. The 1.2.5/1.2.6 TMP path forced the `Valheim-AveriaSansLibre` TMP asset, which is a **fallback atlas** asset (SeneaL's Fonts.cs trusts it only conditionally) whose material renders plain Latin as garbled glyphs; the TMP outline (`outlineWidth`) needs an Outline-capable material the fallback asset's shader doesn't provide. So: font + outline, not layout.

**Fix (CapHint.cs rewritten to the proven path):**
- Rendering back to **legacy `Text`**: `label.font = title.font`, `label.material = title.material` (the window's own AveriaSansLibre Font Material), size = `title.fontSize * 0.58` clamped 15-20, `supportRichText = false`, gold `1,0.82,0.35` / red `1,0.45,0.35` (the 1.2.4 colors). **No TMP, no outline, no Shadow component** (verified gone from the DLL; legacy APIs `supportRichText`/`horizontalOverflow`/`preferredHeight`/`set_material` verified present).
- **Position: starts below the counter number** — anchor is now `BottomLeftIn(PointsText)` (bottom edge) instead of the top edge, `Gap = 8`, left-aligned, panel clamping kept.
- Free respec button uses the same window font/material.
- Kept: overlay canvas (overrideSorting = rootCanvas+1), static `Refresh` postfix, `PointsText`→`TitleText` fallback anchor, Sample/TrimRanks logic untouched.

**Build:** `dotnet build -c Release` → 0 Error(s) (fixed CS1061: legacy `Text` has no `fontMaterial`; used `material`). DLL 44,032 B v1.2.7.0. String scan: `TalentPaceCapLine`/`Valheim-AveriaSansLibre`/`TextMeshProUGUI`/`outlineWidth`/`TMP_Settings` all **gone**; overlay/label/button names and hint strings present.
**Zip:** `dist\cjayride-TalentPace-1.2.7.zip`, inner DLL SHA256 `D9595128…D145` == bin.
**GeometryCheck.ps1** ported to 1.2.7 math (block top = counter bottom, gap 8): 7/7 PASS.
**Version 1.2.6→1.2.7** in csproj/Plugin.cs/manifest. Docs mirrored. Packs: manifests → 1.2.7, CHANGELOG 1.2.7 line (user's wording preserved), cfg headers → v1.2.7 client/server hash-checked.
**Not verified:** in-game.

## 1.2.6 (2026-11-09, this turn) — FIX: all text mashed onto one line
**User report:** the top-left text near the unspent-points counter renders "all on the same line mashed together, not readable".

**Diagnosis:** the single TMP text block relied on the font's own line metrics. Valheim's shipped `Valheim-AveriaSansLibre` is a fallback-style asset (SeneaL's Fonts.cs picks it only conditionally, checking `fallbackFontAssetTable`); such assets can report a zero/degenerate face line height, so TMP stacks every line at the same baseline → one pile of overlapping glyphs. (No screenshot arrived; symptom is the exact signature. `<br>` tags do not exist in Valheim's TMP build — string scan — so `\n` was the only line separator and it depends entirely on line height.)

**Fix (CapHint.cs rewrite):**
- **One TextMeshProUGUI row per hint line**, positioned by our own layout math (`EnsureRow`/`RowHeight`, fixed `RowGap = 3`). Line separation no longer depends on any font metric.
- **Font validation** (`FontIsUsable`): candidate must pass `HasCharacter(' ')` + `HasCharacter('A')` and report `m_FaceInfo.LineHeight > 0` (private-field reflection, verified present in the shipped Unity.TextMeshPro.dll). `Valheim-AveriaSansLibre` used only if usable; otherwise the anchor counter's own TMP font, then `TMP_Settings.defaultFontAsset`; reject state is sticky with a one-time log line.
- Row height floor = fontSize*1.15, fallback 23px when metrics are useless → lines can never collapse.
- Kept: overlay canvas (overrideSorting, rootCanvas+1), panel clamping, block shift, free-respec button, static Refresh invoke.

**Build:** `dotnet build -c Release` → 0 Error(s) (one CS1061 on `_block.rectTransform` fixed — RectTransform *is* the rect transform). DLL 46,592 B v1.2.6.0. Verified in DLL: `TalentPaceCapLine`, `Valheim-AveriaSansLibre`, `is not safe for multi-line text`, `HasCharacter`, `m_FaceInfo`/`LineHeight` (UTF-16 heap).
**Zip:** `dist\cjayride-TalentPace-1.2.6.zip`, inner DLL SHA256 `C97E2F58…9471` == bin.
**GeometryCheck.ps1** updated for the row layout (rows ≈24px + 3 gap): 7/7 cases PASS.
**Version:** csproj/Plugin.cs/manifest → 1.2.6. Docs: Package CHANGELOG/README + mirrors; both pack manifests → `cjayride-TalentPace-1.2.6`; both pack CHANGELOGs get a 1.2.6 FIX line under the user's 4.4.43 heading (their 1.2.5 wording preserved); pack cfg headers → v1.2.6, client/server hash-identical.
**Not verified:** in-game (user-side). The old 1.2.5 zip/release are superseded by 1.2.6 — push + GitHub release + tag for 1.2.6 not yet done.

## 1.2.5 (2026-11-09, this turn) — cap text placement, font, wording
**User request:** move the helper text left, under the unspent-points counter (was centered); use the `Valheim-AveriaSansLibre` font; reword to `Current Progress: Bonemass` / `To Unlock Points: Kill Moder` / `Points Earned by XP: 47` / `Points Spent: 53` / `Points Allowed: 58` / `Kill Moder to Raise Limit to 72 Points`.

**What changed:**
- `CapHint.cs` rewritten: the label is now **TextMeshProUGUI** (was legacy `Text`) so the TMP font asset can be set; anchored **top-left under `TalentWindowView.PointsText`** (the unspent counter), falling back to `TitleText` if that element is ever renamed; parent-relative placement via `TransformPoint`/`InverseTransformPoint` so it works with any pivot; width = counter width ×1.15 (clamped 420–parent width); height from `GetPreferredValues`; black outline (0.18) + size 17 + line spacing 1.08 for readability; free-respec button re-anchored under the text, left-aligned, `Free respec (over talent limit)`.
- Font: resolves `Valheim-AveriaSansLibre` via `Resources.FindObjectsOfTypeAll<TMP_FontAsset>` (exact, then prefix), rescanning ≤2/s, falling back to the counter's own font then `TMP_Settings.defaultFontAsset`. Same pattern as `Cjayride.SeneaLHudLayout\ChatFont.cs`.
- `Plugin.cs`: `Pace.Hint` rewritten to the plain wording above; new `StageBossShort(stage)` ("Bonemass" not "After Bonemass"); unused `StageBossLabel` removed; hint uses `\n` (TMP treats `\r` as an unknown glyph); added a startup warning when `PointsText` is missing; version 1.2.5.
- TMP API notes (Valheim's build has no `horizontalOverflow` / `verticalOverflow` / `enableOutlines` / `padding` on TMP): used `overflowMode`, `textWrappingMode`, `extraPadding`, `margin`, `outlineWidth/outlineColor`.

**Build:** `dotnet build -c Release` → 0 Error(s) / 0 CS warnings. DLL `bin\Release\net481\Cjayride.TalentPace.dll` 44,032 B v1.2.5.0. Zip `dist\cjayride-TalentPace-1.2.5.zip`, DLL SHA256 `EA746041…F06A` matching bin.
**Verified by string scan in the DLL:** `Current Progress:`, `To Unlock Points:`, `Points Earned by XP:`, `Points Spent:`, `Points Allowed:`, `Valheim-AveriaSansLibre`, `to Raise Limit to`, `Remove All Limits`, `Free respec (over talent limit)`.
**Not verified:** live in-game layout/font (needs the client).

**Packs:** both manifests → `cjayride-TalentPace-1.2.5`; cfg header line → v1.2.5 (schema unchanged, client/server identical); client CHANGELOG entry added under the user's new **4.4.43** heading; server CHANGELOG 4.4.43 heading created (user had not added one).

## 1.2.5 visibility pass (same day, follow-up request) — SeneaL-proof the text + free-respec button
**User request:** "make sure the button is visible, SeneaLUI has tricky z-index and since I can't test the visibility, you need to double check that it will be seen on this screen."

**Evidence gathered (no in-game test possible):**
- SeneaL UI's decompiled sources use hard `overrideSorting` canvases at fixed orders 28000–32600 (MerchantView 30000, HudEditor 30500, ModsView 31000, NoticeModal 32010, PopupView 32020, SettingsGuard 32000, VneiView 30900, BarberView 28990, AnnounceView 29000, Selection 28000, BuildBadge 32600). Sibling order inside the talent window is NOT a sufficient guarantee — a separate SeneaL canvas can cover the whole window.
- Workspace-proven safe pattern: nested Canvas + `overrideSorting` + `rootCanvas.sortingOrder + 1` (SeneaLUI.Hud/HudRoot.cs:660-668).
- `UI.TalentWindow` reflection: `Refresh` is **static** (`static void Refresh()`, all window state in static fields incl. `_view`, `WindowGroupPriority`). Its `ApplyZOrder` only bumps its own sub-canvases (search dropdown, StatsPanelView, TalentHoverTooltip, TalentApplyConfirmDialog).
- **Bug caught by that check:** binding Harmony `__instance` on a static method throws at patch time → the whole overlay would have silently never appeared. Reverted; `Invoke(null, null)` on the static Refresh is correct (it was correct pre-change).

**Code changes (CapHint.cs):**
- New `TalentPaceOverlay` GameObject child of the window panel (`TalentWindowView.PanelRect`, fallback: counter's parent), full-stretch RectTransform, with `Canvas.overrideSorting = true` and `sortingOrder = rootCanvas.sortingOrder + 1` (re-checked every refresh). Label + free-respec button now parent to this overlay → guaranteed above the window AND above any same-root SeneaL canvas at the window's order.
- Label + button are clamped inside the panel rect (`HostBounds`): x ≥ xMin+10, width ≤ panel width, button y clamped between panel top-10-34 and bottom+10; whole label+button block shifts up when space under the counter is tight (button room = 34+8 reserved only when visible).
- Font/label/button now apply font on creation; `ApplyFont` for the button text added at creation.
- `tools/GeometryCheck.ps1` (new): ported 1:1 the placement math; 7 layout cases (1800x800, 900x600, 500x400 clamp stress, counter-near-bottom, 3200x1000 ultrawide, 7-line text, counter flush left) → **FAILURES: 0** after the clamp/block-shift fix. (First run failed 2 degenerate cases → added the y-floor + block shift.)

**Build:** `dotnet build -c Release` → 0 Error(s). DLL 45,056 B v1.2.5.0. New strings/members verified in the DLL: `TalentPaceOverlay`, `TalentPaceCap`, `TalentPaceFreeRespec` (UTF-16 user strings) and `set_overrideSorting`, `get/set_sortingOrder`, `get_rootCanvas`, `Canvas` (UTF-8 metadata heap).
**Zip:** rebuilt `dist\cjayride-TalentPace-1.2.5.zip` (128,451 B) with the visibility CHANGELOG bullet; inner DLL SHA256 `FCC24D20…6373` == bin DLL.
**Docs:** Package CHANGELOG + mirrored root README/CHANGELOG + both pack CHANGELOGs updated with the visibility line.

**Remaining:** in-game visual check (label position under the counter, font, button look) — only the user can do that.

## Goal (resumed session)
Find where the previous session stopped on: boss-tier talent caps, EVA Surtr shard → Haldor rewards, cap-hint UI, free respec, auto-trim. Finish the remainder.

## Where the previous session stopped (verified)
- 1.2.3 DLL compiled + `dist\cjayride-TalentPace-1.2.3.zip` packaged (contained EvaSurtr code).
- Modpack 4.4.42: WackyDB clones, Drop That drops, Haldor sell entries, manifest dep, progress/CHANGELOG — all present.
- Cut off: 500/1000/5000 pricing (was first 0 / repeat 200), point grant value, auto-trim, pack `cjayride.talentpace.cfg` still 1.2.2 schema, 1.2.3 zip left `dist\Cjayride.TalentPace.dll` stale at 1.1.3.

## Decisions (user, this session)
- Prices: Svartalfar Queen (Mist) 500 / Hel Demon (Ash) 1000 / Jotunn (Deep) 5000 — on first AND repeat sales.
- Jotunn shard instant points: **15** (was 9 in code; 25 discussed, then user set 15).
- Deploy scope: code + build + configs. No profile install, no modpack zip.

## Changes this session (TalentPace → 1.2.4)
- `Plugin.cs`: **`Enable Haldor EVA surtr rewards` default is now `false`** (standalone users without the modpack's shard items; the CJAYCRAFT pack cfg keeps the explicit `= true`); per-shard coin config keys (`Svartalfar Queen shard coins` 500 / `Hel Demon shard coins` 1000 / `Jotunn shard coins` 5000; removed first/repeat keys); `Jotunn shard bonus points` default 15; `Auto trim over-cap nodes` (default true); split `Queen removes the cap` (false) vs `Fader removes the cap` (**true** — last boss removes all caps); persistent rank trim in `Pace.TrimCache` → `TrimRanksPersistently` (removes outer-node ranks via Talent Tree `Core.TalentState.Ranks`, bumps `CacheVersion`, `Core.TalentRuntimeService.CommitAndSave`, re-entrancy guard); clearer hint lines for last-boss uncap + Jotunn shard.
- `EvaSurtrRewards.cs`: `CoinsFor(kind)` per shard; reward messages include coins.
- `Cjayride.TalentPace.csproj` / `Plugin.cs`: version 1.2.4.
- `Package\manifest.json`, `Package\README.md`, `Package\CHANGELOG.md` (+ mirrored to root and dist).

## Build
- `dotnet build -c Release` — **Build succeeded, 0 Error(s)** (only NU1900 offline-vulnerability warnings).
- DLL: `D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace\bin\Release\net481\Cjayride.TalentPace.dll` (40,960 B, FileVersion 1.2.4.0).
- Zip: `D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace\dist\cjayride-TalentPace-1.2.4.zip` — DLL inside matches the built DLL byte-for-byte (SHA256 80C7B240…FA63C).

## Modpack config changes (client + server, mirrored byte-identical)
- `config\cjayride.talentpace.cfg` — rewritten to 1.2.4 schema; kept CJAYCRAFT values (caps 20/32/48/58/72/88/105/120, parry 0.35, effect scaling on: movement 1, damage 0.5).
- `config\shudnal.TradersExtended.haldor.sell.json` — shard prices 500 (Mist) / 1000 (Ash) / 5000 (Deep).
- `config\wackysDatabase\Items\Item_Cjayride_EvaSurtr_{Mist,Ash,Deep}.yml` — `m_value` = 500/1000/5000, tooltips state reward + coin price (+15 for Jotunn).
- `manifest.json` (both) — dep `cjayride-TalentPace-1.2.4`; `CHANGELOG.md` (both) 4.4.42 entry updated.

## Not done (per user scope)
- Install DLL into Gale profiles (profiles still run 1.1.3 DLL; pack manifests now expect 1.2.4).
- Pack 4.4.42 zips (only 4.4.40.zip exists in pack roots).
- In-game testing.

## Next steps
1. Install `dist\cjayride-TalentPace-1.2.4.zip` (or the bin DLL) into Gale client + dedicated-server profiles when ready to test.
2. In-game: kill Svartalfar Queen → sell Mist shard (expect +9 cap line + 500 coins) → sell again (500 coins only). Jotunn shard → +15 points, 5000 coins.
3. Verify auto-trim: raise a player over cap (lower a cap key), reopen talent window, outer nodes removed, log line “talent tree trimmed…”, button hidden.
4. Package 4.4.42 modpack zips when releasing.
