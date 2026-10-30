# TalentPace 1.1.0

## Goal
Scale only M2Valheim TalentTree movement and damage bonuses (default 50%). Keep boss talent caps optional and off by default.

## Status
Complete. Release build succeeded. DLL 1.1.0.0 at Package/Cjayride.TalentPace.dll.

## Files
- Plugin.cs — config, optional cap registration, talentpace output
- TalentTreeEffectScale.cs — Harmony on SE_Stats speed/stamina, SE_TalentPassives.ModifyAttack, Glass Cannon / Execution
- Package/manifest.json — 1.1.0, denikson-BepInExPack_Valheim-5.4.2360
- README/CHANGELOG/csproj version 1.1.0

## Build
`dotnet build -c Release` — 0 errors. FileVersion 1.1.0.0.

## Not done
In-game Gale A/B tests. Modpack manifest/config (out of scope).
