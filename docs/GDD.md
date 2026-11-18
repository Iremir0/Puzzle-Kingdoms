# Puzzle Kingdoms â Game Design Document (v0.1)

Status: **MVP design locked for prototyping.** Every number in this document is a
starting value to be validated by playtests and headless balance simulations.

## 1. Pitch

> Every puzzle move builds your army. Every army decision wins the battle.
> And when it matters most, *you* sound the war horn.

Puzzle Kingdoms is a portrait mobile game in which the player solves a short
Match-3 puzzle to recruit an army, arranges and merges it on a 4×4 formation,
and watches it fight an auto battle â while holding a small number of
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
4. **Short sessions.** 2â4 minutes per stage. Battles last 20â40 seconds and can
   be sped up.

## 3. Differentiation (vs. Empires & Puzzles, Puzzle Quest, Auto Chess-likes)

| Competitors do | Puzzle Kingdoms does |
|---|---|
| Match-3 *is* the combat (gems = attacks) | Match-3 *builds* the army; combat is a separate tactical layer |
| Gacha heroes decide outcomes | Player decisions decide outcomes (enemy-aware recruiting, formation, merging) |
| Auto battlers are fully passive | Rally charges, earned in the puzzle, give timed command moments |
| RNG crits and misses | Deterministic combat: daily seeded challenges, replays, honest losses |

## 4. Core Loop

Stage Select â **Puzzle** (12 moves, enemy visible) â **War Camp** (formation,
merge, bench) â **Battle** (auto + Rally) â Results (stars, gold) â upgrades â
next stage.

## 5. Puzzle

- Board 6×6. Gem types in MVP (4):

| Gem | Color | Shape | Effect |
|---|---|---|---|
| Sword | Warrior Red `#E85B55` | Square | Warrior recruitment meter |
| Bow | Archer Green `#58C889` | Triangle | Archer recruitment meter |
| Staff | Mage Purple `#A77BE8` | Diamond | Mage recruitment meter |
| Banner | Kingdom Gold `#F4BE4F` | Star | Rally meter |

Every gem type has its own shape as well as its own color, so the board stays
readable for color-blind players (D-027).

- Swap two orthogonally adjacent gems. A swap that creates no match is rejected
  and costs no move. A match is a straight line of 3 or more gems, or a 2×2
  square, of one type.
- **Special matches** (D-028):

| Match | Result |
|---|---|
| Line of 3 | normal clear |
| Line of exactly 4 | normal clear, plus 2 bonus points for that gem's meter |
| 2×2 square | clears, and the swapped gem stays behind as a **Blast** gem (ring) |
| Line of 5 or more | clears, and the swapped gem stays behind as a **Prism** gem (sparkle) |

  A special gem keeps its color and its shape, and matches like a normal gem.
  **Blast**: when it is cleared in any match, it also clears the 3×3 area around
  it. **Prism**: swapped with any neighbour (always allowed) it clears every gem
  of the neighbour's type; cleared in a match it clears every gem of its own
  type. Special gems caught in an explosion go off too (chains). Every gem a
  special gem clears counts for the meters like a normal clear.
- **Stone gems** (D-032): some gems start a stage half turned to stone (their color and
  shape stay, half of the gem is drawn as cracked rock). A stone gem matches like
  any gem of its color, but the first match it takes part in (or the first Blast
  or Prism that reaches it) only breaks the stone: the gem stays where it is and
  scores nothing. The next match clears it. Stones are placed at the start of the
  puzzle, never next to each other, travel with their gem when it falls or is
  swapped, and no new ones appear during the stage. How many a stage has is
  stage data (`stoneGems`).
- **Moves:** 12 per stage (stage data can override).
- **Recruitment meter:** every cleared gem adds 1 point to its meter; every
  **8 points = 1 unit** (remainder carries over within the stage). A line of 4
  adds 2 bonus points. This rewards big matches smoothly and makes cascades
  predictable in value. Measured (D-028, after special matches): a random
  player recruits ~11 units in 12 moves, a greedy one ~16, so filling the 9
  slots and merging the surplus are real decisions.
- **Cascades** count fully and are shown as "Bonus!". With 4 gem types on 6×6
  a move clears ~7 gems on average, cascades included.
- **Rally meter:** 10 Banner points = 1 Rally charge, max 3 charges (typically
  1â2 per stage). Banner points
  beyond the cap convert to gold (1 gold per point, start value).
- If no valid move exists, the board reshuffles for free.
- The initial board never contains a ready-made match (lines or squares) and always has at least
  one valid move.
- The top of the puzzle screen shows the enemy formation and their traits.

