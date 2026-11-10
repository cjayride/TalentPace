# 1.3.6

- **Boss names render white** in the Talent Tree window text — on the `Next Unlock: Kill ###` and `Current Progress: ###` lines, like the numbers.

# 1.3.5

- The window text now defaults to the **same font as the Talent Tree Stats window** (the game's AveriaSerifBold at the Stats rows' 16 pt size).
- `Font names` accepts `Stats` (the Stats window's font, the default), `Valheim-AlveriaSansLibre` (the window title's stylized face), or any Windows font name (Arial, Verdana, ...).

# 1.3.4

- **FIX: Stats window opened behind the progress text after opening the map.** The text's canvas keeps its sorting order in sync with the window's canvas (the game raises it on map focus), and the on-top check runs the moment the Stats window is shown.

# 1.3.3

- **FIX: Stats window slipped behind the text after zooming/panning the tree** — the on-top check runs on every window refresh, not on the 0.25 s text timer.

# 1.3.2

- **Stats window background is fully opaque**, so tree elements no longer show through the rows.

# 1.3.1

- `Valheim-…` font names work again: any name starting with `Valheim-` uses the Talent Tree window's own title font (plain rendering — not the TMP atlas behind the cryptic text in 1.2.7).
- **Live font config**: Font names / style / size changes from the F1 menu apply while the window is open. First render logs which font was used.

# 1.3.0

- **Font settings** for the window text: `Font names`, `Font style` (Normal/Bold/Italic/BoldAndItalic), `Font size` (0 = auto), plus a one-time `List installed fonts` helper that logs every font on your machine to the BepInEx log.
- The text block sits 10 px lower under the points counter.

# 1.2.9

- **Star kill XP**: mobs spawned **more than 5 stars** above their zone now grant **+25% kill XP per star over 5** (up to +150%) — hard star mobs in easy zones are finally worth killing. Works for your party too.
- **Zone XP floor** raised from 5% to 10%: high-star mobs in low zones always grant at least 10% of the zone's XP.

# 1.2.8

- **Talent caps raised by 5 at every boss tier**: 25 / 37 / 53 / 63 / 77 / 93 / 110 / 125.
- **Easier-to-read font** for the Talent Tree text: a clean system sans (Verdana, then Segoe UI / Arial / Tahoma) instead of the stylized AveriaSansLibre.
- **New text format**, with all numbers in **white** and labels in the usual yellow:
  `Points Spent: #` / `Points Earned: #` / `Points Allowed: #` / blank / `Next Unlock: Kill ###` / `Current Progress: ###`.
- **Stats window opens on top** of the progress text (it used to open behind it).

# 1.2.7

- **Fixed the cryptic text**: the window text goes back to the game's own text rendering (the plain Unity text + the Talent Tree window's AveriaSansLibre font) that displayed correctly from the very first version. The `Valheim-AveriaSansLibre` TMP asset is a fallback atlas (CJK/emoji) and rendered plain letters as garbage — it is no longer used for this text at all.
- The text now **starts below the unspent points number** (its top is the bottom edge of the counter, not the top), left-aligned as before.
- Free respec button uses the same window font.

# 1.2.6

- **Fixed the unreadable text**: every line of the progress text is now its **own text row placed by its own math** — the lines can no longer stack on one baseline, no matter what the font reports. (The single text block relied on the font's line spacing; Valheim's `Valheim-AveriaSansLibre` is a fallback-style asset that can report zero line height, which mashed every line into one pile.)
- The font is now **validated before use**: it must contain the space and letter glyphs and report a real line height. If `Valheim-AveriaSansLibre` ever fails that check, the text falls back to the window's own font (with a line in the log saying so) instead of rendering unreadably.

# 1.2.5

- The talent window text moved **left, under the unspent-points counter** (it was centered under the title).
- The text now uses the game's own **Valheim-AveriaSansLibre** TMP font, with a black outline, larger size, and left-aligned lines — much easier to read.
- Rewritten for plain reading:
  `Current Progress: Bonemass` / `To Unlock Points: Kill Moder` / `Points Earned by XP: 47` / `Points Spent: 53` / `Points Allowed: 58` / `Kill Moder to Raise Limit to 72 Points`.
- The Free respec button sits under the text, left-aligned, label **Free respec (over talent limit)**.
- **Visibility hardened for SeneaL UI**: the text and the button live on their own canvas with `overrideSorting` at the window's sorting order **+1**, so SeneaL UI's own canvases (which use fixed orders in the 28000–32600 range) cannot bury them. The label and button are also clamped inside the window panel, so they can never be pushed off-screen by a resized window.

