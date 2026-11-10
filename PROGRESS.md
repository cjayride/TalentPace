# TalentPace 1.3.6

## 1.3.6 (2026-11-10, this turn) — boss names white in the description text
**User:** "Next Unlock: Kill Moder" and "Current Progress: Bonemass" — boss name white.
**Code:** new `Boss(string)` rich-text helper (`<color=#FFFFFF>…</color>`, same as `Num()` for numbers); wraps `NextBoss[stage]` on the Next Unlock line and `StageBossShort(stage)` on Current Progress. Labels keep the orange label color; boss names + numbers white. No new config keys ⇒ cfgs untouched.
**Build:** 0 W 0 E. `D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace\bin\Release\net481\Cjayride.TalentPace.dll` **v1.3.6.0, SHA256 83C6DF5BCB372B92…** — installed to BOTH 4.4.44 Gale profiles (backups `.bak-1.3.5`, profile SHAs match bin). Version bump 1.3.6 csproj/Plugin/manifest.

## 1.3.5 (2026-11-10, this turn) — default font = the Stats window's own font
**User:** "code works. What font does the Stats window use? Make our description text the same font style as the Stats window by default" + update cjaycraft beta, server, gale configs.
**Found (decompile):** Stats labels/values + title built by `StatsPanelView.MakeText` → `GUIManager.Instance.CreateText(..., GUIManager.Instance.AveriaSerifBold, ...)` (L5664) at `StatsPanelRowFontSize/TitleFontSize = 16` (L3927-3929). The boldness is baked into the AveriaSerifBold asset ⇒ legacy style Normal + that Font = the Stats look.
**Code:** new `StatsFontRequested()` (any entry starts with `Stats`, OrdinalIgnoreCase) → `StatsFontAsset()` reads the live `StatsPanelView` root's `Text.font` (`GetComponentsInChildren<Text>(true)` — panel built inactive; runs only on label-signature change), caches `_statsFont`. Chain: Stats token → Valheim- token (`title.font`) → Windows list (`ReadFont`) → cached title. Defaults: `Font names = "Stats"`, `Font size = 16` (was 21; 16 = the exact Stats row size so the match is visual too). Help text documents all three token kinds. 1.3.5 bump csproj/Plugin/manifest.
**Configs (script `tools\SetStatsFontDefault.ps1`, in-section replace, backups `.bak-statsfont`):** all 3 cfgs `Font names = Stats` + `# Default value: Stats` + section comment rewritten; `Font size = 16` (+ default comment); header v1.3.5. Client/server byte-identical.
**Build:** 0 W 0 E. `D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace\bin\Release\net481\Cjayride.TalentPace.dll` **v1.3.5.0, SHA256 E24930C4…** — installed to BOTH 4.4.44 Gale profiles (client + dedicated server; backups `.bak-1.3.4`, SHAs match).
**Expect:** description text in the same AveriaSerifBold 16 the Stats rows use; first-render log `description text font = …`; F1 live: `Arial` → system font, `Valheim-AlveriaSansLibre` → window title face, `Stats` → Stats face.

