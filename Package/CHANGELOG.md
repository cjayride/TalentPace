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
