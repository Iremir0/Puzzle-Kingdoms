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

## D-039 Settings in their own file; how saves survive crashes and bad files, 2026-11-17
- **Where settings live:** `settings.json` next to `profile.json` (`SettingsStore`,
  model `GameSettings` in Logic): sound effects, music, the speed battles start at.
  A separate file because "Reset progress" must delete stars and gold but keep the
  settings, and a broken settings file must never cost progress (it just gives the
  defaults). The puzzle screen's sound button changes the same settings.
- **Crash safety:** both files are written through `SafeFile`: the new text goes to
  `.tmp`, then `File.Replace` swaps it in, so a crash leaves the old or the new file,
  never none (the old code deleted and then moved). If only a `.tmp` is found (left
  by the old code), it is read.
- **Bad or newer files:** a profile that cannot be read, or one with a newer save
  version (after installing an older build), is copied to `profile.json.bak` and a
  fresh profile starts. The game never refuses to start over a save file; the
  `.bak` lets us recover progress by hand. (A newer file is now also never written
  over, see D-067.)
- **Reset progress** asks twice and saves an empty profile right away.
Why: losing progress loses players (PB-14), and testers share PCs (PB-5).

## D-040 Tutorial prompts live on the stage assets; seen prompts in the profile, 2026-11-17
Each `StageDefinition` has a `tutorial` list (id, trigger, text, target UI name)
and a `mapPosition` for the chapter map (PB-13). The card suggested a separate
`TutorialSteps` asset; keeping the prompts on the stage puts a stage's lesson next
to its enemies and difficulty, and needs no extra asset to link. Triggers:
puzzle started, unit recruited, War Camp opened, battle started, battle lost.
The profile (save version 2) stores the ids of prompts already shown and whether
the tutorial was skipped; a prompt counts as seen as soon as it is shown, so a
crash never shows it twice. Version 1 saves load with nothing seen. "Skip
tutorial" turns all prompts off; Settings → Reset tutorial brings them back
without touching stars or gold; Reset progress resets both. While a prompt is up
the screen below takes no touches and the battle clock stops. Prompt ids must be
unique across the game (checked by `GameDataTests`).

## D-041 Art: KayKit (CC0) for our units, AI-made goblins on the free plan, 2026-11-17
Art budget is zero (Alican). Player units come from KayKit Adventurers (CC0;
the itch.io version adds a real Ranger for the Archer). The four goblins are made
by us with Meshy's free plan (prompts and steps in `docs/ART.md` §6). Free-plan
models are CC BY 4.0: commercial use is allowed but the game must credit Meshy
on a Credits screen, and each kept model goes into CREDITS.md first. Why: KayKit
reads well at phone size and already uses our class colours (PB-10 spike), but
has no goblins; AI keeps the goblins of the GDD at no cost. Fallback if the AI
goblins do not match the style: KayKit Skeletons as the enemy (CC0), which means
renaming the enemies in the GDD, data and tutorial texts. Downloaded models stay
in the git-ignored `Assets/_Project/Sandbox/` until chosen.

## D-042 Art: all seven characters made by us with Meshy Pro; replaces D-041's art source, 2026-11-18
Alican approved. Alican bought Meshy Pro, so every character (Hero_Knight, Hero_Archer,
Hero_Mage, Goblin_Grunt, Goblin_Archer, Goblin_Brute, Goblin_Chief) is made by us from our own
concept images (Nano Banana Pro in Meshy, then image → 3D) and lives in
`Assets/_Project/Art/Characters/`. Models made on a paid plan have a private licence: we own
the output and the game needs **no Meshy credit** (D-041's CC BY 4.0 credit screen no longer
applies to them). KayKit stays only in the git-ignored Sandbox as a spike comparison and is not
shipped. Why: one hand-painted style for all seven (KayKit next to the Meshy goblins looked like
two games), at about 53 credits per model. Triangle budget: about 5k per model, Brute 6k, Chief
8k (36.7k for the seven). Method, prompts and credits in `docs/ART.md` §7. Open: the models have
no rig (PB-12), and each model adds ~14 MB to the repository (consider Git LFS).