Post-MVP: more special gems, stage objectives, boss board modifiers.

**Obstacles, combos and the boss (PB-20, D-050).** Stage data can add:
- **Chains** on gems: a chained gem can not be swapped, but it can be part of a match; the first
  hit (a match or a special effect) breaks the chain and the gem stays. Chains fall with their gem.
- **Ice** on cells, one or two layers: every hit on the cell (a cleared gem, a broken stone or chain)
  melts one layer. Ice stays on its cell. Goal *break all the ice*: every layer melted, then win.
- **Special combos** when two special gems are swapped into each other: Blast + Blast hits a 5×5
  square; Prism + Blast turns every free gem of the Blast's colour into a Blast and sets them all off;
  Prism + Prism hits the whole board. Two special gems can always be swapped.
- **Boss stones:** the boss drops K stones every N moves (never on a special gem, never two side by
  side); the puzzle shows the countdown.

## 6. War Camp (army preparation)

- 4×4 formation grid (D-031). **Rows** = depth (row 0 faces the enemy, row 3 is the
  back). **Columns** = four lanes, left to right.
- Unlimited **bench**; at most 16 units deployed.
- **Merge:** three units of the same class and tier â one unit of the next tier,
  max tier 3. Select a unit and press **Merge** to merge that one, or press
  **Merge all** to merge everything that can be merged in one go (merges chain:
  nine tier 1 become one tier 3). The screen says what will happen: the selected
  unit shows "3 x Warrior T1 -> T2 (HP, damage)", with nothing selected a
  "Merge all" line lists every merge, and bench units that have enough copies
  get a gold edge (D-030).
- Units still on the bench when the battle starts are dismissed for 5 gold each.
- Army is stage-local; nothing carries over in the campaign.

### Tier scaling (start values)

| Tier | HP & damage multiplier | Visual |
|---|---|---|
| 1 | ×1.0 | base |
| 2 | ×1.8 | larger, trim color |
| 3 | ×3.2 | larger, glow |

Intent: one merged unit is slightly stronger than three separate ones in raw
numbers but occupies one slot. With â¤ 16 units, spreading is usually better; with
more, merging is. Verified by headless simulation.

## 7. Units (Tier 1 start values)

| Class | HP | Damage | Type | Interval (s) | Range (cells) | Move speed (cells/s) | Notes |
|---|---|---|---|---|---|---|---|
| Warrior | 504 | 12 | Physical | 1.0 | 1.0 (melee) | 1.2 | Frontline |
| Archer | 252 | 10 | Physical | 0.8 | 4.0 | 1.0 | Back line DPS |
| Mage | 231 | 22 | Magic | 1.6 | 3.5 | 0.9 | Ignores armor; AoE post-MVP |

HP of every unit and enemy was tripled in the first balance pass (D-026) and raised
by 40% in D-033, so battles reach the 20â40 s target; damage is unchanged.

Damage types: **Physical** is reduced by Armor (%), **Magic** by Ward (%).

## 8. Enemies (MVP)

| Enemy | Role | Trait taught |
|---|---|---|
| Goblin Grunt | melee | baseline |
| Goblin Archer | ranged | protect your back line |
| Goblin Brute | armored melee (50% armor) | bring Mages |
| (Stage 3 boss) Goblin Chief | tanky melee, larger | merging / Rally timing |

Values after the balance passes (D-026, D-033, `Tools/Balance`):

| Enemy | HP | Damage | Interval (s) | Range | Speed | Armor |
|---|---|---|---|---|---|---|
| Goblin Grunt | 378 | 10 | 1.0 | 1.0 | 1.2 | 0% |
| Goblin Archer | 210 | 9 | 0.9 | 4.0 | 1.0 | 0% |
| Goblin Brute | 672 | 16 | 1.4 | 1.0 | 0.9 | 50% |
| Goblin Chief | 1764 | 30 | 1.2 | 1.2 | 1.0 | 20% |

All Chapter 1 goblins deal Physical damage and have 0% Ward.

**Chapter 2 enemies (PB-18, D-051).** Each changes how you recruit or place units:

| Enemy | Role | Behaviour | Trait taught |
|---|---|---|---|
| Shield Goblin | armored melee | blocks 50% of ranged damage while it is the front-most unit of its side | bring Warriors and Mages |
| Goblin Shaman | healer | every 2 s restores 60 HP to the most hurt ally within 3.5 cells | kill it first |
| Wolf Rider | fast melee | targets the back-most formation row first | protect the back row |
| Goblin Bomber | splash ranged | hits others within 1.5 cells of its target for 50% | spread your units |

