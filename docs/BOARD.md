# Team Board

Two developers: **Alican** and **Emir**. This file is the single task board;
the long-term plan stays in [ROADMAP.md](ROADMAP.md).

## Working rules

- Cards are called **PB-1, PB-2, ...** (Puzzle Board). The number never changes
  and is never reused. The *Milestone* column says where a card belongs in the
  ROADMAP.
- One person owns a card at a time. To take a card, move it to *In Progress*,
  put your name in the Owner column and push that change first.
- Max **2 cards In Progress per person**. Finish before starting.
- One card = one branch off the latest `main`, named with the card:
  `feature/PB-3-war-camp-visuals` (fixes: `fix/PB-3-...`). Commits only, no
  pull requests; the other person reviews by reading the branch, then it is
  merged back into `main` once the tests pass (D-034, D-035).
- A card is *Done* only when every box of its "Done when" list is ticked:
  logic has NUnit tests (`dotnet test Tools/LogicTests` green),
  docs/DECISIONS.md is updated if a rule changed, and the result was played
  once in Unity.
- Conflicts: scenes and prefabs are binary-ish. Only one person edits
  `Main.unity` at a time; say so in the card's Notes.
- Sync: 10-minute check-in at the start of each work session, move cards then.

Size: S ≤ 1 day, M ≈ 2-3 days, L ≈ 1 week. Cards are deliberately big: one card
is a piece of the game you can play or show, not a single code change.

---

## In Progress

### PB-1 Run the game on a real phone  ·  Emir  ·  M0  ·  M
Branch: —  ·  Replaces: M0-01

The dev APK builds (38 MB, IL2CPP ARM64, 2026-11-14). Install it on an Android
phone and play 1-1 to 1-3 on it. It blocks the release work (PB-17) and always
hides surprises (touch size, speed, aspect ratio).

Done when:
- [ ] APK installed on a phone and a stage was won on it
- [ ] Problems found are written down as new cards (or fixed if tiny)
- [ ] SETUP.md §4 matches what you actually had to do

---

## Ready (next up, in priority order)

### PB-2 Play the new rules in Unity and close the review  ·  — ·  M1  ·  S
Replaces: M1-08, M1-06 (Merge all), M1-07 (4×4), M1-01 (stone gems), M1-02 (enemy scaling)

All four features are already merged into `main` (2026-11-14) and `dotnet test`
plus the Unity Test Runner were run. What is missing is one human play-through.

Done when:
- [ ] Unity Test Runner (EditMode + Data tests) green on `main`; the Data tests
      changed (HP, stage counts, multipliers, stones) and were never run
- [ ] 1-1, 1-2, 1-3 played once each; the 4×4 grid fits the War Camp and the
      battle field (cells are 25% smaller)
- [ ] Stone gems look right (half-stone gem, "Stone cracked!" label); stones are
      on 1-2 (3) and 1-3 (5)
- [ ] Merge all works (already played once by Alican)
- [ ] Anything wrong is fixed or turned into a card; then this card is Done

Notes: numbers for stage strength are in D-033. 1-3 with the random bot is under
the D-026 target (42-54%); real players should do better, the playtests (PB-5)
decide.

### PB-3 War Camp and stage select show what the game knows  ·  — ·  M1  ·  S
Replaces: M1-09, M1-10 · View only, suggested owner: Emir

Today the War Camp shows letters and the stage list hides the difficulty. The
data and the art already exist; this card only connects them.

Done when:
- [ ] The stage select and the enemy preview show the stage's stone gems and
      enemy HP multiplier (data: `StageDef.StoneGems`, `EnemyHpMultiplier`)
- [ ] The War Camp formation grid and the bench draw unit silhouettes
      (`Palette.LookFor` + `UnitSilhouettes`) instead of letters
- [ ] Played once in Unity; no logic changed

### PB-4 CI: tests run on GitHub for every push  ·  — ·  M0  ·  S
Replaces: M0-02

Guards the "merge when tests pass" rule. Only `dotnet test Tools/LogicTests` runs
there; Unity tests stay local (they need a license).

Done when:
- [ ] A GitHub Actions workflow runs the logic tests on every push and shows
      green/red on the branch
- [ ] A deliberately broken test makes it red (checked once, then reverted)
- [ ] SETUP.md or CONVENTIONS.md mentions it in one line

### PB-5 Playtest round  ·  — ·  M1  ·  M
Replaces: M1-03, M1-04 · Needs PB-1 for phone testing