## D-043 Rigs and animation clips come from Meshy, not Mixamo, 2026-11-18
Alican approved. Meshy's own rigging (character type "Humanoid", skeleton template **Mixamo**)
rigged all seven models for 0 credits, and Meshy's preset animation library (run, attack
incl. axe spin, hit reaction, death and more) is free to apply on the Pro plan. Checked: the
Knight imported into Unity 6000.3 as a valid Humanoid avatar (24 bones mapped, no missing
bone), with its clip as human motion. Rigging needs the markers (chin, shoulders, elbows,
wrists, groin, knees, ankles) checked by hand on models with non-human proportions, and the
body centred on the symmetry line (Offset). Why: no Adobe account, no manual upload, one
skeleton for every character. Because all rigs are Humanoid, **clips are shared**: the
animation clips live on one model (the Knight) and are retargeted to every other character in
Unity (Animator Controller per class on top); the other six models are downloaded rigged
without clips. Known issues: the Mage's staff bends when the body moves (it is skinned to the
body, handle in PB-12), and nobody has yet checked whether the preset library has clips made for
a bow or a staff; until PB-12 does, Archer and Mage reuse the generic attack/hit/death clips.
**Unity import check of all seven (headless, Humanoid):** Knight, Mage, Goblin Archer, Brute and
Chief became valid Humanoid avatars as downloaded. The **Goblin Grunt** failed (`mixamorig:RightHand
is not an ancestor of Bone_047`: Unity auto-mapped a finger bone to one of Meshy's extra "SmartRig"
bones) and the **Hero Archer** rig-only download failed too (`RightHand not found`; its animated
download worked). Fix: in the Humanoid avatar mapping drop the finger bones (44 → 25 mapped bones,
fingers are not needed in battle): the Grunt then becomes a valid Humanoid with human motion.
PB-12 needs an import step for this (an `AssetPostprocessor` or a one-off editor tool that sets
Humanoid and removes the finger mapping) so it does not depend on clicks in the Inspector.
The clip FBX (Knight) holds five clips: Running, Walking, Attack (2.8 s), Dead, Hit_Reaction; none
loops by default and there is no idle yet (nothing in the library search was clearly an idle).
PB-12's "Mixamo" step is replaced by this. Rigged FBX files stay in the git-ignored Sandbox
(`Sandbox/Meshy/<Name>_Rig/`, `Clips_Knight/`) until PB-12 picks the final set.

## D-044 UI: one font, a six-step type scale and three button styles, 2026-11-18
Alican approved the font download. All UI text uses **Lilita One** (SIL OFL 1.1, `Resources/UiFont.ttf`,
in CREDITS.md): Unity's legacy `Text` cannot pick weights from a variable font, so one bold static family
serves titles and text. Sizes snap to six steps in `Ui.Type` (30 / 38 / 44 / 52 / 64 / 90 px at 1080 wide,
17 sizes before); buttons come in three styles in `Ui.ButtonStyle` (Primary blue, Secondary grey-blue,
Danger red, all from `Palette`) with one pressed look (the fill darkens). Contrast is checked by
`ContrastTests`: white on the three button fills ≥ 3:1 (their captions are large text), dark text on the
Rally gold and muted / gold text on the panels ≥ 4.5:1. Colours are GDD values and did not change.
TextMeshPro is not adopted (the grey-box UI stays on `Text` until real UI art, PB-16 notes).

## D-045 Growth plan: 16 cards in four waves; wave 1 starts before the go decision, 2026-11-18
Alican approved the growth proposal (Claude's, written as the game's owner) and asked to start
it. Cards PB-18 to PB-33 turn the three-stage prototype into a soft-launch game. Wave 1 does not
depend on the playtest result and starts now: **PB-32** Turkish and English, **PB-29** analytics,
remote config, crash reports and consent, **PB-19** stage tool and stage goals. Wave 2 waits for
a "go" in PB-6: PB-18 Chapter 2, PB-20 obstacles, PB-21 Defender and Support, PB-22 kingdom,
PB-23 commanders. Wave 3 leads to the soft launch: PB-24 quests, PB-25 Daily Challenge, PB-26
Expedition, PB-30 economy, PB-31 cloud save, PB-33 real audio. Wave 4: PB-27 async PvP, PB-28
replays. Soft-launch target: about 40 stages (Chapters 1-3; extending Chapter 1 and making
Chapter 3 get their own cards once PB-19 exists), kingdom, two commanders, Daily Challenge,
analytics, TR/EN, rewarded ads. **Never:** selling power, units or stars, loot-box heroes
(pillar 2), real-time PvP (too heavy for two people), an energy system that blocks play (pillar 4).
The plan uses determinism as the selling point (Daily Challenge, async PvP verified by re-running,
replays from inputs). Open owner decisions: the business model (PB-30) and a backend (online part
of PB-25, PB-27). Milestones M7-M9 added to the ROADMAP. Mockups of every card are on Alican's
growth screens page (linked from BOARD.md). Why: competitors have hundreds of stages, long-term
progress and daily reasons to return; we had none of these, and wave 1 is useful whatever the
playtests say (the playtests themselves benefit from analytics and Turkish).

## D-046 Texts: the English text is the key; one Turkish table; language in the settings, 2026-11-18
PB-32. Every text the player reads is looked up by its English source text: `Loc.T("Merge all")`,
`Loc.F("Moves: {0}", n)`. `Ui.Button` and `Ui.SetCaption` translate their captions themselves, so
button objects keep their English names in every language (tests and tutorial targets find them by
name). English has no table; a missing translation shows the English text, never a key.
- **Table:** `Resources/Strings/tr.txt`, one `English<TAB>Türkçe` pair per line, `#` notes, `\n` for a
  line break: a translator can edit it in any editor or spreadsheet, without Unity. Stage names,
  tutorial prompts and unit names stay English on the assets and go through the same table, so no
  asset changed.
