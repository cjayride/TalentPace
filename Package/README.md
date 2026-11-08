# Talent Pace

Optionally scales **Talent Tree** movement and damage bonuses (off by default; when enabled, scale keys default to 0.5). Epic Loot, meads, food, seasons, and base weapon hits are not scaled.

**Parry effect scale** (default 0.35) shrinks extra parry power on timed blocks after other mods. A 2x parry becomes 1.35x; 3.5x becomes 1.875x. Regular blocking and the parry timing window are unchanged. Set to 1 to leave parry alone.

Boss-tier talent **point caps** are **on by default**. Turn them off in config if you do not want boss-gated active points.

Caps how many **Talent Tree** points actually apply, using the bosses **that player** has killed.

Points past the cap stay saved. Outer nodes stop working until that player kills the next boss, then they come back. If a config change leaves you **over** the cap, the tree shows a **Free respec** button (no coin cost).

| Personal boss key | Points that apply (default) |
| --- | --- |
| None | 20 |
| Eikthyr | 32 |
| The Elder | 48 |
| Bonemass | 58 |
| Moder | 72 |
| Yagluth | 88 |
| The Queen | 105 |
| The Fader | 120 |

The Talent Tree window lists boss progress, level budget, spent points, active limit, and the next boss that raises the cap.

Install on the **dedicated server and every client**. Same config on both. Console: `talentpace`.

| Section | Key | Default |
| --- | --- | --- |
| TalentTree effect scaling | Enable effect scaling | false |
| TalentTree effect scaling | Movement effect scale | 0.5 |
| TalentTree effect scaling | Damage effect scale | 0.5 |
| Parry | Parry effect scale | 0.35 |
| Caps | Enable talent point cap | true |

When the point cap is on, the Talent Tree window shows the cap and the next boss.

## Changelog

### 1.1.3

- Parry effect scale (default 0.35) reduces extra timed-block power. Regular block armor is unchanged.

### 1.1.0

- Talent Tree movement and damage can be scaled without changing other mods. Point cap is optional (off by default).
- Requires denikson-BepInExPack_Valheim-5.4.2360.

### 1.0.1

- The Talent Tree window shows the point cap and the boss that raises it.

### 1.0.0

- Boss-tier cap for Talent Tree points that apply.
