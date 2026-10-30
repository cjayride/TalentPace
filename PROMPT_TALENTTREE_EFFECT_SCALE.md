# Implementation prompt: TalentPace — scale TalentTree move & damage by 50%

Use this document as the full task spec when editing **Cjayride.TalentPace** (`D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace`).

## Goal

Add an **optional** feature that reduces **only M2Valheim TalentTree** movement and damage bonuses to **50%** of their designed values. Do **not** change Epic Loot, meads, food, seasons, vanilla weapon damage, or any non–TalentTree source.

Boss **talent point caps** (existing TalentPace behavior) must remain **optional** and **off by default** for this CJAYCRAFT use case — or controlled by a config flag so the modpack can run **effect scaling only** with no point cap.

## Acceptance criteria

1. With scaling enabled at `0.5`, a maxed **Battle March** aura behaves like ~half of TT’s normal move speed bonus (and run-stamina side if implemented via same SE data).
2. TalentTree **damage** bonuses (passive % damage, proc buffs, etc. that go through TT status effects or documented TT damage hooks) are scaled by the same factor.
3. **Ratatosk**, Epic Loot move/damage enchants, Guardian Powers, PassivePowers, and base hit damage are **unchanged** when A/B testing with scaling on vs off.
4. Config is **server authoritative** (synced); client and dedicated server use the same values.
5. Build outputs `Cjayride.TalentPace.dll` to `Package/`; bump version and update `Package/CHANGELOG.md` and Thunderstore zip if releasing.
6. Modpack: add dependency + config under `beta_cjaycraft` and `beta_cjaycraft-server` when asked (not required for code-only task).

## Non-goals

- Do not patch TalentTree DLL or ship a fork of M2Valheim-TalentTree.
- Do not scale total outgoing `HitData` damage (that would nerf base weapon hits).
- Do not change TalentTree tooltips unless you find a safe, low-risk way (tooltips showing full TT text while effect is halved is **acceptable** for v1).

## Config (BepInEx)

Add section **`[TalentTree effect scaling]`** (or similar) to `cjayride.talentpace.cfg`:

| Key | Type | Default (CJAYCRAFT) | Description |
|-----|------|---------------------|-------------|
| `Enable effect scaling` | bool | `true` | Master switch for TT-only scaling. |
| `Movement effect scale` | float 0–1 | `0.5` | Multiplier on positive TT movement modifiers. `1` = no change. |
| `Damage effect scale` | float 0–1 | `0.5` | Multiplier on TT damage-related modifiers. `1` = no change. |
| `Enable talent point cap` | bool | `false` | When `false`, existing boss-tier point cap logic is **disabled** (no trim, no budget postfix change). When `true`, keep current 1.0.1 cap behavior. |

Keep existing **`[Caps]`** entries unchanged for packs that re-enable the cap.

## Implementation strategy

Follow existing patterns in `Plugin.cs`, `Pace`, and `CapHint.cs`: Harmony + `AccessTools.TypeByName` for TalentTree `Core.*` / `UI.*` types. `[BepInDependency("M2Valheim.TalentTree")]` stays required.

### Phase 1 — Movement (status effects)

1. Discover how TT registers status effects (in-game dev: `wackydb_se` not needed; use ILSpy/dnSpy on `TalentTree.dll` from Gale profile or Thunderstore package).
2. Identify TT effects that grant movement speed (minimum: **Battle March**; also consider Hit and Run, Weapon Swap, Shadow Dash, Seafarer wet speed, Silent Stride if they use `SEData.m_speedModifier` / swim modifier).
3. Patch **`StatusEffect.Setup`** or **`StatusEffect.Start`** (postfix): if effect is from TalentTree, scale:
   - `m_speedModifier`: only **positive** values × `Movement effect scale`
   - `m_swimSpeedModifier` if used: same rule
   - Do **not** scale negative modifiers (slows/debuffs).

**Identifying “from TalentTree” (pick one robust approach):**

- **A (preferred):** Maintain a hash/name set built at startup by reflecting TT’s status effect registration (e.g. effects created under TT assembly or listed in TT config/registry types).
- **B:** Prefix match on `StatusEffect.m_name` / internal name against known TT effect names (document list from TT changelog/README; update when TT adds talents).
- **C:** Check `StatusEffect` type namespace / custom TT subclass if TT uses derived SE types.

Use the same discovery approach as `TalentPointBudgetService` — fail gracefully with a single log line if reflection fails (scaling off, caps unaffected).

### Phase 2 — Damage (TalentTree only)

1. Map TT damage sources:
   - Status effects with `m_damageModifier`, `m_percentDamageModifiers`, or similar on `SEData`.
   - Direct damage modifications in TT Harmony patches (search TalentTree for `Damage`, `HitData`, `ModifyDamage`, talent IDs).
2. Apply **`Damage effect scale`** with the same TT-only filter as movement:
   - Scale **bonus** fields (positive damage modifiers, percent bonuses above neutral).
   - Do not scale generic player attack pipeline for non-TT hits.

If some TT damage never touches `StatusEffect`, add **targeted** postfixes on TT methods found in step 1 (e.g. passive damage multiplier application), still gated by `Enable effect scaling`.

### Phase 3 — Integrate with point cap

- Wrap existing `BudgetPatch`, `RebuildPatch`, `BossKeyPatch`, and `CapHint` registration in `if (EnableTalentPointCap)`.
- When cap is disabled, plugin still loads scaling patches and optional `talentpace` console command (extend command to print scale factors + cap state).

## Verification

1. **Build:** `dotnet build -c Release` on `Cjayride.TalentPace.csproj`; confirm DLL path under `Package/`.
2. **Manual (Gale profile with TT):**
   - Scaling on, cap off: compare sprint speed with full Battle March vs same build without TT move talents (or rank 1 vs max rank).
   - Drink Tonic of Ratatosk / equip EL move speed: confirm **unchanged** when toggling scaling.
   - Warrior/Berserker damage talent: rough DPS check on training dummy or `LogArmorMitigation` on TT if enabled — should drop ~50% from **talent portion**, not halve total hit.
3. **Multiplayer:** dedicated server + one client; config sync verified.

## Files to touch (expected)

- `Plugin.cs` — config, conditional cap registration, scaling patch registration
- New file(s) e.g. `TalentTreeEffectScale.cs` — Harmony patches + TT effect detection
- `CapHint.cs` — only if cap hint must hide when cap disabled
- `Package/README.md`, `Package/CHANGELOG.md`, `Package/manifest.json` — version bump
- Optional: `progress.md` in `beta_cjaycraft` when modpack integrates

## Reference paths

- Project: `D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace`
- Modpack config (orphaned, re-add to manifest when shipping): `D:\AI-Agent-Projects\cjaycraft\beta_cjaycraft\config\cjayride.talentpace.cfg`
- TalentTree plugin (inspect only): `...\Gale\...\plugins\M2Valheim-TalentTree\TalentTree.dll`
- Existing cap logic: `Plugin.cs` (`BudgetPatch`, `Pace.TrimCache`, boss order keys)

## Suggested version

**1.1.0** — TalentTree effect scaling + optional cap toggle.