## 1.3.4 (2026-11-10, this turn) — FIX: stats behind text after moving the MAP (1.3.3 still failed)
**User repro (1.3.3, log-confirmed loaded):** open Talents → move map → click Stats → stats behind the points description text; close+reopen → normal.
**Root causes (two):**
1. **Stale overlay canvas order.** The overlay canvas order was captured ONCE at creation (`rootCanvas.sortingOrder + 1`). Opening the map re-focuses the talent window and the game RAISES its canvas sortingOrder ⇒ overlay keeps the old, now-lower number ⇒ the entire base window canvas draws above the overlay — stats (child of overlay) behind base-canvas content. Close+reopen rebuilds the window ⇒ order recomputed ⇒ "works again". (1.3.3's reparent-every-refresh was correct but powerless against a stale canvas order.)
2. **Show path missed.** The Stats button shows via `StatsPanelView.SetVisible(true)` + its own `Refresh`; `UI.TalentWindow.Refresh` does NOT run on that click ⇒ the window-refresh postfix can miss the show frame.
**Fixes (CapHint.cs):** `EnsureOverlayOrder()` re-derives `rootCanvas.sortingOrder + 1` every `EnsureStatsOnTop()` (cheap int compare); Harmony postfix `AfterStatsShow(bool visible)` on `StatsPanelView.SetVisible` → `EnsureStatsOnTop()` in the same frame the panel shows.
**Build:** 0 W 0 E. `D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace\bin\Release\net481\Cjayride.TalentPace.dll` **v1.3.4.0, SHA256 8671E393C333C04DA6329C4010E90629AB063C826AB7068B8D82751464996EDC**. Installed to 4.4.44 client Gale profile (Valheim closed; backup `.bak-1.3.3`) AND dedicated-server 4.4.44 profile (was 1.3.3.0, backup `.bak-1.3.3`; UI patches self-disable there — no talent window refresh on DS). Version bump 1.3.4 csproj/Plugin/manifest. No config keys added.

## 1.3.3 (2026-11-10, this turn) — FIX: Stats panel slips under text after zoom/pan
**User repro:** zoom/pan the tree → click Stats → stats text behind the description text; close+reopen window → Stats normal on top afterwards.
**Cause:** zoom/pan rebuilds the stats panel into the BASE window canvas (parent reset); the on-top re-assert ran on the 0.25 s text throttle, so between the Stats click and the next tick the panel sat under the overlay canvas. After re-assert it stays parented in the overlay, hence "fixed after reopen, forever".
**Fix:** `EnsureStatsOnTop()` moved to the TOP of `AfterRefresh`, runs on every window refresh (cheap: parent check + sibling check + alpha check, no allocations when already settled).
**Build:** 0 W 0 E. `D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace\bin\Release\net481\Cjayride.TalentPace.dll` **v1.3.3.0, SHA256 9A9296BA5F92BEA4B969BDBB36BA9805E26CCA24BA8909802B3F842A96AAC665**. Installed to 4.4.44 Gale profile (Valheim closed; backup `.bak-1.3.2`; SHA matches bin). Version bump 1.3.3 csproj/Plugin/manifest. No config changes needed.

## 1.3.2 (2026-11-10, this turn) — Arial 21 defaults + opaque Stats background
**User:** default font name Arial, default size 21; Stats panel background fully opaque (alpha 1) so tree elements behind don't distract; update configs in cjaycraft_beta pack, server pack, Gale profile.

**Code:** Plugin.cs binds `Font names` default `Arial`, `Font size` default 21. CapHint `EnsureStatsOnTop` now also grabs the StatsPanelView root's `Image` (the translucent `HoverTooltipBackground` Talent Tree builds with, decompile L5580) and forces `color.a = 1` (RGB kept, idempotent check `a < 0.999f`, re-asserted every 0.25 s refresh so panel rebuilds on window open are covered).
**Configs (all 3, script `tools\SetArialDefaults.ps1`, backups `.bak-arial`):** `Font names = Arial` (+ `# Default value: Arial`), `Font size = 21` (+ default comment via next-line `From 0 to 40` anchor), header v1.3.2. Client + server pack cfgs byte-identical (6,272 B, same SHA256). Profile cfg updated in place (user's F1 value overwritten per request).
**Build:** 0 W 0 E. `D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace\bin\Release\net481\Cjayride.TalentPace.dll` **v1.3.2.0, SHA256 FEE0FEAB9CCE8F1A028A57FBBECC28B70F8975C5601D4A5066AF536886CC8848**. Valheim closed ⇒ **installed to 4.4.44 Gale profile** (backup `.bak-1.3.1`, SHA matches bin). Version bump 1.3.2 csproj/Plugin/manifest. No zip/GitHub/pack manifest-CHANGELOG.

**Checks:** text renders Arial 21; Stats panel background solid; F1 font edits still live; log line `description text font = Arial (requested: Arial)`.

## 1.3.1 (2026-11-10, this turn) — Valheim window font token + LIVE font config
**User:** make the description default font `Valheim-AlveriaSansLibre` (user's spelling, matches the window title font) in code + pack configs. Then: "font face didn't change, F1 changes don't update — did 1.3.1 reach my Gale profile?" **Verified: profile ran 1.3.0.0** (1.3.1 built last turn, install never ran before the user's test).

**1.3.0 bugs confirmed:** (a) no `Valheim-*` token — name went to `CreateDynamicFontFromOSFont`, not installed ⇒ Unity silent fallback ≈ same face; (b) `AfterRefresh` signature excluded font settings ⇒ early-return on config-only change ⇒ label kept old font (F1 edits ignored).

**Fixes:** `WindowFontRequested()` = any list entry starts with `Valheim-` (OrdinalIgnoreCase, tolerates Alveria/Averia) ⇒ `TitleFontAsset()` = window title's `title.font` (the real stylized face via legacy Text — NOT the TMP atlas). Resolution chain: Valheim token → OS list (`ReadFont`, keyed names+size) → title font cached fallback. Signature now includes FontNames/FontStyle/FontSize + `ApplyWindowFont(label)` on every signature change ⇒ live F1 updates (0.25 s refresh). One-time log: `description text font = <font.name> (requested: …)`. Defaults aligned to user spelling `Valheim-AlveriaSansLibre` (Plugin.cs bind + both pack cfgs via Replace; profile cfg already user-edited to it). Plugin writes its cfg itself; profile cfg auto-updates on launch.

**Build:** 0 W 0 E. `D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace\bin\Release\net481\Cjayride.TalentPace.dll` **v1.3.1.0, SHA256 37B0447CE7E8F555F43DE5413EFE7B1C85527252C7B14DADEFDE0A27B8ADB813**. **Installed to 4.4.44 Gale profile** (Valheim closed; backup `.bak-1.3.0`; profile SHA matches bin). Version bump 1.3.1 csproj/Plugin/manifest. No zip/GitHub/pack manifest-CHANGELOG.

**Expect in-game:** description text now renders in the SAME stylized face as the Talent Tree window title (that's what the Valheim asset is); startup log line proves the font; F1 changes (names/style/size) apply live while the window is open; `Valheim-` prefix = window asset, Windows names = OS font list.

## 1.3.0 (2026-11-10, this turn) — font config + text 10px lower + Stats z-order REWRITE
**User:** "The description text looks great! (1) expose the font style and available fonts in the config file. (2) Lower the description points text an additional 10 pixels. (3) Stats info still displays BEHIND the points description text — must be on top."

**1 — Font config (section "Description text"):** `Font names` (string, default `Verdana, Segoe UI, Arial, Tahoma`, comma list, first installed wins, empty = built-in default), `Font style` (UnityEngine.FontStyle enum: Normal/Bold/Italic/BoldAndItalic; applied via `label.fontStyle` — verified property on UnityEngine.UI.Text), `Font size` (int, 0 = auto 58% of title clamped 15-20, range 0-40), `List installed fonts` (bool; one-time: logs `Font.GetPathsToOSFonts()` file basenames to the BepInEx log so the user can fill the list — e.g. "Segoe UI", "Comic Sans MS"). ReadFont now keyed on names+size (recreates on config change); ApplyWindowFont sets size/style every call (cheap, config live). GetPathsToOSFonts verified static String[] in UnityEngine.TextRenderingModule (ECall: reflection Invoke outside Unity throws, compiles fine, runtime-safe in-game).

**2 — 10px lower:** `Gap` 8 → **18** (block top = counter bottom − Gap).

**3 — Stats on top (REWRITE, the +2 canvas bump failed in-game):** TalentTree decompile: `StatsPanelView._root` = GameObject "StatsPanelView" parented to `_view.PanelRect`, plain Image+Text; `UI.TalentWindow::ApplyZOrder` orders it by **sibling index only** (`SetAsLastSibling`). Unity draws same-canvas siblings in ONE pass, so a nested canvas at root+1 that is an EARLIER sibling still loses — root+2 didn't beat it reliably. New fix (`EnsureStatsOnTop`, every refresh): re-parent the stats root **into our overlay canvas** (worldPositionStays=false; overlay stretches exactly over PanelRect so layout is pixel-identical) and `SetAsLastSibling()` it — the Talent Tree mod's own proven mechanism, sibling-order authority inside one canvas. No canvases added to the stats panel. Panel visibility (`SetVisible`) untouched.
Overlay canvas restored to root+1 (text must stay above window content; stats now rides above the text as its last child).

**Build:** 0 W 0 E. DLL `D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace\bin\Release\net481\Cjayride.TalentPace.dll` **v1.3.0.0, 51,200 B, SHA256 67FCCDC56A25CCBCB08401DE0207D34C521F2B6DAA10C88541558E1CC689D3DE**. Strings FOUND: `Description text`, `Font names`, `Font style`, `Font size`, `List installed fonts`, `available fonts` (UTF-16); `SetAsLastSibling`, `SetParent`, `GetPathsToOSFonts`, `CreateDynamicFontFromOSFont`, `fontStyle` (ASCII).
**Install:** Valheim closed ⇒ **1.3.0.0 installed** into 4.4.44 Gale profile (backup `Cjayride.TalentPace.dll.bak-1.2.9`), SHA matches bin. Version bump 1.3.0: csproj, Plugin.cs, Package\manifest.json. No zip / GitHub / pack manifest-CHANGELOG.

**In-game checks:** text 10px lower; config `Description text` section present; `List installed fonts = true` ⇒ log prints "available fonts (N): …"; Stats panel fully visible above the text; font style/size live-reload on window refresh.

## 1.2.9 (2026-11-10, this turn) — star-kill XP bonus + zone floor raise
**User:** hard star-mobs in low zones (8★ BF troll) pay ~nothing because the SkillsReworked over-level floor crushes kill XP; wants the floor balanced and more XP for higher stars, threshold "over 5 star levels", "a little more worthwhile". Rejected the first flat +200/star proposal as "too much"; approved **percentage** design: **+25% per star over 5** and **floor 5%→10%**.

**Ground truth (decompiled):**
- Kill XP pipeline = SkillsReworked: `CharacterOnDeathPatch.Prefix` → `KillXpCalculator.CalculateRawXp(maxHealth)` = `HP*0.1*XpMultiplier + 5` → `KillRewardFlow.AnnounceDeath(victim, killerPeerId, baseXp, biome, defeatKey, isBoss)` (namespace `SkillsReworked.Systems.Rewards`, owner-side, pre-RPC) → RPC → `AwardXp` → `LevelXpCalculator.ApplyLevelScaling` = `xp * factor`, factor lerps 1→`XP minimum factor` (pack 0.05) over `XP levels above optimal until minimum`=10 from biome optimal (BF=25).
- Star mobs = Smoothbrain CL&LC: stars = `Character.GetLevel() - 1` (CLLC uses that identity internally, e.g. `SetLevel((GetLevel()-1)/2+1)`, HUD `level_" + key.GetLevel()`); vanilla `Character.GetLevel()` exists (game decompile L960) so the patch is safe with CL&LC absent (stars 0 → no bonus).
- Bonus enters **baseXp** so the zone floor still applies to it (by design: keeps over-level gains modest, scales with mob HP).

**Implementation:** new `StarKillXp.cs`. Harmony **prefix on `KillRewardFlow.AnnounceDeath`** applied **by name** (`AccessTools.TypeByName`) — with SkillsReworked absent it stays off: immediate try in Awake, then a 1 s × 60 coroutine retry; logs one line on success/timeout. Skips `isBoss` (bosses already 10×), tamed, players (baseXp>0 gate in SR), `baseXp<=0`. `stars = victim.GetLevel()-1`; `over = stars - StarThreshold`; bonus% = min(over×PercentPerStar, PercentCap); `__ref baseXp += baseXp * pct*0.01`. Patch rides owner side so party-share gets the bonus too.
Config (section "Star kill XP"): `Enable star kill XP bonus` (true), `Bonus starts above (stars)` = 5, `XP bonus percent per star over` = 25, `XP bonus percent cap` = 150. (6★ +25%, 8★ +75%, 11★+ capped.)
Build fixes this turn: CS0246 `Config` type not in referenced BepInEx build → `Bind(BepInEx.Configuration.ConfigFile config)`; Plugin.cs calls `StarKillXp.Bind(Config)` + `StarKillXp.Apply(harmony, this)`.

**Floor:** `XP minimum factor = 0.05 → 0.1` in client pack cfg, server pack cfg, AND the 4.4.44 Gale profile cfg (each backed up `.bak-floor5`). Client+server pack cfgs byte-identical after edit? — verified single-line replace in each, no other keys touched.

**Build:** 0 Warnings 0 Errors. DLL `D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace\bin\Release\net481\Cjayride.TalentPace.dll` **v1.2.9.0, 48,128 B, SHA256 EBE4715AAFD9673318355BC13D01D3C622252CEED551F0C3B463D880EF4F7254**. Strings FOUND (UTF-16 scan, both alignments): `Star kill XP`, `KillRewardFlow`, `AnnounceDeath`, `Bonus starts above (stars)`, `XP bonus percent per star over`, `XP bonus percent cap`.
**Install:** watcher (job pwsh-38b3d37334ce4767) delivered **1.2.8.0 + new-caps cfg** at 03:39 when Valheim exited (profile cfg backup `.bak-127caps`). Valheim closed again ⇒ **1.2.9.0 installed directly** into 4.4.44 profile plugins (backup `Cjayride.TalentPace.dll.bak-1.2.8`), SHA matches bin.
**Version bump 1.2.9:** csproj, Plugin.cs, Package\manifest.json. **No zip, no GitHub, no pack manifest/CHANGELOG (standing instruction).**

**In-game checks:** log line "star kill XP bonus active, +25% per star over 5, capped at +150%"; 6★+ kills visibly worth more (8★ troll in over-level BF ≈ 3.5× old XP); 1-4★ unchanged; bosses unchanged.

## 1.2.8b (2026-11-10, this turn) — audit: retroactive skill points (NO code change needed)
**User:** "if we've increased the number of skill points per level per player, we need to make sure that player can re-apply the new points they are owed without them having to do a rebirth."

**Decompiled proof (ilspycmd → `_tmp_scan\decompile\SkillsReworked` + `TalentTree`):**
- **TalentTree budget** (`Core.TalentPointBudgetService.GetTotalPoints`): `BasePoints + GetLevel(player)*PointsPerLevel + rt.State.BonusPoints + defeatedBosses*PointsPerBoss`. Level comes live from `SkillsReworkedApi.GetLevel` (the Level skill). **Nothing is granted/stored at level-up** — available = formula total − spent, recomputed every call. Raising per-level points credits every player instantly; no rebirth, no migration.
- **SkillsReworked itself** (`SkillPointService.GetTotalPoints/GetAvailablePoints`): total = base + level×PointsPerLevel (+ prestige extra), and the stored value is only **`SkillsReworked.SpentSkillPoints`** (custom data). Available = total − spent. Also fully retroactive; prestige is only where that pool resets.
- Pack already runs `Points per level = 7` (SkillsReworked cfg, vs default 5) — everyone already sees the extra 2/level immediately.
- The limiter is Talent Pace's cap (BudgetPatch clamps total to cap); raised +5 this release (25→125), which is what unblocks the newly-owed points. The 1.2.8 window shows `Points Earned` from the live formula.

**Conclusion: no migration/grant code written** (would double-grant). 1.2.8 DLL ships as built.

## 1.2.8 (2026-11-10, this turn) — user's 4-part test build (DLL only, no zip/GitHub)
**User tasks:** (1) caps +5 per boss, also in both pack configs; (2) easier-to-read font for the window text; (3) new text format `Points Spent / Points Earned / Points Allowed / (blank) / Next Unlock: Kill ### / Current Progress: ###`, all numbers white, labels stay yellow; (4) Talent Tree **Stats** window opens on top of our text. Constraints: test DLL only — no zip, no GitHub, no pack manifest/CHANGELOG touches.

**1 — Caps +5:** defaults 20/32/48/58/72/88/105/120 → **25/37/53/63/77/93/110/125** (Plugin.cs BindCap). Pack configs client+server updated by `tools\ApplyCapDefaults5.ps1` (exact full-line map, 17 replacements per file: 8 `Key = value` + 8 `# Default value:` + header v1.2.8); verified client==server identical (5151 B), all 8 values +5. (Script's backup line had a `-Value` typo so no .bak-127 files; originals fully recoverable via the exact map.)

**2 — Font:** window's AveriaSansLibre is a stylized face. Now `Font.CreateDynamicFontFromOSFont("Verdana","Segoe UI","Arial","Tahoma")` (clean system sans; Unity picks first installed), size rule unchanged, window font = last-resort fallback. String scan: font names FOUND.

**3 — Text format:** `Pace.Hint` rewritten to the user's exact layout; `_hintGranted` added to the cache signature; numbers wrapped `<color=#FFFFFF>N</color>` (helper `Num()`), label `supportRichText = true` so numbers render white and labels keep the yellow/red color mark. Over-cap warning line kept. Cap=∞ → `Points Allowed: Unlimited`, `Next Unlock: Nothing, all limits removed`.

**4 — Stats on top:** IL of `UI.TalentWindow::ApplyZOrder` (static; dumps via `_tmp_scan\dump_il.ps1` — repointed to the 4.4.44 profile) shows the stats panel is ordered by **sibling index -1 inside the window's canvas**, so our nested overlay canvas (root order +1) covers it. Fix: `EnsureStatsOnTop()` gives `UI.StatsPanelView.Root` (static `_statsPanel` field; Root is Transform-family — unresolvable type via reflection, handled via `as Transform`/`as GameObject`) its own nested Canvas, `overrideSorting`, order = root+2 → always one above the overlay. Runs in the throttled Refresh postfix, cached in `_statsCanvas`.

**Build:** 0 Error(s) after CS0103 fix (renamed `root`→`go` in the hardened cast). DLL `D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace\bin\Release\net481\Cjayride.TalentPace.dll` **v1.2.8.0, 44,544 B, SHA256 DB0D3722C31289E9B9FBA4DFF8344040514D92E3EDE4A6CD22B1269E48468C99**. Strings FOUND: new line labels, Verdana/Segoe UI/Arial/Tahoma, CreateDynamicFontFromOSFont, _statsPanel, StatsPanelView, supportRichText.
**csproj/Plugin/Package manifest bumped to 1.2.8. No zip, no git, no pack manifest/CHANGELOG (user's instruction).**

**Install:** Gale profile renamed by Gale to `…modpack-4.4.44` (was 4.4.43). Valheim **running** (pid 32664) ⇒ DLL memory-mapped, copy failed (`user-mapped section open`). Backup `Cjayride.TalentPace.dll.bak-1.2.7` created. Background job `pwsh-38b3d37334ce4767` (tools\InstallWhenClosed.ps1, log `dist\install-1.2.8.log`) waits for Valheim to exit, then installs the 1.2.8 DLL (picks up current bin file) + replaces the profile cfg with the pack cfg (new +5 caps; profile cfg backed up as `.bak-127caps`). 4-hour timeout, never kills the game.

**In-game checks owed:** readable Verdana-ish text; 6-line format with WHITE numbers, yellow labels; caps 25→…→125; Stats button opens **above** the text (closes behind? no — stays on top, fine per user); free-respec flow unchanged.

**Not touched:** pack manifest.json, pack CHANGELOG.md, GitHub, dist zips, dedicated-server profile (DLL still 1.2.7 there — server has no UI; ask user before installing 1.2.8 server-side).

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