| Enemy | HP | Damage | Type | Interval (s) | Range | Speed | Armor | Ward |
|---|---|---|---|---|---|---|---|---|
| Shield Goblin | 756 | 12 | Physical | 1.2 | 1.0 | 0.9 | 20% | 0% |
| Goblin Shaman | 231 | 8 | Magic | 1.6 | 3.5 | 0.9 | 0% | 20% |
| Wolf Rider | 336 | 14 | Physical | 0.9 | 1.0 | 2.0 | 0% | 0% |
| Goblin Bomber | 252 | 22 | Physical | 2.0 | 3.0 | 0.9 | 0% | 0% |

"Ranged" means an attack range above 1.5 cells. Heals land after the hits of a tick, so a unit killed
this tick is not saved. "Back row" is the formation row a unit started in. Until the four Meshy models
exist (ART.md Â§7) the grey-box silhouettes of Brute, Archer and Grunt stand in for them.

Enemies use the same rules as player units and their own 4×4 formation.

## 9. Battle

- Deterministic simulation at a fixed 20 ticks per second; presentation is
  separate (3D isometric view; 2D debug view for prototyping).
- Units spawn at the battlefield position mapped from their formation cell.
- **Targeting:** the nearest enemy, preferring the same lane on ties within
  0.5 cells. Targets are kept until they die.
- Melee units advance until in range; ranged units hold position until a target
  is in range, then fire; if nothing is in range they advance.
- No randomness. No pathfinding: straight-line movement with simple separation.
- **Rally:** the player taps the Rally button: all allies gain +35% damage and
  +35% attack speed for 5 seconds. One activation at a time; charges come from
  the puzzle.
- Battle ends when one side is wiped out. A 60 s timeout counts as a defeat.
- Controls: 1×/2× speed, pause.
- Geometry (start values, `Data/BattleRules.asset`): front rows start 6 cells
  apart, rows 1 cell apart, lanes 1.2 cells apart; allies keep 0.6 cells apart.
- All hits of a tick land together: a unit killed this tick still strikes. If
  both armies die on the same tick, it is a defeat.

## 10. Results & Progression (MVP)

- Victory: gold + stars. Stars: 1 = win, 2 = win with â¥ 50% of deployed units
  alive, 3 = win with all deployed units alive.
- Defeat: retry immediately; nothing is lost.
- Three stages in Chapter 1. Progress saved locally.

**The Kingdom (PB-22, D-052).** Gold buys building levels; each is a small permanent bonus. Numbers on
`Data/KingdomRules.asset`: 4 buildings, 10 levels, +3 % per level (+30 % at the cap), level cost
60 × 1.4^level × the building's cost factor, rounded to 5:

| Building | Improves | Cost factor | Level 1 | Level 5 | Level 10 |
|---|---|---|---|---|---|
| Barracks | Warrior HP | 1.0 | 60 | 230 | 1240 |
| Archer Tower | Archer attack speed | 1.0 | 60 | 230 | 1240 |
| Mage Tower | Mage damage | 1.2 | 70 | 275 | 1490 |
| Treasury | victory gold of a stage | 1.5 | 90 | 345 | 1860 |

All four to level 5 is about 3,100 gold; a full pass of both chapters pays about 1,500, so levels come
from replaying. Bonuses apply to the player's units only, when the battle is set up (HP, damage rounded,
attack interval divided by the speed bonus); the Treasury raises victory gold, not dismissal or Banner gold.
The save is version 3 (levels by building id); older saves load with no buildings. A bottom tab bar
(Map Â· Kingdom Â· Commanders Â· Shop) switches screens; Commanders and Shop are greyed until PB-23 and PB-30.
Measured with `Tools/Balance --kingdom N` (every building at level N, greedy bot, no Rally): 1-3 81% → 91% at
level 5; 2-1 92% → 96%; 2-5 60% → 77%; 2-10 46% → 61% (68% at level 10). Level 0 stays winnable, level 5 helps
without making the boss trivial.

### Post-MVP progression order

1. Permanent class levels bought with gold (small % per level).
2. New classes as chapter rewards: Defender (blue), Support (yellow).
3. Daily Challenge: fixed seed, same board and enemy for everyone.
4. Expedition mode: 3â5 battles in a row with a persistent army (roguelite).
5. Commander heroes: one special slot with a unique Rally variant.

These are now board cards (PB-22, PB-21, PB-25, PB-26, PB-23) inside the wider growth plan
of D-045 (16 cards, PB-18 to PB-33, in four waves).

## 11. Stages (MVP)

