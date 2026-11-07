# TalentPace 1.1.3

## Goal
Reduce overall parry power with one config multiplier.

## Status
Complete for build + zip. Not in-game tested.

## Changes
- `Parry effect scale` (default 0.35) postfixes `SEMan.ModifyTimedBlockBonus` at Harmony last priority.
- Extra timed-block bonus above 1x is scaled. A 2x parry becomes 1.35x. Values at or below 1x (tower shields / no parry) are unchanged. Regular block armor is unchanged.

## Build
- `dotnet build -c Release` — 0 Error(s), 0 Warning(s).

## Output
- DLL: `D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace\bin\Release\net481\Cjayride.TalentPace.dll`
- Zip: `D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace\dist\cjayride-TalentPace-1.1.3.zip`

## Remaining
- Install the 1.1.3 DLL on server and clients and confirm a timed parry staggers less.
- Modpack plugin zips not replaced unless requested.
