# Puzzle Kingdoms — Game Design Document (v0.1)

Status: **MVP design locked for prototyping.** Every number in this document is a
starting value to be validated by playtests and headless balance simulations.

## 1. Pitch

> Every puzzle move builds your army. Every army decision wins the battle.
> And when it matters most, *you* sound the war horn.

Puzzle Kingdoms is a portrait mobile game in which the player solves a short
Match-3 puzzle to recruit an army, arranges and merges it on a 3×3 formation,
and watches it fight an auto battle — while holding a small number of
**Rally** charges that they earned on the puzzle board and fire at the moment
they choose.

## 2. Pillars

1. **The puzzle is the recruitment office.** The enemy army is visible while the
   player solves the puzzle, so every match is a tactical choice, not a reflex.
2. **Readable cause and effect.** Combat is fully deterministic. A loss can always
   be explained by the player's own decisions, and a retry with a better plan
   wins.
3. **Auto battle, not idle battle.** The player commands once or twice per battle
   (Rally). Small, timed, high-impact input.
4. **Short sessions.** 2–4 minutes per stage. Battles last 20–40 seconds and can
   be sped up.

## 3. Differentiation (vs. Empires & Puzzles, Puzzle Quest, Auto Chess-likes)

| Competitors do | Puzzle Kingdoms does |
|---|---|
| Match-3 *is* the combat (gems = attacks) | Match-3 *builds* the army; combat is a separate tactical layer |
| Gacha heroes decide outcomes | Player decisions decide outcomes (enemy-aware recruiting, formation, merging) |
| Auto battlers are fully passive | Rally charges, earned in the puzzle, give timed command moments |
| RNG crits and misses | Deterministic combat: daily seeded challenges, replays, honest losses |

## 4. Core Loop

Stage Select → **Puzzle** (12 moves, enemy visible) → **War Camp** (formation,
merge, bench) → **Battle** (auto + Rally) → Results (stars, gold) → upgrades →
next stage.

## 5. Puzzle

- Board 6×6. Gem types in MVP (4):

| Gem | Color | Effect |
|---|---|---|
| Sword | Warrior Red `#E85B55` | Warrior recruitment meter |
| Bow | Archer Green `#58C889` | Archer recruitment meter |
| Staff | Mage Purple `#A77BE8` | Mage recruitment meter |
| Banner | Kingdom Gold `#F4BE4F` | Rally meter |

- Swap two orthogonally adjacent gems. A swap that creates no match is rejected
  and costs no move.
- **Moves:** 12 per stage (stage data can override).
- **Recruitment meter:** every cleared gem adds 1 point to its meter; every
  **3 points = 1 unit** (remainder carries over within the stage). So a match-3
  gives 1 unit, match-4 gives 1⅓, match-5 gives 1⅔. This rewards big matches
  smoothly and makes cascades predictable in value.
- **Cascades** count fully and are shown as "Bonus!". With 4 gem types the
  cascade rate stays moderate (to be measured).
- **Rally meter:** 3 Banner points = 1 Rally charge, max 3 charges. Banner points
  beyond the cap convert to gold (1 gold per point, start value).
- If no valid move exists, the board reshuffles for free.
- The initial board never contains a ready-made match and always has at least
  one valid move.
- The top of the puzzle screen shows the enemy formation and their traits.

Post-MVP: special gems from match-4/5, stage objectives, boss board modifiers.

## 6. War Camp (army preparation)

- 3×3 formation grid. **Rows** = front / middle / back. **Columns** = lanes
  (left / center / right). The front row faces the enemy.
- Unlimited **bench**; at most 9 units deployed.
- **Merge (manual):** three units of the same class and tier → one unit of the
  next tier. Triggered by dragging a unit onto an identical one when a third copy
  exists, or by the highlighted Merge button. Max tier 3.
- Units still on the bench when the battle starts are dismissed for 5 gold each.
- Army is stage-local; nothing carries over in the campaign.

### Tier scaling (start values)

| Tier | HP & damage multiplier | Visual |
|---|---|---|
| 1 | ×1.0 | base |
| 2 | ×1.8 | larger, trim color |
| 3 | ×3.2 | larger, glow |

