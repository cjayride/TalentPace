# TalentPace 1.1.2

## Goal
1. Double-check TalentPace against the latest installed Valheim.
2. Improve code speed, load times, lag, and FPS.

## Status
Complete for build + static verification + dedicated-server load evidence from 1.1.1 on the same game build. Client in-game A/B of the talent window was not re-run this pass.

## Compatibility (re-checked 2026-11-04)
- Installed `valheim.exe`: FileVersion `6000.0.75.2503836`, ProductVersion `6000.0.75f1`.
- Dedicated-server BepInEx log (`_tmp_scan/server_test`): Valheim **1.0.21**, Unity `6000.0.75.2503836`, BepInExPack **5.4.2360**, TalentTree **2.3.5**, Talent Pace **1.1.1** loaded, **0 failed plugins**.
- Thunderstore latest for declared deps remains `denikson-BepInExPack_Valheim-5.4.2360` and `M2Valheim-TalentTree-2.3.5`.
- Cecil verifier vs installed `assembly_valheim.dll` + Gale test `TalentTree.dll`: `TOTAL 59  CHECKED 57  MISSING 0`. All 12 previously patched methods still have healthy IL bodies.
- Latest public patch is Valheim 1.0.21 (2026-10-28); the local install matches that era (Unity 6 0.75 line).

## Performance this pass
- Removed the Harmony postfix on `UI.TalentWindow.Tick`. That was the remaining FPS cost: a trampoline every frame the talent window was open. Cap hint now runs only on `Refresh`.
- Replaced leftover `AccessTools.TypeByName` in `BudgetPatch`, `RebuildPatch`, and `CapHint.Apply` with `TT.Type`.
- Cached boss-stage (`HaveUniqueKey` scans) per player until a `defeated_*` key is added.
- Reused invoke argument arrays in `MarkDirty`, `TryGet`, `CacheFor`, and cap-hint sampling.

## Files changed
- `Plugin.cs`, `CapHint.cs`, `TT.cs`, `Cjayride.TalentPace.csproj`, changelogs, `Package/manifest.json`.

## Build
- `dotnet build -c Release` — 0 Error(s), 0 Warning(s). DLL FileVersion `1.1.2.0`.

## Output
- DLL: `D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace\bin\Release\net481\Cjayride.TalentPace.dll`
- Zip: `D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace\dist\cjayride-TalentPace-1.1.2.zip`

## Remaining
- In-game client confirmation of the cap line after dropping the Tick patch (opens/refreshes the talent window).
- Modpack manifests not updated (out of scope unless requested).
