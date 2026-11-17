# Decision Log

Short records of decisions that shape the project. Newest at the bottom.
Format: decision — why — alternatives rejected.

## D-001 Puzzle and battle are separate phases
Why: readable on a phone, simpler to build, each phase gets full attention.
Rejected: simultaneous puzzle + battle (split attention, high complexity).

## D-002 Four gem types: Warrior, Archer, Mage, Banner
Why: three colors on 6×6 cascade almost constantly and make the army random.
Banner adds a "more units vs. more Rally" trade-off.
Rejected: 3 colors; 5 colors with Defender (scope).

## D-003 Recruitment meter (3 gems = 1 unit), cascades count fully
Why: smooth reward for bigger matches, predictable value, rewarding cascades.
Rejected: 1/2/3 units for match 3/4/5 (overflows 9 slots fast); cascades → gold
only (less exciting).

## D-004 Unlimited bench, 9 deployed, leftovers dismissed for gold
Rejected: stopping recruitment at 9; auto-merging overflow.

## D-005 No army carry-over in the campaign
Why: prevents snowballing, keeps stages balanceable. Carry-over is reserved for
the post-MVP Expedition mode.

## D-006 Manual merge, tier multipliers ×1.8 / ×3.2
Why: keeps "wide vs. tall" as a player decision. Values tuned by headless sims.

## D-007 Formation rows = depth, columns = lanes; same-lane targeting tie-break
Why: makes placement matter without pathfinding.

## D-008 Enemies follow the same rules as the player
Special behaviors (divers, healers) arrive later as taught mechanics.

## D-009 Deterministic combat, no RNG
Why: honest losses, easy testing, enables replays and seeded Daily Challenges.

## D-010 Rally: player-timed army buff, charged by Banner gems
Why: core differentiator; gives the auto battle a moment of command and links
puzzle decisions directly to battle.

## D-011 Do not fork the Kenney Match-3 Starter Kit (Godot-era; still valid for its art)
Why: its logic is coupled to nodes and lacks group detection, move limits and
deadlock handling. We write our own tested `BoardModel` and may reuse its CC0
art/audio as placeholders. (Code: MIT — keep the notice if any is copied.)

## D-012 ~~Godot 4.6, Mobile renderer, GDScript~~ (superseded by D-018), portrait 1080×1920 base
Why: Mobile renderer targets Android Vulkan devices; fall back to Compatibility
if low-end device tests fail.

## D-013 ~~Minimal in-house test runner instead of GUT~~ (superseded by D-019)
Why: zero third-party code to vendor, enough for pure-logic tests. Revisit if
we need mocking, parameterized tests or scene tests.

## D-014 Art direction split
Semi-realistic quality is concentrated in 2D (portraits, cards, UI,
backgrounds). Battlefield units are mid-poly 3D with strong silhouettes, because
they render at roughly 80–120 px on a phone. Prototype uses CC0 placeholders.

## D-015 Enemies reuse UnitDef
Why: D-008 (same rules for both sides) means one definition type is enough.
In Unity: one `UnitDefinition` ScriptableObject type for player units and
goblins; it builds the plain logic `UnitDef`.
Rejected: separate EnemyDef (duplicate fields).

## D-016 Merge consumes bench copies first; merged unit takes the target's place
Why: merging should not silently punch holes in a formation the player built.
The unit the player merges into keeps its cell (or stays on the bench).
Rejected: consuming deployed copies first; always returning the result to the
bench.

## D-017 Instant hits, simultaneous resolution, mutual wipe = defeat
Why: instant hits keep the M1 simulator small (projectiles are a view effect);
resolving all hits of a tick together removes any advantage from unit order.
A defeat on mutual wipe keeps "win" unambiguous for stars.
Rejected: simulated projectiles (more state, little gameplay value in M1);
sequential damage in id order (player side would always strike first).

## D-018 Switch from Godot to Unity 6 LTS (C#, URP), 2026-11-13
Why: the game is a free-to-play mobile title. Unity has first-party, mature
SDKs for ads, in-app purchases, analytics and remote config; a much larger
asset store for the 3D battle (M4); better-proven Android/iOS tooling; and far
more learning material for a team new to game engines. Switching cost was
small: only engine-free logic existed (ported 1:1 with its tests).
Rejected: staying on Godot (monetization plugins are community-maintained,
smaller asset ecosystem). Kept from D-012: portrait 1080×1920 base, Android
first. Render pipeline: URP (mobile default). UI: uGUI + TextMeshPro.