- **Checked by `TurkishTableTests`** (Unity and `dotnet test`): the table reads cleanly, every row keeps
  its `{n}` placeholders, and every literal passed to `Loc.T` / `Loc.F` / `Ui.Button` / `Ui.SetCaption` in
  Views and Flow, and every stage, unit and tutorial text in the assets, has a Turkish row. `Loc.T($"…")`
  fails the test. `LocalizationTests` checks that Lilita One draws every character of the table.
- **Numbers:** "2.5" in English, "2,5" in Turkish, from a number format built in code rather than
  `CultureInfo("tr-TR")`, so it does not depend on the platform's culture data.
- **Language choice:** `language` in `settings.json`: "" follows the phone (Turkish on a Turkish phone,
  English anywhere else), "en" or "tr" is the player's choice from Settings → Language, which redraws
  the map and the settings at once. The settings file stays version 1 (a new field with a default).
- **Words:** Rally is "Hücum" in Turkish; the one-letter class tags W / A / M become S / O / B.
Why: one plain file is the simplest thing a translator can work on, and the English code stays readable;
Unity's Localization package (string tables, smart strings, an editor of its own) is more than about 110
texts need. If we add many languages or plural rules beyond "{0} taş", revisit.

## D-047 Statistics and error reports only after an opt-in; local files until a service is chosen, 2026-11-18
PB-29. The first launch asks before anything is recorded (KVKK and GDPR want consent that is freely
given and specific): three switches (play statistics, error reports, personalised ads), all **off** at
first, "Allow all" or "Save my choice"; Settings → Data and privacy asks again. The answer is stored in
`settings.json` with the version of the consent text (`Consent.CurrentVersion`); raising it asks
everyone again. Without consent an event is dropped, not queued.
- **Events** are built in Logic (`GameEvents`, tested): app_started, stage_started, stage_won /
  stage_lost (numbers from the playtest record, no formation or free text), tutorial_skipped,
  setting_changed. The full list, and where data goes, is `docs/PRIVACY.md`.
- **Where they go:** `AnalyticsHub` (Flow) passes events to sinks only with consent. Today the only
  sink is a local `analytics-log.jsonl` in tester builds (editor, Windows playtest, Android development);
  errors go to `error-log.jsonl` (exceptions only, 20 per session). A store build records nothing until
  an online service is connected; that is one more sink, no game code changes.
- **Remote config:** `StageTuning` applies `stage.<id>.moves | stones | enemy_hp | enemy_damage` overrides
  when a run starts; out-of-range values are ignored. Today the values come from a local
  `remote-config.txt` (tester builds), later from the service. The stage map card still shows the
  asset's numbers.
- **Open (owner):** which service (Unity Gaming Services or Firebase), the account, and a public privacy
  policy URL for Google Play.
Why: the playtests need numbers per stage, and doing consent right from the first build is cheaper than
adding it to a released game; keeping the service behind a sink keeps that choice open.

## D-048 Stage goals, hand-painted start boards, one balance runner and a Stage Editor, 2026-11-18
PB-19.
- **Goals** (`StageGoal`): *defeat the army* (every stage so far), *break N stones*, *win within N seconds*.
  A goal always comes on top of winning the battle; missing it is a defeat: no stars, no victory gold
  (dismissal and Banner overflow gold are still paid, as on any defeat). `StageRun.Won` is the stage
  result and the playtest record's `victory` now means it. The goal shows in the top right of the enemy
  panel on the puzzle (with progress, "2/5") and in the War Camp, on the stage card, next to the battle
  clock ("12.3 / 30 s") and on the results ("Goal missed: …").
- **Start boards** (`BoardLayout`): six text rows on the stage asset, `.` random, `0`–`9` a gem type,
  `a`–`j` that type in stone, `x` a random gem in stone. Placed gems may not already match and stones may
  not touch (as in D-032); random cells are drawn again until no match stands and a move exists. The
  stage's `stoneGems` still adds random stones on top. Empty = a random board, as before.
- **One balance runner** (`BalanceRunner` in Logic, bots unchanged from D-026): `Tools/Balance` and the
  Stage Editor use the same code, so a stage tested in the editor gives the terminal's numbers (1-1 to
  1-3 unchanged).
- **Stage Editor** (Puzzle Kingdoms → Stage Editor): stage list (campaign first), numbers, goal and
  enemies, a board painter, *Test difficulty* (100 runs per bot), *New stage* (a copy without the
  tutorial, whose ids must stay unique), *Add to / Remove from campaign*, *Play in editor* (straight into
  the stage; `GameApp.StartStage(StageDefinition)`).
