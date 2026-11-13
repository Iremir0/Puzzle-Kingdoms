# Roadmap

Estimates assume two developers working full time; double them for part time.

| # | Milestone | Est. | Exit criteria |
|---|---|---|---|
| M0 | Foundation | 1 wk | Docs, Godot project, test runner, **empty APK running on a real Android phone** |
| M1 | Grey-box prototype (2D, no art) | 2–3 wk | Full loop playable with shapes: puzzle → camp → battle (2D debug view) → result. Playtested by 5+ people. **Go / pivot decision.** |
| M2 | Puzzle polish | 2 wk | Touch input, gem animations, cascade feedback, hint, reshuffle animation |
| M3 | War Camp | 2 wk | Drag & drop formation, bench, merge UX, enemy preview |
| M4 | 3D battle presentation | 3 wk | Isometric scene on top of the same simulator, placeholder models, Mixamo animations, Rally VFX |
| M5 | Loop integration | 2 wk | 3 stages, stars, gold, local save, results screen |
| M6 | Android & polish | 2 wk | Performance profile on target device, UI polish, playtest round |

Art direction research (packs vs. freelancer) runs in parallel from M3.

## Current status

Work continues on the developers' local machines (see SETUP.md).
`[x]` done · `[~]` written but unverified · `[ ]` to do

- [x] M0: design decisions, docs, project skeleton, test runner
- [ ] M0: run the test suite locally and fix failures (tests never executed yet)
- [ ] M0: Android export verified on a device (see SETUP.md §4)
- [~] M1: puzzle logic (`BoardModel`, `RecruitmentMeter`, `PuzzleSession`) + tests: written, not yet verified
- [ ] M1: unit/enemy/stage data resources
- [ ] M1: puzzle grey-box view
- [ ] M1: war camp logic (formation, bench, merge) + grey-box view
- [ ] M1: battle simulator + 2D debug view + Rally
- [ ] M1: stage flow (SceneRouter, RunContext, results)
