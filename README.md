# Talent Pace

Optionally scales **Talent Tree** movement and damage bonuses (off by default; when enabled, scale keys default to 0.5). Epic Loot, meads, food, seasons, and base weapon hits are not scaled.

**Parry effect scale** (default 0.35) shrinks extra parry power on timed blocks after other mods. A 2x parry becomes 1.35x; 3.5x becomes 1.875x. Regular blocking and the parry timing window are unchanged. Set to 1 to leave parry alone.

Boss-tier talent **point caps** are **on by default**. Turn them off in config if you do not want boss-gated active points.

Caps how many **Talent Tree** points actually apply, using the bosses **that player** has killed.

Points past the cap are handled two ways:

- **Soft**: outer nodes stop applying until the next boss raises the cap, then they come back.
- **Auto trim** (`Auto trim over-cap nodes`, on by default): if the player has spent more points than the cap allows, the outer nodes are **removed from the tree** and saved that way, keeping the inner nodes. Points return when the cap rises.

If a config change leaves a player **over** the cap, the Talent Tree window shows a **Free respec** button (no coin cost, talent points are not reset). The button disappears once the tree is back under the cap.

| Personal boss key | Points that apply (default) |
| --- | --- |
| None | 20 |
| Eikthyr | 32 |
| The Elder | 48 |
| Bonemass | 58 |
| Moder | 72 |
| Yagluth | 88 |
| The Queen | 105 |
| The Fader | No cap (the last boss removes all limits by default) |

**EVA Surtr shards** (CJAYCRAFT only): this feature is **off by default** — the shard items exist only in the CJAYCRAFT modpack, which sets `Enable Haldor EVA surtr rewards = true` in its own config. Each Epic Valheim Additions boss drops one shard per player. Selling a shard to Haldor grants a once-per-character talent reward, and every shard always sells for coins:

| Shard (boss) | First sale per character | Coin price (every sale) |
| --- | --- | --- |
| `Cjayride_EvaSurtr_Mist` (Svartalfar Queen) | +9 talent limit | 500 |
| `Cjayride_EvaSurtr_Ash` (Hel Demon) | +9 talent limit | 1000 |
| `Cjayride_EvaSurtr_Deep` (Jotunn) | +15 talent points | 5000 |

Repeat sales pay the coin price only; the talent reward is remembered per character.

The Talent Tree window lists boss progress, points from your level, points spent, the active limit, and exactly how to unlock the next tier.

Install on the **dedicated server and every client**. Same config on both. Console: `talentpace`.

| Section | Key | Default |
| --- | --- | --- |
| TalentTree effect scaling | Enable effect scaling | false |
| TalentTree effect scaling | Movement effect scale | 0.5 |
| TalentTree effect scaling | Damage effect scale | 0.5 |
| Parry | Parry effect scale | 0.35 |
| Caps | Enable talent point cap | true |
| Caps | Auto trim over-cap nodes | true |
| Caps | Fader removes the cap | true |
| EVA Surtr shards | Enable Haldor EVA surtr rewards | false |
| EVA Surtr shards | Svartalfar Queen shard coins | 500 |
| EVA Surtr shards | Hel Demon shard coins | 1000 |
| EVA Surtr shards | Jotunn shard coins | 5000 |
| EVA Surtr shards | Jotunn shard bonus points | 15 |

## Changelog

### 1.2.4

- Per-boss shard coin prices (500 / 1000 / 5000) on every sale, not one flat repeat price.
- Jotunn shard now grants **15** talent points (was 9).
- **Auto trim**: over-cap players get outer nodes removed from the saved tree automatically.
- Last boss (the Fader) **removes all caps** by default; Queen has its own toggle.
- Clearer Talent Tree window text: current restrictions and how to unlock the next tier.

### 1.2.3

- EVA Surtr shards: first Haldor sale per character grants a talent limit or bonus points.

### 1.2.0

- Point cap on by default, multi-line cap details, free respec button when over cap.

### 1.1.3

- Parry effect scale (default 0.35) reduces extra timed-block power. Regular block armor is unchanged.

### 1.0.0

- Boss-tier cap for Talent Tree points that apply.
