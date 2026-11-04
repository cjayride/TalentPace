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
