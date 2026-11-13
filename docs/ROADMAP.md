# Roadmap

Estimates assume two developers working full time; double them for part time.

| # | Milestone | Est. | Exit criteria |
|---|---|---|---|
| M0 | Foundation | 1 wk | Docs, Unity project, test runner, **empty APK running on a real Android phone** |
| M1 | Grey-box prototype (2D, no art) | 2–3 wk | Full loop playable with shapes: puzzle → camp → battle (2D debug view) → result. Playtested by 5+ people. **Go / pivot decision.** |
| M2 | Puzzle polish | 2 wk | Touch input, gem animations, cascade feedback, hint, reshuffle animation |
| M3 | War Camp | 2 wk | Drag & drop formation, bench, merge UX, enemy preview |
| M4 | 3D battle presentation | 3 wk | Isometric scene on top of the same simulator, placeholder models, Mixamo animations, Rally VFX |
| M5 | Loop integration | 2 wk | 3 stages, stars, gold, local save, results screen |
| M6 | Android & polish | 2 wk | Performance profile on target device, UI polish, playtest round |

Art direction research (packs vs. freelancer) runs in parallel from M3.

## Current status

Engine switched from Godot to Unity on 2026-11-13 (D-018); all logic was
ported to C# with its tests.
`[x]` done · `[~]` written but unverified · `[ ]` to do

- [x] M0: design decisions, docs
- [x] M0: logic test runner (`dotnet test Tools/LogicTests`)
- [ ] M0: Unity 6 LTS project created (ProjectSettings, Packages, URP, portrait)
- [ ] M0: Android build verified on a device (see SETUP.md §4)
- [x] M1: puzzle logic (`BoardModel`, `RecruitmentMeter`, `PuzzleSession`) + tests
- [~] M1: unit/enemy/stage data assets (ScriptableObjects; not yet opened in Unity)
- [ ] M1: puzzle grey-box view
- [x] M1: war camp logic (`ArmyUnit`, `Formation`, `Roster`, `MergeRules`) + tests
- [ ] M1: war camp grey-box view
- [x] M1: battle simulator + Rally + tests
- [ ] M1: battle 2D debug view
- [ ] M1: balance pass: battles last 5–15 s with current numbers (target 20–40 s)
- [x] M1: stage flow logic (`StageRun`, `AutoFormation`) + progression (`StageReward`, `PlayerProfile`) + tests
- [ ] M1: stage flow scenes (SceneRouter, SaveService, results screen)
