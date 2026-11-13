# Project Structure

Legend: ✅ exists · 🧪 exists, tests not yet run · ⬜ planned (milestone)

```
puzzle-kingdoms/
├── project.godot                 ✅ Godot 4.6, Mobile renderer, portrait 1080×1920
├── README.md                     ✅ entry point
├── CLAUDE.md                     ✅ rules for Claude Code sessions
├── CREDITS.md                    ✅ third-party licenses (fill before adding assets)
│
├── docs/
│   ├── README.md                 ✅ documentation index
│   ├── GDD.md                    ✅ game design (rules and numbers)
│   ├── ARCHITECTURE.md           ✅ layers and patterns
│   ├── STRUCTURE.md              ✅ this file
│   ├── CONVENTIONS.md            ✅ code style, naming, git workflow
│   ├── SETUP.md                  ✅ local machine setup, tests, Android export
│   ├── DECISIONS.md              ✅ decision log
│   └── ROADMAP.md                ✅ milestones and status
│
├── core/                         ⬜ M1
│   ├── scene_router.gd           ⬜ autoload: screen switching
│   ├── run_context.gd            ⬜ data handed between screens
│   ├── save_service.gd           ⬜ autoload: local JSON save
│   └── audio.gd                  ⬜ autoload: sfx/music
│
├── features/
│   ├── puzzle/
│   │   ├── logic/
│   │   │   ├── board_model.gd        🧪 board rules, cascades, reshuffle
│   │   │   ├── match_group.gd        🧪 connected match shape
│   │   │   ├── move_result.gd        🧪 playback steps of one move
│   │   │   ├── recruitment_meter.gd  🧪 gems → units / Rally charges
│   │   │   └── puzzle_session.gd     🧪 moves + meters for one stage
│   │   └── view/                     ⬜ M1 grey-box, M2 polish
│   ├── army/
│   │   ├── logic/                    ⬜ M1: army_unit, roster, formation, merge_rules
│   │   └── view/                     ⬜ M1 grey-box, M3 drag & drop
│   ├── combat/
│   │   ├── logic/                    ⬜ M1: battle_simulator, combatant, targeting, damage, rally
│   │   └── view/                     ⬜ M1 2D debug view, M4 3D isometric view
│   ├── progression/
│   │   └── logic/                    ⬜ M5: player_profile, reward_rules
│   └── menus/
│       ├── main_menu.tscn            ✅ placeholder
│       └── main_menu.gd              ✅ placeholder
│                                     ⬜ stage_select, results screens
│
├── data/                         ⬜ M1: .tres resources
│   ├── units/                    warrior, archer, mage
│   ├── enemies/                  goblin_grunt, goblin_archer, goblin_brute, goblin_chief
│   └── stages/                   stage_1_1 … stage_1_3
│
├── shared/ui/                    ⬜ theme, fonts, reusable widgets
│
├── assets/                       ⬜ art and audio only (see CREDITS.md)
│   ├── characters/  environments/  ui/  audio/  effects/
│
└── tests/
    ├── run_tests.gd              🧪 headless runner
    ├── test_case.gd              🧪 assertion base class
    └── puzzle/
        ├── test_board_model.gd   🧪
        └── test_recruitment.gd   🧪
```

Each folder also contains a short `README.md` describing what belongs in it.

## Where does new code go?

| I am writing… | Put it in |
|---|---|
| A game rule (no visuals) | `features/<feature>/logic/` + a test in `tests/<feature>/` |
| A screen, scene or node script | `features/<feature>/view/` |
| Numbers a designer will tweak | a Resource in `data/` |
| Something every screen needs | `core/` (ask first: is it really global?) |
| A reusable button or panel | `shared/ui/` |
| A model, texture or sound | `assets/` + a row in `CREDITS.md` |
| A design decision | `docs/DECISIONS.md` |

## Dependency direction

```
view ──► logic ──► data
  │                  ▲
  └──► core ─────────┘
```

- `logic` never references `view`, `core` or any Node.
- Features do not reach into each other's `view/`. They share data via
  `RunContext` and logic classes.
- `tests/` only test `logic/`.
