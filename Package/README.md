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

The Talent Tree window shows the cap text **left-aligned, starting below the unspent-point counter**, in the Talent Tree window's own font (the game's AveriaSansLibre) so it always matches the rest of the window:

```
Current Progress: Bonemass
To Unlock Points: Kill Moder
Points Earned by XP: 47
Points Spent: 53
Points Allowed: 58
Kill Moder to Raise Limit to 72 Points
```

If a player is over the limit, the text turns red and names the Free respec button below it.

Each line is its own text row placed by its own math, so the lines can never collapse into one mashed-together line (Valheim's `Valheim-AveriaSansLibre` is a fallback-style font asset that can report zero line height). The font is validated before use (glyphs + line height); if it ever fails that check, the text falls back to the Talent Tree window's own font and the log says so.

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
| Star kill XP | Enable | true |
| Star kill XP | Star threshold | 5 |
| Star kill XP | Percent per star | 25 |
| Star kill XP | Percent cap | 150 |
| Description text | Font names | Stats |
| Description text | Font style | Normal |
| Description text | Font size | 16 |
| Description text | List installed fonts | false |

## Changelog

### 1.3.7

- **Label fix:** `Next Unlock: Kill Moder` is now **`Next Unlock Kill: Moder`** — only the boss name is white.

### 1.3.6

- **Boss names render white** in the window text (Next Unlock / Current Progress lines), like the numbers.

### 1.3.5

- The window text now defaults to the **Stats window's own font** (AveriaSerifBold, 16 pt — a pixel match). `Font names` accepts `Stats`, `Valheim-AlveriaSansLibre`, or any Windows font.

### 1.3.4

- **FIX: Stats window behind the text after opening the map** — the canvas order stays in sync with the window and the on-top check runs at show time.

### 1.3.3

- **FIX: Stats window behind the text after zoom/pan** — on-top check on every refresh.

### 1.3.2

- Stats window background is **fully opaque** (no tree elements showing through).

### 1.3.0 - 1.3.1

- **Font settings**: names / style / size, live F1 updates, `List installed fonts` helper. `Valheim-*` names = the window title's own face. Text sits 10 px lower.

### 1.2.9

- **Star kill XP**: mobs more than 5 stars over their zone grant +25% kill XP per star over 5 (cap +150%); zone XP floor 5% → 10%.

### 1.2.8

- Caps +5 per tier (25 / 37 / 53 / 63 / 77 / 93 / 110 / 125); readable font; numbers white; Stats opens on top.

### 1.2.7

- **FIX: cryptic text** — the cap text renders with the Talent Tree window's own AveriaSansLibre font (the rendering that worked from the start), not the TMP fallback atlas.
- **FIX: text too high** — it now starts **below the unspent points number**.

### 1.2.6

- **FIX: mashed-together text** — line separation no longer depends on the font's line metrics.

### 1.2.5

- Cap text moved left, under the unspent-points counter; plain wording; **SeneaL-proof canvas** (own canvas at the window's sorting order + 1) with panel clamping.

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