Done when:
- [ ] A build testers can install, and a one-page feedback form
- [ ] 5+ testers played; results written in `docs/PLAYTESTS.md`
- [ ] Top problems turned into cards

### PB-6 Go / pivot decision  ·  both  ·  M1  ·  S
Replaces: M1-05 · After PB-5

Done when:
- [ ] The decision and its reasons are recorded in DECISIONS.md

---

## Backlog

Not started; do not split these until they are next. Each card is a milestone
slice that ends in something playable.

| ID | Card | Milestone | Size | Replaces |
|---|---|---|---|---|
| PB-7 | Puzzle feel: swipe threshold and drag-follow, invalid-swap bounce, swap/clear/fall/spawn animations, reshuffle animation, hint polish | M2 | L | M2-01, M2-02, M2-05 |
| PB-8 | Puzzle feedback: cascade SFX with rising pitch, screen shake on Prism, clear / explosion / stone-break effects, placeholder SFX and music (log in CREDITS.md first) | M2 | M | M2-03, M2-04, M2-06 |
| PB-9 | War Camp interaction: drag and drop onto the 4×4 formation, scrollable and grouped bench, merge animation and undo before the fight | M3 | L | M3-01, M3-02, M3-03 |
| PB-10 | Art direction decision: asset packs vs. freelancer, with samples and a recommendation (runs in parallel) | M3 | M | M3-05 |
| PB-11 | 3D battle scene: isometric battlefield fed by the existing simulator, placeholder models per class and tier, camera, 1×/2× speed, pause | M4 | L | M4-01, M4-02, M4-05 |
| PB-12 | Battle life: Mixamo animations (idle, walk, attack, hit, death), Rally VFX, camera shake, hit/damage feedback | M4 | L | M4-03, M4-04 |
| PB-13 | Stage map and tutorial: a real map instead of the list, tutorial prompts for 1-1, 1-2, 1-3 | M5 | M | M5-01, M5-04 |
| PB-14 | Settings and save safety: sound, default speed, reset progress; `ProfileStore` tests (corrupt file kept as `.bak`, newer save version, atomic write) | M5 | M | M5-03, M5-05 |
| PB-15 | Phone performance and layout: profile on the target device and fix the top 3 hotspots, safe areas, aspect ratios, small-screen pass | M6 | L | M6-01, M6-02 |
| PB-16 | Final polish: fonts, spacing and colours from the GDD, last playtest round, balance pass with `Tools/Balance` | M6 | M | M6-03, M6-04 |
| PB-17 | Release build: app icon, splash, release keystore, signed AAB (package name is set) | M6 | S | M6-05 |

### Post-MVP ideas (do not start before the go decision, PB-6)
Class levels with gold, Defender/Support classes, Daily Challenge, Expedition
mode, Commander heroes (see GDD §10).

---

## Done

| ID | Task |
|---|---|
| M0-D1 | Design decisions, docs, test runner, Unity 6 project |
| M0-D2 | Dev APK build command and Android toolchain; `main` brought up to date, ticket branch workflow (D-034) |
| M1-D1 | Puzzle logic, special matches, data assets, grey-box views |
| M1-D2 | War camp logic and view |
| M1-D3 | Battle simulator, Rally, 2D debug view |
| M1-D4 | Stage flow, progression, JSON save, PlayMode smoke test |
| M1-D5 | Balance pass with `Tools/Balance` (D-026) |
| M1-D6 | Gem shapes, special matches (Blast, Prism), code-drawn unit silhouettes, pointsPerUnit 8 (D-027, D-028, D-029) |
| M1-D7 | Stars and gold on the results screen, star and save-version rules tested (`ProgressionTests`) |
| M1-D8 | Enemy army preview in the War Camp and on the puzzle screen |
| M1-D9 | Merge all and merge preview, 4×4 formation, stone gems, rising enemy strength (D-030 to D-033); waits for the Unity play-through in PB-2 |

---

## Suggested split (so you rarely touch the same files)

- **Alican — rules and data:** `Scripts/Logic/`, `Data/`, tests, `Tools/Balance`
  (PB-14 save tests, balance work in PB-16).
- **Emir — Unity and presentation:** `Scripts/Views/`, scenes, prefabs, Android,
  animation and VFX (PB-1, PB-3, PB-7 to PB-9, PB-11, PB-12).
- Anything that crosses the line (a new rule that needs a new view) is agreed
  in the check-in: Alican ships the logic + tests first, Emir builds the view on top.