## D-019 Engine-free logic assembly, tested with dotnet and Unity
`PuzzleKingdoms.Logic` has `noEngineReferences: true`. `Tools/LogicTests`
compiles the same logic and NUnit test files with C# 9 and runs them via
`dotnet test`, so tests run in seconds without opening Unity (and in CI
later). The same tests run in Unity's Test Runner.
Rejected: tests only inside Unity (slow, editor needed); logic using Unity
types like `Vector2Int` (would tie tests to the editor).

## D-020 PCG32 for all gameplay randomness
`SeededRandom` (PCG32) gives bit-identical sequences on Mono, IL2CPP and
.NET, which daily challenges and replays need. `System.Random` and
`UnityEngine.Random` make no such promise.

## D-021 Float combat for now; fixed-point only if replays must match across devices
Combat uses `float` (`System.Numerics.Vector2`). Results are identical on one
device and build, which covers tests and local replays. Floating-point results
can differ between CPUs/compilers; if shared replays or competitive daily
challenges need cross-device equality, switch the simulator to fixed-point.

## D-022 Stage rewards: earned gold is kept on defeat, stage gold only on victory
Dismissal gold (5 per bench unit) and Banner overflow gold (1 per point) are
earned during the attempt and paid even on defeat; the stage's gold reward and
stars only on victory, every time it is won. Next stage unlocks with 1 star.
Open question: repeat wins pay full gold; revisit (first-clear bonus) once
upgrades make gold matter.

## D-023 StageRun is logic, screens only drive it
The whole attempt (puzzle → war camp → battle → reward) is one engine-free
`StageRun`, testable headless. Scenes call it and listen to its events;
`GameContent` (ScriptableObject) builds it from data.

## D-024 Unity 6000.3.28f1, lean package set, one mobile URP asset, 2026-11-14
Pinned to Unity 6.3 LTS 6000.3.28f1. The manifest keeps only what we use:
URP, uGUI (includes TextMeshPro), Test Framework, Timeline, 2D Sprite, IDE
integrations and the engine modules they need. Template extras (Purchasing,
Analytics, Collab, XR, Terrain, Vehicles…) were removed; add them back when a
feature needs them. `ProjectSetup` creates one URP asset (`URP_Mobile`,
Universal Renderer, HDR off, MSAA 2x) and assigns it to every quality level,
so the 2D grey-box and the later 3D battle share one pipeline.

## D-025 Grey-box: one generated scene, screens built from code, 2026-11-14
For M1 the whole game runs in one scene (`Scenes/Main.unity`) that
`SceneBuilder` generates; it holds a camera and `GameApp`. Screens are uGUI
panels created in code (`Views/Common/Ui`), swapped by `GameApp`, with the
built-in font. Reasons: no hand-edited scene/prefab YAML while the layout
changes daily, screens are testable from PlayMode tests, and nobody on the
team needs editor skills yet. Replaces the planned SceneRouter + one scene per
screen; revisit at M2/M3 when real UI (prefabs, TextMeshPro, art) arrives.
Views get callbacks and never reference Flow.

## D-026 First balance pass from headless runs, 2026-11-14
`Tools/Balance` plays every stage 300–500 times with bots (random or greedy
puzzle moves, `AutoFormation`, Rally never or as soon as possible) on the
real assets. Before: ~22 units and 3 Rally charges every stage, battles 3–15 s,
boss stage won 89–100%. Changes:
- 6 points per unit (was 3) and 10 Banner points per Rally charge (was 3):
  ~10 units (random) / ~14 (greedy), 1.8 / 2.4 Rally charges.
- All HP ×3 (damage unchanged) so fights last longer and read better.
- Rally +35% damage and attack speed (was +50%): at +50% the boss stage was
  0% without Rally and ~90% with it; Rally should help, not decide alone.
- Goblin Chief damage 24 → 30; 1-1 has 4 Grunts, 1-2 4 Grunts + 3 Archers.
Targets (random bot with Rally / greedy bot with Rally): 1-1 ~100%,
1-2 ≥ 90%, 1-3 60–80% / ≥ 90%. Results in GDD §11. Known gaps: 1-1 and 1-2
battles are 7–19 s (GDD wants 20–40 s; accepted for tutorial stages); bots do
not merge or time Rally, so real players should do somewhat better. Re-run the
tool after every data change.