- **The Quarry** (`stage_1_4`) was made with the tool: a copy of 1-2 with the goal *break 5 stones*, four
  painted stones and two random ones. With Rally the bots win 66 % (random) and 82 % (greedy) and miss
  the goal 10 % / 16 % of the time: between 1-2 and 1-3. It is **not in the campaign**: Chapter 1 stays at
  three stages for the playtests; adding it is one click (owner's call).
Why: about 40 stages are planned (D-045); a goal changes how a stage is played without new rules, and a
painted board lets a stage teach one thing on purpose.

## D-049 Go to growth wave 2 before the playtests (owner's call), 2026-11-18
Alican decided to treat PB-6 as **go** without waiting for the PB-5 playtest sessions. No tester has
played the game yet, so the GDD pillars are **not scored** and there is no tester evidence; the only
data is the balance bots (D-026, D-048: every stage winnable, battles 10-40 s, Rally 6-10 s shorter),
which say nothing about whether the loop is fun. **Risk:** weeks of content (wave 2) on a loop nobody
has played. **Kept open:** the playtests still run (kit from PB-5), analytics with consent records real
play (PB-29), and PB-6 gets its pillar scores once five sessions are in; a "pivot" then stops wave 2
where it stands. Wave 2 order: **PB-20** obstacles (code only, Chapter 2 stages use them) → **PB-18**
Chapter 2 (four new enemy models cost Meshy credits; Alican approves each) → **PB-22** kingdom →
**PB-23** commanders → **PB-21** Defender and Support.

## D-050 Chains hold a gem in place, ice is a floor to melt; special combos; the boss drops stones, 2026-11-18
PB-20. The card described ice as a cover that holds the gem; that is what a **chain** now does, so ice
became the other classic obstacle and the two do not overlap.
- **Chain** (on a gem, falls with it): the gem can not be swapped but can be part of a match; the first
  hit breaks the chain and the gem stays (like a stone, D-032). A gem with a chain and a stone loses the
  chain first. No special gem is created on a chained gem.
- **Ice** (on a cell, 1 or 2 layers, never moves): each hit on the cell melts one layer (a cleared gem,
  a cracked stone, a broken chain). New goal **break all the ice** (`StageGoalKind.BreakIce`).
- **Combos:** two special gems swapped together always swap and combine: Blast + Blast 5×5, Prism + Blast
  turns every free gem of the Blast's colour into a Blast (not gems under stone or chain: a Blast left on
  them would outlive the move), Prism + Prism the whole board. `CascadeStep.Combo` tells the view.
- **Boss stones** (`bossStonesEvery`, `bossStonesCount` on the stage): after every N-th move the boss
  drops K stones (not on special gems, not touching, not after the last move); `MoveResult.StonesDropped`
  and a `StonesDropped` replay frame; the puzzle shows "Stones fall in N".
- **Data:** a second row block on the stage, `covers` (`.` `i` `I` `c` `k` `K`), painted in the Stage
  Editor; chains can take away the last move, so random cells are drawn again until a move exists.
- Random boards draw exactly as before (same seeds, same boards; the tests did not change).
- Two **draft Chapter 2 stages** made with the Stage Editor, not in the campaign: *Frozen Gate* (2-1, break
  all the ice, 14 layers, 12 moves; bots win 70 % / 94 %, goal missed 18 % / 7 %) and *The Chief's Forge*
  (2-2, as 1-3 with four chained corners and 2 boss stones every 3 moves; bots win 48 % / 84 %).
Why: Chapter 2 needs boards that play differently from Chapter 1 without new screens; chains and ice
are known to players from other match-3 games, so they need little teaching.

## D-051 Chapter 2: four enemy behaviours as unit data, ten stages, one map per chapter, 2026-11-18
PB-18 (Alican). Chapter 2 is playable end to end except for the four new models.
- **Behaviours are data on `UnitDef`**, all off by default so Chapter 1 is unchanged: `RangedDamageReduction`
  (shield goblin, only while front-most of its side), `HealAmount`/`HealInterval`/`HealRange` (shaman),
  `TargetsBackRow` (wolf rider), `SplashRadius`/`SplashFraction` (bomber). No new unit classes or enums in
  logic; the simulator reads the fields. New `BattleEventType.Heal`; heals land after the hits of a tick.
- **Ranged** = attack range above 1.5 cells (`UnitDef.RangedThreshold`). **Back row** = the formation row
  the unit started in (positions move, rows do not). Splash is armor/ward-aware per victim.
- **Ten stages** `Stage_2_1` to `Stage_2_10`, in the campaign after 1-3 (a star on 1-3 opens 2-1). The two
  D-050 drafts were reworked: Frozen Gate is 2-2, The Chief's Forge 2-9. Each stage has one tutorial prompt.
  Difficulty is tuned only through stage data (HP/damage multipliers, stones, goals) against `Tools/Balance`.
- **`chapter` on the stage asset** (`StageDef.Chapter`, default 1) and a chapter switch on the stage map;
  stage numbers are chapter-local ("2-3"). A chapter button is greyed until its first stage is unlocked.
