# 1.4.3

- **SkillsReworked:** if a config cut leaves a player with **more spent Active Skill points than the new budget**, Talent Pace **refunds those Active Skills** (same idea as talent over-cap). **No** Rebirth cost, **no** talent-tree wipe, **no** Level/Prestige change. Optional skills-window button if auto refund is off.

# 1.4.2

- **FIX:** Talent window stats match the unspent counter on Talent Tree 3.0 — **Points Spent** / **Points Left** use the same budget as the game (includes unconfirmed draft picks). Shows **Reserved (unconfirmed picks)** when draft points are holding budget.

# 1.4.1

- **FIX:** SkillsReworked integration no longer spams HarmonyX warnings at load (`ItemCostService`, `ItemCosts.DisplayName`). Rebirth cost hints use silent reflection; pack SkillsReworked item names resolve via `M2Core.Content.ItemCosts.DisplayName`.

# 1.4.0

- **Talent Tree 3.0** dependency (`M2Valheim-TalentTree-3.0.0`). Cap line, Talent Effects, and Shardstone Effects buttons work with the 3.x window (Stats/Builds panels stay clickable).
- **MLZ heroic shards** section in default BepInEx cfg when generated fresh (pack still opts in via config).

# 1.3.22

- **FIX:** Cap description and Talent/Shardstone toggles were never drawn: 1.3.21 hid the overlay whenever the Builds/Stats *objects* existed (even while closed). Overlay stays on unless Stats/Builds is actually open. Logs restored so a missed Talent Tree hook is visible.

# 1.3.21

- **FIX:** Talent Tree cap text + Talent/Shardstone buttons show again (full-window overlay, not a clipped top-left band). Overlay no longer adds a Canvas on the Stats panel (that stopped Stats from opening). While **Stats** or **Builds** is open, the cap overlay hides so those panels are not covered.

# 1.3.20

- **MLZ heroic talent shards** (pack opt-in): `Cjayride_MLZShard_*` from MonsterLabZ heroics — sell to Haldor for +cap once per character (config per boss) and coins every sale. F1 `[MLZ heroic shards]`. Works like EVA Surtr shards.

# 1.3.19

- **FIX:** Talent Tree 2.3.x window UI — the cap hint strip is a top-left band (does not cover the header Stats button or unspent counter). Stats panel stays on Talent Tree’s layout parent; when open it sorts above the hint. Hooks `ApplyZOrder` so pan/zoom does not slip panels under the tree. Fallback anchor if `PointsText` moves.

# 1.3.18

- **FIX:** Valheim 1.0.30 — gating Talent Effects no longer logs a Harmony warning for `Patches.PlayerGetTotalFoodValuePatches.Prefix` (that hub is postfix-only). Food HP/stamina/Ironclad talent bonuses still respect the Talent Effects toggle.

# 1.3.16

- **UI:** Next unlock boss names — `The Elder` / `The Queen` (capitalized article), **Fader** without "the"; removed the `(removes all limits)` suffix on the last unlock line (uncap behavior unchanged).

# 1.3.15

- **FIX:** Temporary — skip Talent Tree item tooltip Prefixes so forge/inventory show real numbers: **block** (was 100-Blocking as orange), **weapon damage** (was 70% nerf cap as orange). Vanilla: orange = listed/base, yellow = your skill. Combat damage nerf unchanged. F1 `Tooltips` → **Restore vanilla item tooltips** (on by default). Off when Talent Tree fixes it.

# 1.3.14

- **SkillsReworked:** small **Rebirth cost** line under the Rebirth button on the skills window (reads server config: Level loss and/or items). Same cost is appended to the Yes/No confirm dialog. Hidden when the button says **Reset** (undoes pending spends, no Rebirth cost). Section `[Skills Reworked UI]` in config.

# 1.3.13

- **FIX:** Talent Effects / Shardstone Effects off no longer zeroes baseline weapon damage. The Talent Effects gate on Talent Tree's `Character.ApplyDamage` hub Prefix returned `false` when skipped, so Harmony skipped `Character.ApplyDamage` itself. Talent procs and shardstone skills stay gated; normal melee/weapon hits apply again.

# 1.3.12

- **FIX:** Talent Effects off now also gates **Bulwark Immolation** (and the rest of the Talent Tree `Player.Update` hub: Warden, Shield Charge, Battle Hardened, explosive-shot toggle, passives heartbeat) plus block-attack talent procs. Immolation was still lighting nearby creatures because it ticks on player update, not on `Character.Damage`.

# 1.3.11

- **FIX:** Talent / shardstone buttons now toggle immediately (text, color, config), show a center confirmation (`Talent effects disabled` / `enabled`), and save the F1 keys. Clicks were registering but the overlay only redrew on a Talent Tree refresh, so the buttons looked dead.

# 1.3.10

- Talent / shardstone / free-respec buttons are **half as wide**, with smaller label text.
- **FIX:** the overlay canvas now has a GraphicRaycaster, and the Stats panel no longer sits on top of the buttons while it is hidden (that was eating clicks).

# 1.3.9

- **Talent / shardstone effect buttons** under the Current Progress line on the Talent Tree window. `Talent Effects [enabled/disabled]` turns all Talent Tree combat and aura effects off (taming-safe; on by default). If EpicLoot is installed, `Shardstone Effects [enabled/disabled]` turns socketed shardstone bonuses off and leaves item enchants alone (on by default). Same keys in F1: `Enable talent effects` / `Enable shardstone effects`.

# 1.3.8

- **Parry put back a little**: the `Parry effect scale` default is now **0.55** (was 0.35). The 1.1.3 nerf landed harder than intended; 0.55 sits between the nerf and unscaled parry — a 2x parry becomes 1.55x (was 1.35x, vanilla 2x) and 3.5x becomes 2.375x. Regular block armor and the parry timing window are unchanged, and the key still goes to 1 for full vanilla parry.

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