## D-027 Every gem type has its own shape, 2026-11-14
Gems were colored squares with a letter. Each type now also has a shape:
Warrior square, Archer triangle, Mage diamond, Rally star, so the board is
readable without color (color-blind players) and without text. Sprites are
generated in code (`GemShapes`, white, tinted by `Image.color`), so no art
files or credits are needed. Unit classes get shapes in registration order
(`Palette.Register`); a fifth class would reuse the circle.

## D-028 Special matches: 4-line bonus, square → Blast gem, 5-line → Prism gem, 2026-11-14
A 2×2 square now counts as a match (and a valid swap), so squares are possible
at all; generated boards never start with one. Results:
- Line of exactly 4: +2 bonus points on that gem's meter (`PuzzleRules.FourMatchBonusPoints`, data asset).
- Square: clears 4 and leaves a **Blast** gem where the swapped gem was; when
  that gem is cleared in a match it also clears the 3×3 around it.
- Line of 5+: clears and leaves a **Prism** gem; swapped with any neighbour it
  clears all gems of the neighbour's type, cleared in a match it clears all of
  its own type. A Prism can always be swapped, so a board with a Prism is never stuck.
- A special gem keeps its color/type and stays on the board until used. It is
  chosen from a group in this order: line of 5+ beats square beats nothing; it
  never replaces a gem that is already special; specials caught in an
  explosion fire too (chain). Everything cleared counts for the meters.
- The anchor is the swapped gem if it is in the match, otherwise the middle
  cell; in cascades always the middle cell.