- **Art:** the four Meshy models are not made (credits need Alican's approval each, D-043): the grey-box
  silhouettes of Brute, Archer and Grunt stand in via four new `UnitLook` values.
Why: data-driven behaviours keep logic free of per-enemy special cases and let PB-21 reuse healing; a
`chapter` field instead of nested campaigns keeps unlock order and the save format unchanged.

## D-052 Kingdom: four buildings with small bonuses, one cost curve, bonuses applied when the battle is set up, 2026-11-18
PB-22 (Alican). Gold now has a use and the game a reason to replay.
- **Buildings are data** (`KingdomRules.asset`, a list): Barracks (Warrior HP), Archer Tower (Archer attack
  speed), Mage Tower (Mage damage), Treasury (stage victory gold). Id, stat, unit class, bonus per level and
  cost factor per building; one shared cost curve (`baseCost`, `costGrowth`) and level cap. PB-21 adds
  Defender and Support buildings by adding entries, not code.
- **Bonuses in one place:** `KingdomBonuses` is read by the `Combatant` constructor (player side only) and by
  `StageReward` (victory gold only). Enemies never get them. Rounding: HP and damage to whole numbers
  (at least 1); attack interval = interval ÷ (1 + bonus).
- **Save version 3:** `buildingIds` and `buildingLevels` on the profile snapshot, older files load with no
  buildings (tested for versions 1 and 2); levels are keyed by building id, never renamed once shipped.
- **Small steps:** +3 % a level (+30 % at level 10) keeps the stages designed against level 0 valid; the
  Balance tool got `--kingdom N` to check them at any level (numbers in GDD §10).
- **Tabs:** `TabBar` along the bottom of the map and the Kingdom; Commanders and Shop are greyed until
  their cards. The map and its stage card sit a little higher to make room.
- **Art:** buildings are drawn in code for now (class silhouette on a round plate, star for the Treasury);
  models are optional later (Meshy).
Why: a long-term sink for gold that stays inside the "no loot boxes, no pay to win" line (D-045) and gives
replays a purpose; data-driven buildings avoid per-class code before PB-21 adds classes.

## D-053 Commanders change what Rally does; unlocked by stages, chosen once, saved in the profile, 2026-11-18
PB-23 (Alican). The same stage plays differently with a different commander.
- **Rally variants are data** (`RallyVariant` on the battle, built from `CommanderDefinition` assets): Classic
  (the old Rally, from `BattleRules`, also for a player without a commander), Shield Wall, Arrow Rain, Fireball.
  The simulator reads the fields; there is no class per variant. New `BattleEventType.RallyStrike` for
  hits with no attacker.
- **Deterministic:** no randomness anywhere; "back row" is the back-most spawn row still alive, the Fireball
  centre is the enemy with most enemies within the radius (ties: lower id). The Rally input log still replays a
  battle exactly (tested for every variant).
- **Strikes** take a share of max HP (at least 1) and ignore armor and ward: they must not be wasted on
  armored units or made useless by bosses with big HP pools; the share stays small (8 % a volley, 18 % a blast).
- **Unlock and choice:** a commander is unlocked once its unlock stage has a star (nothing extra to save);
  the profile stores only the chosen commander id (save version 4; older saves choose none). A chosen
  commander that is no longer in the data falls back to the classic Rally. The results screen says when a
  stage unlocked one.
- **Balance:** no variant may be a must-have: each stays within about 8 points of the classic Rally on the
  Chapter 2 stages (GDD §9 table); the first Fireball was cut after measuring.
- **Art:** the commander crest is drawn in code (a colour and a shape); the three Meshy commander models of
  the card are not made yet (credits need Alican's go, D-043). In 3D each variant has its own burst colours
  and the banner and glow take the variant's colour; a dedicated particle effect per variant is left for the art pass.
Why: Rally is the signature move; variants give replay value without a loot box (GDD pillar 2).

## D-054 One UI look for every screen: gradient backdrop, rounded shadowed cards, glossy buttons, 2026-11-18
Alican found the flat grey-box screens plain. All of it is code, no art files (D-025 still holds).
- `Ui.Backdrop` (deep blue gradient with three soft glows) replaces the flat background on every screen;
  `Ui.Card` (rounded 9-slice with a drop shadow) is the panel, box, bar and card; `Ui.Button` is rounded with a
  shadow and a soft gloss; big text (44 and up) has a soft shadow. Sprites are generated once in `UiStyle`.
- The tab bar got an icon per tab. Long chapters (more than 5 stages) show no name under the map nodes
  (the stage card has it), their nodes sit on a snake of four rows and shrink on a short map; locked numbers
  stay readable. Results stats sit on a card.
- The palette is unchanged, so the contrast tests (D-044) still hold. Screens are checked with
  `GreyboxScreenshots` (`Logs/Screenshots/`), which now also captures Chapter 2, the Kingdom and the Commanders.

## D-055 The battlefield is a place: painted ground, trees, fence, banners, 2026-11-18
Alican found the flat green battlefield childish. `BattleScenery` (Views/Battle3D) builds it from code: a
procedural ground texture (mown grass, a trampled dirt arena with a muddy rim, pebbles, shade towards the
edges), chalk marks where the armies meet, a fence along both sides, trees, bushes and rocks at the sides and the
far end, a banner at each corner (blue for the player, olive for the goblins), and a soft sun with shadows.
Low props only at the player's end, so nothing stands in front of the player's units. No art files; the same
every battle (fixed random seed). Tuned by looking at `GreyboxScreenshots.CaptureChapterTwoBattle`.

## D-056 - Ambient and procedural motion (2026-11-18)

The battlefield and the unrigged goblin models felt lifeless. Trees and bushes sway and banner cloth flutters
(`SceneryMotion`, play mode only). Models without an Animator breathe, bob and lean while walking and lunge when
they strike (`UnitActor.AnimateByCode`). Purely visual, nothing in Logic changes. Rigged animation for the four
Chapter 2 goblins stays an art item (Meshy rig).

## D-057 - Painted art pass for menus and battlefield (2026-11-18)

Building pictures, chapter map paintings (Meshy text-to-image) and commander portraits plus battle ground
paintings (ChatGPT image generation, supplied by the team) replace the procedural look where a file exists
(`ArtSprites`, `Resources/Art/UI`). The battle ground picture replaces the procedural ground, trees and fence;
nothing else is added. The fort-gate picture is used for stage 2-10. Missing files fall
back to the procedural look. Tab bar and commander emblems are drawn from simple shapes (`TabIcons`).

## D-058 - Liveliness pass (2026-11-18)

Battle: scenery sways (D-056), a camera shake on enemy deaths and Fireball (strength and length per call), impact
sparks on every hit, a pop of sparks and two coins when an enemy dies. UI: every button squashes while pressed and
springs back (`PressFeel`), the victory screen pops its title and stars in and throws confetti (`UiJuice`).
PlayMode tests run with the master volume at zero (`SilentTests`), so they stay quiet; `Sfx` still counts plays.

## D-059 - Defender and Support, per-stage class sets (2026-11-18)

Two player classes: Defender (900 HP, armour 30 %, enemies within 4 cells attack it first; the wolf rider ignores
the taunt) and Support (heals the most hurt ally 60 HP every 2 s). A stage chooses its own class set (at most 4,
plus the Banner gem), stored on the stage asset; empty means Warrior, Archer, Mage. The Defender unlocks with the
first win of stage 1-3, the Support with stage 2-5, and stages only use a class after its unlock stage.
A fifth gem type cuts accidental chains, so recruits fell by about 60 % at equal moves: Chapter 2 moves were raised
to 17-32 and checked with `Tools/Balance` (see the PB-21 card). Models and 3D effects stay an art item.

## D-060 - Quests, achievements and star chests (2026-11-19)

Logic (`QuestBoard`) has no clock: the game passes the date as text. The day's quests are drawn from a seed made of
the date, so a restart shows the same ones and a day change redraws them with zero progress. Quests count what a
finished `StageAttempt` did (`ToQuestEvent`); progress is capped at the target. Achievements keep their progress
across days and a new one added to the pool appears in old saves. A star chest is earned per 30 stars (sum of the best
stars per stage) and paid on tap; rewards are gold only (no power for sale, PB-30 decides the rest). Save version 5
adds the quest state; older saves load with none. The Quests tab is the fifth tab with a count badge.

## D-061 - Daily Challenge (2026-11-19)

One challenge a day, the same for everyone: `DailyChallenge.For(date, rules)` builds a `StageDef` (enemy army from a
pool, formation cells, tiers, moves, stones, an optional break-stones goal and a fixed board seed) from a seed made of
the date text, so every device gets the same stage. The setup lives in `Data/DailyChallenge.asset`. The attempt uses no
commander and no building bonuses, so results compare. One attempt a day: the result is saved when the battle ends,
not when it starts, and a second start the same day does nothing. A result is won/lost, then the share of the army's
HP left, then the time (`DailyResult.IsBetterThan`). The last 30 days are kept in the profile (save version 6); the
daily day is not a campaign stage, so it pays its gold but gives no stars and no star-chest progress. The entry is a
gold button above the stage map. Online ranking waits for the backend decision (PB-25 step 4); the saved result and
the fixed seed are what a server needs to re-run and verify a battle.

## D-062 - Expedition mode (2026-11-19)

A run of 5 battles (the last is a boss) where the army and its wounds carry over. `Expedition` (Logic) holds only what
lasts between battles: the step, the army (class, tier, `ArmyUnit.HpFraction`), the offers and a Rally bonus. Each battle
is an ordinary `StageRun`: a short puzzle (12 moves, then 8) adds recruits to the carried army, the War Camp deploys,
the battle plays. Survivors and the bench go on and regain 40 % HP; the dead are gone; merging gives a fresh unit. After
a win the player picks one of three rewards (recruit 3 of a class, heal everyone, promote the weakest unit, one extra
Rally); a loss ends the run, and gold (60 per battle, 250 for winning) is paid when the run is collected or given up.
The same seed gives the same enemies, boards and offers. The player's buildings and commander are not used, as in the
daily challenge. The run is saved after every battle and every pick (save version 7) and resumes from the map button.
Numbers: `Data/ExpeditionRules.asset`. A simple bot that never merges clears 3.9 of 5 battles and wins 2 of 20 runs;
a player who merges should do clearly better. Meta rewards beyond gold and leaderboards stay out of this card.

## D-063 - Battle replays (2026-11-19)

A battle is saved as a small text (`BattleReplay`, format version 1): both armies at the first tick (class, tier, cell,
wounds), the stage's enemy strength, the Rally variant and the building bonuses, and the ticks where Rally was
pressed. The simulator is deterministic, so playing it again from that text gives the same battle tick for tick (tested:
same ticks, same HP of every unit, same Rally ticks). A `Stamp`, a hash of the battle and army rules and of the numbers of
every unit in the battle, tells whether the game still plays the battle as it was fought; a replay with an old stamp is
listed as "Older version" and cannot be watched (it would play out differently). The profile keeps the last 5
battles and up to 10 starred ones (save version 8). The replay player is the normal battle screen with the simulator
driven by the replay: no Rally button, a timeline with Rally marks, 1x/2x/4x, Restart, and a tap on the timeline
starts a new screen at that tick (the simulator is advanced without drawing). The list opens from Settings → Replays.
Sharing a video clip is split off: it needs a recorder plugin and a phone, to be chosen and added to CREDITS.md first.

## D-064 - Business model: free with rewarded ads and a Supporter pack (2026-11-19)

The game is free. Money comes only from ads the player chooses to watch and from one optional purchase; power is never
sold (pillar 2): no ad and no purchase adds strength, units or stars. There are no forced ads (no interstitials, no banners).
Rewarded ads give a little time or gold: +5 puzzle moves when the moves run out (once per attempt), one extra Rally
charge in the War Camp (once per attempt), and 100 gold in the Shop (3 times a day). They are offered only in the
campaign; the daily challenge and the expedition stay comparable and balanced without them. The Supporter pack
(about $2.99, one purchase) grants every one of these rewards without watching an ad; it is the "ad-free" product, since there
are no forced ads to remove. Crowns, a season pass and the second currency of the card are left out for now; the plan
is to add them in their own card if this part earns its place. Code: `IAds` and `IBilling` interfaces (`NoAds`/`NoBilling`
in release builds until the SDKs are added, `TestAds`/`TestBilling` in the editor and dev builds), rewards in
`StageRun`/`AdLedger`, numbers in `Data/Monetization.asset`, save version 9. The real ad SDK and Google Play Billing, the
store products and a consent check (PB-29) before personalised ads are the owner's next steps.

## D-065 - Cloud save: conflict rule and the interface (2026-11-19)

Sign-in is optional; playing without it works as before. When this phone and the cloud both hold a save,
`SaveConflict.Resolve` decides: progress first, then time. Progress is what cannot be bought back (best stars per stage,
building levels, the expedition record, achievements and chests claimed, the commander, the Supporter pack, daily results);
gold is not. A save that is ahead somewhere and behind nowhere wins. Saves with the same progress go by the time they were
written. Saves that are each ahead somewhere make the player choose in a dialog that shows stars, stages and gold of both;
nothing changes until they pick. A cloud save from a newer game version is left alone. The game uploads after saves (at most
every 20 seconds, and when it is paused or closed) and when the player resets progress, so a reset is not undone by the old
cloud copy. The cloud is behind `ICloudSave`; releases ship `NoCloud` (the Settings row stays hidden) until the Google Play
Games plugin and a Play Console app exist (owner), the editor and dev builds use `TestCloud`. Settings gained a Cloud save
row; its rows were re-spaced to fit.

## D-066 - Audio method: themes, cross-fade, import rules, free sources (2026-11-19)

Music has a theme per kind of screen (map and menus, puzzle and War Camp, battle, results) in the `SoundBank`; an empty
theme falls back to the default loop, so the game works with one track or five. Changing screens cross-fades with two
music players (1.2 s), so at most two streams play. Files in `Audio/Music` import as streamed stereo Vorbis, files in
`Audio/Sfx` as mono Vorbis decoded on load (`AudioImportRules`), which is the cheap setup on a phone; the whole audio
folder stays under 20 MB. Sources, in order of preference: CC0 packs (Kenney) for the effects, free music with a clear
commercial licence (Pixabay), and AI generated with Stable Audio 3.0 open weights run on our own computer (Stability AI
Community Licence: outputs are ours, free while the organisation earns under $1M a year; the licence text of the
downloaded version is to be read before use). Free tiers of AI music websites are not used for the store build: they
are non-commercial. Every file is in CREDITS.md before it is committed, with tool, model and prompt for AI files.
The method is in `docs/AUDIO.md`. The real files are still to be made or downloaded (owner).

## D-067 - A save from a newer version is locked, not replaced (2026-11-20)

Installing an older build over a newer one (a tester's old APK, a store rollback) used to set the newer save aside as
`profile.json.bak`, start a fresh profile and then save that over `profile.json`; updating again found only the fresh
profile. Now `ProfileStore` still copies the file to `.bak`, but marks itself `Locked`: the game plays on a fresh
profile and writes nothing, to the phone or to the cloud (no upload, no cloud sync), and a "Update the game" notice
says so at start. Updating brings the save back as it was. Resetting progress does nothing while locked; that is
rare enough to leave. Backups no longer replace each other: a second bad file goes to `.bak2`, `.bak3` ...; the same
file seen again at the next start is not copied twice. Older save versions keep loading: tests read save files with
only the version 1, 2 and 4 fields, and a snapshot with every later field missing (`SaveVersionTests`,
`SaveFileTests`). Why: a tester who installs an old APK must not lose their progress (PB-15, package A of the
2026-11-20 review).

## D-068 - Merged units are worth more than their parts: tiers ×1 / ×4 / ×13 (2026-11-21)

With ×1.8 for tier 2 and ×3.2 for tier 3, three units became one that had less HP and damage than the three
together, and with 16 slots and 10-21 recruits space never runs out. The balance bots showed it once they could
merge (`Tools/Balance --merge all`): "Merge all" cut stage wins by 30-60 points, while the 1-2 tutorial and the
quests ask the player to merge. Now a tier 2 is ×4 and a tier 3 ×13: each tier beats the three units it is made of
in raw numbers and in a straight fight (`MergeValueTests`). No-merge play is unchanged (it never meets a tier above
1), so this only rewards merging. Greedy bot, no Rally, 200 runs:

| Stage | No merge | Merge all, old ×1.8/×3.2 | Merge all, ×4/×13 |
|---|---|---|---|
| 1-3 | 86% | 55% | 92% |
| 2-1 | 88% | 64% | 91% |
| 2-3 | 75% | 14% | 87% |
| 2-4 | 81% | 31% | 93% |
| 2-5 | 72% | 26% | 86% |
| 2-6 | 70% | 18% | 77% |
| 2-7 | 76% | 30% | 77% |
| 2-8 | 59% | 73% | 100% |
| 2-9 | 63% | 27% | 88% |
| 2-10 | 47% | 9% | 76% |

Enemy units keep ×1 / ×1.8 / ×3.2 (`ArmyRules.EnemyTierMultipliers`): they are not made by merging, and the
Expedition and Daily Challenge, which field tier 2 and 3 enemies, were tuned with that curve. Merged armies also win faster (2-10: 49 s to 41 s). 2-8 becomes too easy with merging; stage difficulty, battle
length and Rally weight are PB-35. `--tiers 1,4,13` lets the balance tool try other multipliers without editing
the asset. Alican reviews the branch (rules and data are his area).

## D-069 - Battle pace, the time-out rule, stage tuning and the defeat hint (2026-11-21)

Chapter 2 wins took 38-49 s (target 20-40 s) and a battle still running at 60 s was a defeat, however far ahead the
player was. Now:
- **Pace:** every hit and heal of both sides is multiplied by `BattleRules.DamageScale` (1.6 in the asset). One knob
  keeps the matchups as they were and only shortens battles. Rally strikes (a share of max HP) are not scaled.
- **Time-out:** at 60 s the side with the larger share of its HP left wins; a tie is a defeat; a win on time is one
  star (`BattleResult.Stars`).
- **Stages** (tuned with the merge-all bot, since merging is now the taught play, D-068): 2-2 has 26 moves (the ice
  goal was missed 15% by bots that do not aim at ice; 8% now, a human who aims does better); enemy HP and damage
  re-set on 2-3 to 2-10 so the merge-all bot goes from about 95% down to 69% at 2-10; 2-8 is "win within 20 s" with
  enemy HP x1.9, so it is no longer decided by Rally (82% without Rally for a player who does not merge, 100% for
  one who does). Players who never merge find 2-9 and 2-10 hard (33% and 24%): that is the lesson those stages teach.
- **Defeat hint:** `BattleResult.Reason` (time up, Rally unused, outnumbered, overpowered) gives one line on the
  results screen.

Greedy bot, no Rally, 200 runs; "before" is the table of the 2026-11-21 review:

| Stage | Before: no merge | Now: no merge | Now: merge all | Win time, merge all |
|---|---|---|---|---|
| 1-3 | 86% | 88% | 93% | 19 s |
| 2-1 | 88% | 96% | 97% | 26 s |
| 2-2 | 86% | 92% | 92% | 14 s |
| 2-3 | 75% | 82% | 95% | 33 s |
| 2-4 | 81% | 84% | 93% | 26 s |
| 2-5 | 72% | 70% | 86% | 28 s |
| 2-6 | 70% | 80% | 88% | 30 s |
| 2-7 | 76% | 87% | 84% | 34 s |
| 2-8 | 59% | 82% | 100% | 13 s |
| 2-9 | 63% | 33% | 83% | 35 s |
| 2-10 | 47% | 24% | 69% | 38 s |

`Tools/Balance` gained `--damage`, `--only` and `--set stage_2_3.hp=1.6` (hp, damage, moves, goal) to try values
without editing assets. Alican reviews the branch (rules and data are his area).
