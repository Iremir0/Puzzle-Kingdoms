# Team Board

Two developers: **Alican** and **Emir**. This file is the
single task board; the long-term plan stays in [ROADMAP.md](ROADMAP.md).

## Working rules

- One person owns a task at a time. To take a task, move it to *In Progress*,
  put your letter in the Owner column and push that change first.
- Max **2 tasks In Progress per person**. Finish before starting.
- One task = one branch off the latest `main`, named with the task ID
  (`feature/M1-03-playtest-kit`), commits only, no pull requests; the other
  person reviews by reading the branch, then it is merged back into `main`
  once the tests pass (D-034).
- A task is *Done* only when: logic has NUnit tests (`dotnet test Tools/LogicTests`
  green), docs/DECISIONS.md is updated if a rule changed, and the task was
  played once in Unity.
- Conflicts: scenes and prefabs are binary-ish. Only one person edits
  `Main.unity` at a time; say so in the task's Notes.
- Sync: 10-minute check-in at the start of each work session, move cards then.

Task ID prefix = milestone (`M1-03`). Size: S ≤ ½ day, M ≈ 1–2 days, L ≈ 3–5 days.

---

## In Progress

| ID | Task | Owner | Size | Branch | Notes |
|---|---|---|---|---|---|
| M0-01 | Verify Android build on a real phone (SETUP.md §4) | Emir | M | — | Dev APK builds (38 MB, IL2CPP ARM64, 2026-11-14); still to do: install and play on a phone. Blocks M6; it always hides surprises |

## Ready (next up, in priority order)

| ID | Task | Owner | Size | Notes |
|---|---|---|---|---|
| M1-08 | Play 1-1 to 1-3 once in Unity on `main` and check the In Review notes below | Emir | S | Merging into `main` and the Test Runner are done (2026-11-14, see In Review). Moves M1-06, M1-07, M1-01, M1-02 to Done |
| M0-02 | CI: run `dotnet test Tools/LogicTests` on GitHub for every push | — | S | Guards the "merge when tests pass" rule; Unity tests stay local (they need a license) |
| M1-03 | Playtest kit: build, 5+ testers, one-page feedback form | — | S | Needs M0-01 for phone testing |
| M1-09 | Show a stage's difficulty on the stage select and the enemy preview (stone gems, enemy HP ×) | — | S | View only; data is in `StageDef` (`StoneGems`, `EnemyHpMultiplier`). Suggested owner: Emir |
| M1-10 | War Camp: draw unit silhouettes (`Palette.LookFor` + `UnitSilhouettes`) instead of letters in the formation grid and the bench | — | S | View only. Suggested owner: Emir |
| M1-04 | Run playtests, collect results into `docs/PLAYTESTS.md` | — | M | Exit of M1 |
| M1-05 | Go / pivot decision, recorded in DECISIONS.md | both | S | After M1-04 |

## Backlog

### M2 Puzzle polish (2 wk)
| ID | Task | Size |
|---|---|---|
| M2-01 | Touch input feel: swipe threshold, drag-follow, invalid-swap bounce | M |
| M2-02 | Gem animations: swap, clear, fall, spawn (tweens, no logic changes) | M |
| M2-03 | Cascade feedback: rising pitch SFX, screen shake on Prism ("Bonus! xN" text already shows) | M |
| M2-04 | Special and stone gem effects: Blast ring, Prism sparkle and half-stone gems are drawn (D-028, D-032); still to do: clear / explosion / stone-break effects and sound | M |
| M2-05 | Reshuffle animation (board snaps today) and hint polish (5 s idle hint exists) | S |
| M2-06 | Placeholder SFX/music pass (log in CREDITS.md first) | S |

### M3 War Camp (2 wk)
| ID | Task | Size |
|---|---|---|
| M3-01 | Drag & drop onto the 4×4 formation (replaces tap-select) | L |
| M3-02 | Bench UI: scroll, grouping by class/tier, mergeable highlight | M |
| M3-03 | Merge UX: merge animation, undo before fight (preview done in M1-06) | M |
| M3-05 | Art direction research (packs vs. freelancer), runs in parallel | M |

### M4 3D battle presentation (3 wk)
| ID | Task | Size |
|---|---|---|
| M4-01 | Isometric battlefield scene fed by the existing simulator | L |
| M4-02 | Placeholder 3D models per class and tier scaling | M |
| M4-03 | Mixamo animations: idle, walk, attack, hit, death | L |
| M4-04 | Rally VFX and camera shake; hit/damage feedback | M |
| M4-05 | Battle camera, 1×/2× speed, pause in 3D | S |

