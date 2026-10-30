# Talent Pace

Optionally scales **Talent Tree** movement and damage bonuses (off by default; when enabled, scale keys default to 0.5). Epic Loot, meads, food, seasons, and base weapon hits are not scaled.

Boss-tier talent **point caps** are optional and **off by default**. Turn them on in config if you want the original 1.0.1 cap behavior.

Caps how many **Talent Tree** points actually apply, using the bosses **that player** has killed.

Points past the cap do not turn off the save. Outer nodes stop working until that player kills the next boss, then they come back. No respec.

| Personal boss key | Points that apply |
| --- | --- |
| None | 9 |
| Eikthyr | 15 |
| The Elder | 21 |
| Bonemass | 36 |
| Moder | 54 |
| Yagluth | 75 |
| The Queen or Fader | no cap |

Inner rings are kept first. A finished Berserker sector is about 33 points, so The Elder cap (21) leaves the outer damage nodes off.

Install on the **dedicated server and every client**. Same config on both. Console: `talentpace`.

| Section | Key | Default |
| --- | --- | --- |
| TalentTree effect scaling | Enable effect scaling | false |
| TalentTree effect scaling | Movement effect scale | 0.5 |
| TalentTree effect scaling | Damage effect scale | 0.5 |
| Caps | Enable talent point cap | false |

When the point cap is on, the Talent Tree window shows the cap and the next boss.

## Changelog

### 1.1.0

- Talent Tree movement and damage can be scaled without changing other mods. Point cap is optional (off by default).
- Requires denikson-BepInExPack_Valheim-5.4.2360.

### 1.0.1

- The Talent Tree window shows the point cap and the boss that raises it.

### 1.0.0

- Boss-tier cap for Talent Tree points that apply.