Intent: one merged unit is slightly stronger than three separate ones in raw
numbers but occupies one slot. With ≤ 9 units, spreading is usually better; with
more, merging is. Verified by headless simulation.

## 7. Units (Tier 1 start values)

| Class | HP | Damage | Type | Interval (s) | Range (cells) | Move speed (cells/s) | Notes |
|---|---|---|---|---|---|---|---|
| Warrior | 120 | 12 | Physical | 1.0 | 1.0 (melee) | 1.2 | Frontline |
| Archer | 60 | 10 | Physical | 0.8 | 4.0 | 1.0 | Back line DPS |
| Mage | 55 | 22 | Magic | 1.6 | 3.5 | 0.9 | Ignores armor; AoE post-MVP |

Damage types: **Physical** is reduced by Armor (%), **Magic** by Ward (%).

## 8. Enemies (MVP)

| Enemy | Role | Trait taught |
|---|---|---|
| Goblin Grunt | melee | baseline |
| Goblin Archer | ranged | protect your back line |
| Goblin Brute | armored melee (50% armor) | bring Mages |
| (Stage 3 boss) Goblin Chief | tanky melee, larger | merging / Rally timing |

Start values (unbalanced; tune with headless sims once the simulator exists):

| Enemy | HP | Damage | Interval (s) | Range | Speed | Armor |
|---|---|---|---|---|---|---|
| Goblin Grunt | 90 | 10 | 1.0 | 1.0 | 1.2 | 0% |
| Goblin Archer | 50 | 9 | 0.9 | 4.0 | 1.0 | 0% |
| Goblin Brute | 160 | 16 | 1.4 | 1.0 | 0.9 | 50% |
| Goblin Chief | 420 | 24 | 1.2 | 1.2 | 1.0 | 20% |

All goblins deal Physical damage and have 0% Ward.

Enemies use the same rules as player units and their own 3×3 formation.

## 9. Battle

- Deterministic simulation at a fixed 20 ticks per second; presentation is
  separate (3D isometric view; 2D debug view for prototyping).
- Units spawn at the battlefield position mapped from their formation cell.
- **Targeting:** the nearest enemy, preferring the same lane on ties within
  0.5 cells. Targets are kept until they die.
- Melee units advance until in range; ranged units hold position until a target
  is in range, then fire; if nothing is in range they advance.
- No randomness. No pathfinding: straight-line movement with simple separation.
- **Rally:** the player taps the Rally button: all allies gain +50% damage and
  +50% attack speed for 5 seconds. One activation at a time; charges come from
  the puzzle.
- Battle ends when one side is wiped out. A 60 s timeout counts as a defeat.
- Controls: 1×/2× speed, pause.
- Geometry (start values, `Data/BattleRules.asset`): front rows start 6 cells
  apart, rows 1 cell apart, lanes 1.2 cells apart; allies keep 0.6 cells apart.
- All hits of a tick land together: a unit killed this tick still strikes. If
  both armies die on the same tick, it is a defeat.

## 10. Results & Progression (MVP)

- Victory: gold + stars. Stars: 1 = win, 2 = win with ≥ 50% of deployed units
  alive, 3 = win with all deployed units alive.
- Defeat: retry immediately; nothing is lost.
- Three stages in Chapter 1. Progress saved locally.

### Post-MVP progression order

1. Permanent class levels bought with gold (small % per level).
2. New classes as chapter rewards: Defender (blue), Support (yellow).
3. Daily Challenge: fixed seed, same board and enemy for everyone.
4. Expedition mode: 3–5 battles in a row with a persistent army (roguelite).
5. Commander heroes: one special slot with a unique Rally variant.

## 11. Stages (MVP)

| Stage | Name | Enemies | Lesson |
|---|---|---|---|
| 1-1 | Border Watch | 3 Grunts | match, recruit, place |
| 1-2 | Arrow Rain | 3 Grunts, 2 Archers | protect your back line, merge |
| 1-3 | The Chief's Guard | 2 Brutes, 2 Archers, Chief | Mages vs armor, Rally timing |

Formations (row 0 = front): 1-1 Grunts across the front; 1-2 adds Archers in
the back corners; 1-3 has Brute–Chief–Brute in front and Archers in the back
corners. Gold rewards: 30 / 40 / 60 (start values).