# 1.2.4

- **EVA shard rewards are off by default** (`Enable Haldor EVA surtr rewards` = false). The shard items only exist in the CJAYCRAFT modpack, which enables the feature in its own config; standalone installs no longer load the Haldor sell patch.
- **EVA Surtr shard prices are per boss, on every sale**: Svartalfar Queen (Mist) **500**, Hel Demon (Ash) **1000**, Jotunn (Deep) **5000** coins. One config key each; the flat first/repeat prices are gone.
- **Jotunn shard grants 15 talent points** on first sale per character (was 9). Mist / Ash shards still raise the talent limit by +9 each.
- **Auto trim over-cap nodes** (on by default): when a player has spent more talent points than the current cap allows, the outer nodes are removed from the tree and saved that way. Inner nodes are kept, points return when the cap rises, and the Free respec button still works.
- **The last boss removes all caps**: `Fader removes the cap` now defaults to **true**. `Queen removes the cap` is its own toggle (default false).
- Clearer Talent Tree window: current limit, points from level, points spent, and exactly how to unlock the next tier (kill the named boss, or sell the Jotunn shard for instant points).

# 1.2.3

- EVA Surtr shards at Haldor: first sale per boss type grants +9 talent limit (Mist/Ash) or +9 talent points (Deep). Repeat sales: 200 coins.

# 1.2.2

- Tighter caps mid/late (58 / 72 / 88 / 105 / 120) so over-leveled players cannot use their full talent budget until more bosses.

# 1.2.1

- Cap tiers for **the Queen** (165) and **the Fader** (200). No uncap unless `Queen or Fader removes the cap` is true.

# 1.2.0

- Point cap **on by default** with higher boss-tier limits (20 / 32 / 48 / 68 / 95 / 130).
- Talent window shows **multi-line cap details**: boss tier, level points, spent points, active limit, and next boss.
- **Free respec** button when spent points exceed the current cap (e.g. after a config change); no coin cost.

# 1.1.3

- Added a single **Parry effect scale** (default 0.35). It shrinks extra timed-block / parry power after other mods, so a 2x parry becomes 1.35x. Regular block armor and the parry window are unchanged.

# 1.1.2

- Re-checked against Valheim 1.0.21 (Unity 6000.0.75f1) with TalentTree 2.3.5 and BepInExPack 5.4.2360. The dedicated-server load of 1.1.1 already succeeded with zero failed plugins; this build keeps the same patch targets.
- Performance: the cap hint no longer Harmony-patches TalentWindow.Tick, so the talent window does not pay a postfix trampoline every frame. The line still updates on Refresh.
- Performance: remaining TypeByName scans at patch time now use the cached Talent Tree assembly. Boss-stage lookups are cached per player. Reflection invokes reuse argument arrays instead of allocating per call.

# 1.1.1

- Verified against the Unity 6 build of Valheim (6000.0.75f1). All patched game and Talent Tree (2.3.5) targets confirmed present in the installed assemblies.
- Fixed: the point cap could throw an internal error when trimming the Talent Tree cache, so the cap silently stopped applying. Tier values are now converted correctly.
- Fixed: the point cap no longer lifts itself for players who have only defeated Yagluth; it lifts on the Queen/Fader stage as intended.
- Performance: the Talent Tree window hint no longer rebuilds its label, layout, text, and colour every frame. It refreshes at most four times a second, and writes to the UI only when the text, colour, or layout actually changes.
- Performance: type lookups at startup now share a single cached assembly scan instead of scanning every loaded assembly twelve times.
- Performance: the damage-scaling path now reads the Talent Tree cache through bound delegates and reuses scratch collections, instead of reflection calls and fresh allocations per hit.
- Reduced allocations on the cap hint and cache-trim paths to cut per-open garbage collection pressure.

# 1.1.0

- Optional Talent Tree-only movement and damage scaling (off by default; scale keys default to 0.5). Point cap toggle lives under Caps and is off by default.
- BepInEx pack requirement is denikson-BepInExPack_Valheim-5.4.2360.

# 1.0.1

- The Talent Tree window shows the point cap and the boss that raises it. At the cap the line reads "Talent cap reached" and names the next boss.

# 1.0.0

- Boss-tier cap for Talent Tree points that apply. Outer nodes past the cap stay saved and turn back on after the next boss.
