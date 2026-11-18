# Roadmap

Estimates assume two developers working full time; double them for part time.

| # | Milestone | Est. | Exit criteria |
|---|---|---|---|
| M0 | Foundation | 1 wk | Docs, Unity project, test runner, **empty APK running on a real Android phone** |
| M1 | Grey-box prototype (2D, no art) | 2–3 wk | Full loop playable with shapes: puzzle → camp → battle (2D debug view) → result. Playtested by 5+ people. **Go / pivot decision.** |
| M2 | Puzzle polish | 2 wk | Touch input, gem animations, cascade feedback, hint, reshuffle animation |
| M3 | War Camp | 2 wk | Drag & drop formation, bench, merge UX, enemy preview |
| M4 | 3D battle presentation | 3 wk | Isometric scene on top of the same simulator, our Meshy models and animations (D-043), Rally VFX |
| M5 | Loop integration | 2 wk | 3 stages, stars, gold, local save, results screen |
| M6 | Android & polish | 2 wk | Performance profile on target device, UI polish, playtest round |
| M7 | Content & meta (growth wave 2) | 5–6 wk | Chapter 2, obstacles, Defender/Support, kingdom, commanders (D-045; after the go decision) |
| M8 | Retention & soft launch (wave 3) | 5–6 wk | Quests, Daily Challenge, Expedition, economy, cloud save, real audio; ~40 stages; soft launch |
| M9 | Live modes (wave 4) | 3 wk | Async PvP verified by re-running battles, replays and sharing |

Art direction research (packs vs. freelancer) runs in parallel from M3.

## Current status

Engine switched from Godot to Unity on 2026-11-13 (D-018); all logic was
ported to C# with its tests.
`[x]` done · `[~]` written but unverified · `[ ]` to do

- [x] M0: design decisions, docs
- [x] M0: logic test runner (`dotnet test Tools/LogicTests`)
- [x] M0: Unity 6 LTS project created (6000.3.28f1, URP, portrait); 91 EditMode tests pass in Unity
- [ ] M0: Android build verified on a device (see SETUP.md §4). The dev APK
      builds (2026-11-14, 38 MB, IL2CPP ARM64); still to do: install and play on a phone
- [x] M1: puzzle logic (`BoardModel`, `RecruitmentMeter`, `PuzzleSession`) + tests
- [x] M1: unit/enemy/stage data assets (ScriptableObjects, verified by Data.Tests in Unity)
- [x] M1: puzzle grey-box view (tap/swipe, animated cascades, enemy preview, meters, idle hint)
- [x] M1: war camp logic (`ArmyUnit`, `Formation`, `Roster`, `MergeRules`) + tests
- [x] M1: war camp grey-box view (deploy/move/bench, merge, auto, fight)
- [x] M1: battle simulator + Rally + tests
- [x] M1: battle 2D debug view (Rally, 1x/2x, pause, skip, damage numbers)
- [x] M1: first balance pass with `Tools/Balance` (D-026); revisit after playtests
- [x] M1: stage flow logic (`StageRun`, `AutoFormation`) + progression (`StageReward`, `PlayerProfile`) + tests
- [x] M1: stage flow (GameApp, stage map, results, JSON save) + PlayMode smoke test
- [ ] M1: playtest with 5+ people, go/pivot decision