### M5 Loop integration (2 wk)
| ID | Task | Size |
|---|---|---|
| M5-01 | Stage map polish: a real map instead of the list (3 stages and the list exist) | M |
| M5-03 | Tests for `ProfileStore`: corrupt file kept as `.bak`, newer save version, atomic write (the code exists, untested) | S |
| M5-04 | Tutorial prompts for stages 1-1, 1-2, 1-3 | M |
| M5-05 | Settings: sound, speed default, reset progress | S |

### M6 Android & polish (2 wk)
| ID | Task | Size |
|---|---|---|
| M6-01 | Performance profile on target device, fix top 3 hotspots | L |
| M6-02 | Safe areas, aspect ratios, small-screen layout pass | M |
| M6-03 | UI polish pass (fonts, spacing, colours from GDD) | M |
| M6-04 | Final playtest round and balance pass with `Tools/Balance` | M |
| M6-05 | App icon, splash, release keystore, signed AAB (package name is set) | S |

### Post-MVP ideas (do not start before M1 go decision)
Class levels with gold, Defender/Support classes, Daily Challenge, Expedition
mode, Commander heroes (see GDD §10).

---

## In Review

Merged into `main` on 2026-11-14 (Emir, via `feature/enemy-scaling`, which contains all four); `dotnet test` and the Unity Test Runner were run on the result. Each card still needs one play-through in Unity (M1-08) before it moves to Done.

| ID | Task | Owner | Reviewer | Branch |
|---|---|---|---|---|
| M1-06 | Merge all button and merge preview (D-030) | Alican | Emir | `feature/merge-all` |
| M1-07 | 4×4 formation, enemy armies rebuilt for it (D-031) | Alican | Emir | `feature/formation-4x4` |
| M1-01 | Stone gems: first match only breaks the stone (D-032) | Alican | Emir | `feature/stone-gems` |
| M1-02 | Rising enemy strength per stage from stage data; unit HP ×1.4 (D-033) | Alican | Emir | `feature/enemy-scaling` |

Notes for the reviewer:
- **M1-06**: played in Unity, works. **M1-07**: not played yet: check the 4×4 grid fits the War Camp and the battle field (cells are 25% smaller).
- **M1-01**: check the half-stone gem look on the puzzle board and the "Stone cracked!" label; stones are on stages 1-2 (3) and 1-3 (5).
- **M1-02**: `Tools/Balance` numbers are in D-033. 1-3 with the random bot is under the D-026 target (42–54%); real players should do better, check in the playtests (M1-04). Battle length 16–41 s without Rally, 6–10 s shorter with it.
- All four: 130 EditMode tests pass with `dotnet test Tools/LogicTests`; the Data tests (Unity only) changed (HP, stage counts, multipliers, stones) and have not been run yet. Unity will create `.meta` files for `MergeAllTests`, `StoneGemTests`, `EnemyScalingTests`; commit them with the branch.

## Done

| ID | Task |
|---|---|
| M0-D1 | Design decisions, docs, test runner, Unity 6 project |
| M1-D1 | Puzzle logic, special matches, data assets, grey-box views |
| M1-D2 | War camp logic and view |
| M1-D3 | Battle simulator, Rally, 2D debug view |
| M1-D4 | Stage flow, progression, JSON save, PlayMode smoke test |
| M1-D5 | Balance pass with `Tools/Balance` (D-026) |
| M1-D6 | Gem shapes, special matches (Blast, Prism), code-drawn unit silhouettes, pointsPerUnit 8 (D-027, D-028, D-029) |
| M1-D7 | Stars and gold on the results screen, star and save-version rules tested (`ProgressionTests`) |
| M1-D8 | Enemy army preview in the War Camp and on the puzzle screen |
| M0-D2 | Dev APK build command and Android toolchain; `main` brought up to date, ticket branch workflow (D-034) |

---

## Suggested split (so you rarely touch the same files)

- **Alican — rules and data:** `Scripts/Logic/`, `Data/`, tests, `Tools/Balance`.
  Owns M1-01, M1-02, M5-03, balance work.
- **Emir — Unity and presentation:** `Scripts/Views/`, scenes, prefabs, Android,
  animation and VFX. Owns M0-01, M2, M3, M4.
- Anything that crosses the line (a new rule that needs a new view) is agreed
  in the check-in: Alican ships the logic + tests first, Emir builds the view on top.