| Stage | Name | Enemies | Lesson |
|---|---|---|---|
| 1-1 | Border Watch | 6 Grunts | match, recruit, place |
| 1-2 | Arrow Rain | 5 Grunts, 3 Archers | protect your back line, merge |
| 1-3 | The Chief's Guard | 2 Brutes, 2 Archers, Chief | Mages vs armor, Rally timing |

Formations (row 0 = front, four lanes): 1-1 a front row of 4 Grunts and 2 behind
them; 1-2 a front row of 4 Grunts, 1 in the second row and 3 Archers in the back
row; 1-3 has BruteâChiefâBrute (in lanes 0, 1, 2) in front and Archers in the back
corners. Gold rewards: 30 / 40 / 60 (start
values).

Each stage raises the difficulty through data (D-032, D-033):

| Stage | Stone gems | Enemy HP |
|---|---|---|
| 1-1 | none | ×1.0 |
| 1-2 | 3 | ×1.1 |
| 1-3 | 5 | ×1.2 |

(`stoneGems`, `enemyHpMultiplier`, `enemyDamageMultiplier` on the stage asset; the
HP multiplier applies to every enemy of the stage on top of the unit asset and
the tier.)

**Stage goals and start boards (PB-19, D-048).** A stage may ask for more than winning:
*break N stones* or *win within N seconds*; missing the goal is a defeat. A stage may also
start from a hand-painted board (placed gems and stones, the rest random). Made with the
Stage Editor, not yet in the campaign:

| Stage | Name | Enemies | Goal | Board | Lesson |
|---|---|---|---|---|---|
| 1-4 | The Quarry | as 1-2 | break 5 stones | 4 painted + 2 random stones | match next to stones |
| 2-1 | Frozen Gate (draft) | as 1-2 | break all the ice | 14 ice layers in the middle, 12 moves | aim your matches |
| 2-2 | The Chief's Forge (draft) | as 1-3 | defeat the army | 4 chained corner gems; boss drops 2 stones every 3 moves | play around the boss |

**Chapter 2: Goblin Fort (PB-18, D-051).** Ten stages, campaign order after 1-3, `chapter: 2` on the
stage asset, its own map. One lesson each; the last is the boss stage. Rally is taught again in 2-8 and 2-10.

| Stage | Name | Enemies | Goal / board | Lesson |
|---|---|---|---|---|
| 2-1 | Shield Wall | 2 Shield Goblins, 2 Grunts, 2 Archers | 3 stones | arrows bounce off the front shield |
| 2-2 | Frozen Gate | 2 Shield Goblins, 2 Grunts, 2 Archers | break all the ice | aim your matches |
| 2-3 | Healer's Camp | 2 Brutes, 2 Grunts, 2 Shamans | 3 stones | kill the healers first |
| 2-4 | Wolf Pack | 2 Grunts, 3 Wolf Riders | 3 stones | the back row gets dived |
| 2-5 | Powder Keg | 2 Brutes, 2 Shields, 3 Bombers | 3 stones | spread your units |
| 2-6 | Chain Gang | 2 Shields, 2 Wolves, 2 Shamans | 4 chained corner gems | match chains open |
| 2-7 | Stone Pit | 2 Brutes, Shaman, Bomber, 2 Wolves | break 4 stones | stones first |
| 2-8 | Race the Clock | 4 Grunts, 2 Wolves | win within 30 s | fast army, Rally |
| 2-9 | The Chief's Forge | Chief, 2 Brutes, Shaman, Bomber | chains; boss stones every 3 moves | play around the boss |
| 2-10 | Goblin Fort Gate | Chief, 2 Shields, Wolf, Shaman, Bomber | boss stones every 3 moves | everything at once |

Enemy HP multipliers 1.35, 1.35, 1.2, 2.4 (damage x1.2), 1.1, 1.45, 1.7, 1.85, 1.05, 1.2; gold 70 to 220.
Measured (300-400 runs, greedy puzzle bot, auto formation, no merges, without Rally): 2-1 92%, 2-2 89%, 2-3 70%,
2-4 84%, 2-5 60%, 2-6 62%, 2-7 71%, 2-8 67%, 2-9 51%, 2-10 46%; with Rally 6-20 points higher.
The random bot wins 13-58%. Nodes sit in a three-column sawtooth on the map (placeholder layout, see PB-18 notes).

Measured difficulty (D-033, 400 runs; random/greedy puzzle bot, auto formation, no merges;
without / with Rally): 1-1 won 94% / 96% (random) and 100% (greedy), battles 16â22 s
without Rally; 1-2 won 74% / 81% and 96% / 98%, 23â28 s; 1-3 won 42% / 54% and 81% / 87%,
38â41 s. With Rally battles are 6â10 s shorter.