Why a gem that stays instead of an instant effect: the player can plan around
it (the user's call, option 2). Views show a Blast as a ring and a Prism as a
sparkle over the gem.
Balance effect (`Tools/Balance`, 300 runs): units recruited per stage rose from
10.4 / 14.0 (random / greedy bot) to 14.8 / 21.9 and Rally charges from 1.8 / 2.4
to 2.4 / 2.8; 1-3 with Rally went from 77% / 96% to 90% / 100%. Open: raise
`pointsPerUnit` from 6 to 8 to get back to 10.7 / 16.0 units. Not applied yet.
Applied (D-028 follow-up): `pointsPerUnit` 6 → 8. Measured with `Tools/Balance`
(300 runs): recruited units 10.7 / 16.0 (random / greedy bot), Rally charges
2.4 / 2.8, 1-3 with Rally 76% / 98%: back to the D-026 targets.

## D-029 Grey-box battle units are code-drawn silhouettes, 2026-11-14
Battle units were colored squares with a letter. Each unit asset now has a
`look` (`UnitLook`: Warrior, Archer, Mage, GoblinGrunt, GoblinArcher,
GoblinBrute, GoblinChief, Generic) and the battle view draws a silhouette sprite
for it, tinted with the unit color (details darker): shield and sword, hood and
bow, pointed hat and staff, long-eared goblins, an armored brute with an axe, a
crowned chief. The geometry is plain math (`SilhouetteMath`, no engine types),
rendered once into a sprite (`UnitSilhouettes`), so there are no art files and
no credits. Only the tier is written on a unit now ("T2"). This is a
placeholder for real art, not the final look: realistic models or sprites
(asset pack vs. freelancer) are still open (ROADMAP M3/M4).

## D-030 "Merge all" button and merge preview, 2026-11-14
Merging unit by unit (select, press Merge) was tedious once the puzzle gives
~11-16 units. The War Camp now has a **Merge all** button next to the per-unit
Merge: `Roster.MergeAll()` merges everything that can be merged, lowest tier
first, so merges chain (9 tier 1 → 3 tier 2 → 1 tier 3). It keeps D-016: a
deployed unit is the merge target when the class has one (the result keeps its
cell) and bench copies are consumed before deployed ones. `Roster.PlanMergeAll()`
computes the same result without doing it; the screen uses it to write what will
happen and which bench units are mergeable (gold edge). Per-unit Merge stays, so
the player can still keep three separate units instead of one strong one (D-006:
wide vs. tall is still a choice, now with one tap for the common case).
Rejected: automatic merging whenever three match (removes the choice).
Requested next, not built yet: 4×4 formation, rising enemy strength per stage,
and stage board modifiers (e.g. stone gems that vanish after two uses).

## D-031 4×4 formation (16 slots), enemy armies rebuilt for it, 2026-11-14
3×3 (9 units) was too small to make placement a real decision once the puzzle
gives 11–16 units. The formation is now 4 lanes × 4 rows (`Formation.Size = 4`).
Changes: lanes sit symmetrically around the middle (`(x - 1.5) * LaneSpacing`),
the battle view sizes its field from `Formation.Size`, `AutoFormation` sends long
range units to the back row (row 3) and fills lanes from the middle outwards.
Balance (`Tools/Balance`, 300 runs): with unchanged enemies the player fielded
10–14 units instead of 8–9 and 1-3 became easy (greedy bot without Rally 46% →
95%, 1-1 9 s). Enemy armies were rebuilt for 4 lanes:
- 1-1: 7 Grunts (front row of 4 + 3 behind).
- 1-2: 6 Grunts (front row of 4, 2 behind) + 3 Archers in the back row.
- 1-3: 2 Brutes + Chief in front, 2 Grunts behind, 2 Archers in the back corners.
Result (random bot / greedy bot): 1-1 wins 90–93% / 100% in 13–19 s; 1-2 76–87% /
97–99% in 12–21 s; 1-3 47–62% / 87–93% in 19–28 s: inside the D-026 targets except
battle length, which is shorter than the GDD's 20–40 s. Open: with the stage
modifiers and rising enemy strength asked for next, this is the baseline to
scale from; battle length is better fixed there than by tuning these numbers.

## D-032 Stone gems: the first match only breaks the stone, 2026-11-14
Requested difficulty feature: a gem can start the stage turned to stone; "the first
match it is in removes the stone, the second one clears it". It looks like its
normal gem with half of it cracked rock, so color and shape stay readable.
- A stone gem takes part in matches like any gem of its type (it counts for the
  line/square, for the 4-line bonus and for creating a special gem) and can be
  swapped. When it is part of a match, or reached by a Blast or a Prism, its stone
  breaks (`CascadeStep.Cracked`) and the gem stays on the board, scoring nothing.
  The next time it is cleared normally.
- Stones are placed once by `BoardModel.PlaceStones(StageDef.StoneGems)` after the
  board is generated: random cells, never two orthogonally adjacent (so a match can
  not be left standing after its stones break), no new stones during play. They
  fall, swap and shuffle together with their gem. A special gem is never created on
  a stone cell.
- Per stage (data, `stoneGems`): 1-1 none, 1-2 three, 1-3 five.
Balance (`Tools/Balance`, 300 runs): stones cost 4-8% of the units recruited and
1-3 got harder, so a Grunt left 1-3 (2 → 1). Result: 1-2 wins 72% / 85% (random bot
without / with Rally) and 94% / 99% (greedy); 1-3 45% / 59% and 83% / 88%: at the
low edge of the D-026 targets, on purpose, since the stage is meant to be the hard
one. Not built: stones that appear during a stage, 2+ layer stones, other
modifiers (fewer moves, no Rally); rising enemy strength beyond what the three
stages already do.

## D-033 Rising enemy strength per stage from data; unit HP ×1.4, 2026-11-14
Board item M1-02. Each stage now has `enemyHpMultiplier` and `enemyDamageMultiplier`
(default 1, on the stage asset): `Formation.ForStage` builds the enemy army with
scaled copies of the unit definitions (`UnitDef.Scaled`), tier still multiplies on top.
Nothing is hardcoded: 1-1 ×1.0, 1-2 ×1.1, 1-3 ×1.2 HP, damage ×1.0, together with
the stone gems of D-032 (0 / 3 / 5).
Battle length: raising only the enemies' HP lengthened battles but broke win rates
(1-3 greedy bot with Rally 74%), because the player's army is limited. Scaling both
sides keeps outcomes (Lanchester) and only stretches time, so the HP of all units and
enemies went up by 40% (warrior 504, archer 252, mage 231, grunt 378, goblin archer
210, brute 672, chief 1764) and the stage multipliers carry the progression.
Enemy counts were trimmed to match: 1-1 6 Grunts, 1-2 5 Grunts + 3 Archers, 1-3 2
Brutes + Chief + 2 Archers.
Result (`Tools/Balance`, 400 runs, random bot / greedy bot): 1-1 wins 94-96% / 100%,
16-22 s without Rally; 1-2 74-81% / 96-98%, 23-28 s; 1-3 42-54% / 81-87%, 38-41 s.
Battles with Rally are 6-10 s shorter, so the early stages with Rally are under the
20 s target. Still open: the random bot loses 1-3 more often than the D-026 target
(60-80%); real players should do better (they merge and time Rally), to be checked in
the playtests (M1-04). Not built: showing the multiplier on the stage select / enemy
preview; per-stage damage multipliers are supported but all 1.0.

## D-034 Ticket branches off main, merged back when tests pass, 2026-11-14
Work is tracked as tasks in `docs/BOARD.md`, coded by milestone (`M1-01`). Each
card gets its own branch from the latest `main` (`feature/M1-01-<short-name>`)
and is merged back into `main` once the logic and Unity tests pass. There is no
separate `dev` branch (the team's call). Why: two developers worked on the same
area in parallel on 2026-11-14; one card per branch makes ownership visible
and keeps `main` playable.

## D-035 Board cards are PB-n and bigger, 2026-11-14
Cards on `docs/BOARD.md` are coded `PB-1`, `PB-2`, ... (a running number, never
reused) instead of milestone codes (`M1-03`), and branches follow them
(`feature/PB-3-<short-name>`). Why: the old cards were half-day pieces and too
small to follow; a card is now a playable slice with a "Done when" checklist, and
the milestone is just a column. Each card has a detail file `docs/cards/PB-n.md`
(why, current code, steps, mockup, prototype assets, Unity steps, checklist, notes)
so either developer can pick it up cold. Old codes stay in the board's *Replaces* field and
in older decisions (D-033 mentions M1-02, M1-04). Supersedes the code format of D-034.

## D-036 Placeholder sounds are generated in code, 2026-11-16
The PB-8 sounds (match pop, Blast, Prism, stone crack, invalid swap, unit ding,
Rally horn, music loop) are synthesized by an editor tool
(`Puzzle Kingdoms → Generate Placeholder Sounds`, `Scripts/Editor/PlaceholderSounds.cs`)
into `Audio/Placeholder/*.wav` instead of downloading CC0 packs. The clips are
referenced only through `Data/SoundBank.asset`, which `GameContent` links. Why: no
downloads or licences to track for throw-away sounds, the same files come out on
every machine, and the timing and pitch rules (cascade pitch +0.08 per wave, capped
at 1.4) can be tested now. Real sounds later replace the clips in the SoundBank
(add them to CREDITS.md first); no code change needed.

## D-037 Playtest log: one JSON line per finished stage attempt, 2026-11-14
Written on the PB-5 branch as D-036 and renumbered when merging, because PB-8 took
D-036 first.
For the playtests (PB-5) the game appends a `StageAttempt` record to
`playtest-log.jsonl` next to the save file whenever a battle ends: stage, outcome,
stars, time on the puzzle and in the War Camp, battle length, units recruited per
class, merges, units deployed/surviving/dismissed, Rally earned/used, the formation
and gold. The record is built in Logic (`StageAttempt.From(StageRun)`, tested);
Flow adds the clock fields and writes the file (`PlaytestLog`). Merges are derived,
not counted: recruited − deployed − dismissed, divided by two. Why: observers miss
things and testers misremember; the log answers "did they merge, use Rally, put
archers in the back" for every attempt. Local only, no personal data, nothing is
sent. Stays on during M1-M5; turn it off (or behind a debug flag) in the release
build (PB-17). Summaries: `Tools/PlaytestSummary`.

## D-038 Undo in the War Camp takes back merges, until the battle starts, 2026-11-17
`Roster` saves the bench and the formation before every Merge and every Merge all
(one step for the whole Merge all) and `Roster.Undo()` puts them back exactly,
the same unit objects on the same cells (PB-9). Steps stack, so several merges can
be undone in reverse order. Deploy moves are not undo steps: they are already
reversible by dragging, but an undo also takes back the moves made after that
merge, because it restores the saved picture. `StageRun.StartBattle` clears the
stack. Why: merging is the one War Camp action the player cannot reverse by hand,
and testers merge by accident while learning; after the battle starts the army is
committed. Unit uids are never reused after an undo, so views can still tell units
apart.
