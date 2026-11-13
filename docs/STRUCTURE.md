# Project Structure

Legend: ✅ exists · 🧪 exists, not yet verified in the Unity editor · ⬜ planned (milestone)

```
puzzle-kingdoms/
├── README.md                     ✅ entry point
├── CLAUDE.md                     ✅ rules for Claude Code sessions
├── CREDITS.md                    ✅ third-party licenses (fill before adding assets)
├── docs/                         ✅ GDD, architecture, structure, conventions, setup, decisions, roadmap
│
├── Assets/
│   └── _Project/                 everything we make (third-party packs go in their own folders)
│       ├── Scripts/
│       │   ├── Logic/            ✅ PuzzleKingdoms.Logic (no engine references)
│       │   │   ├── Core/         ✅ GridPos, IRandom, SeededRandom (PCG32)
│       │   │   ├── Puzzle/       ✅ BoardModel, MatchGroup, MoveResult, RecruitmentMeter, PuzzleSession
│       │   │   ├── Army/         ✅ UnitDef, ArmyRules, ArmyUnit, Formation, Roster, MergeRules
│       │   │   ├── Combat/       ✅ BattleRules, BattleSimulator, Combatant, Targeting, Damage,
│       │   │   │                    Rally, BattleEvent, BattleResult
│       │   │   └── Stages/       ✅ StageDef, EnemySlot
│       │   ├── Data/             🧪 PuzzleKingdoms.Data: UnitDefinition, StageDefinition,
│       │   │                        ArmyRulesAsset, BattleRulesAsset (ScriptableObjects)
│       │   ├── Flow/             ⬜ M1: SceneRouter, RunContext, SaveService, Audio
│       │   └── Views/            ⬜ M1 grey-box: Puzzle, WarCamp, Battle, Menus
│       ├── Data/                 🧪 .asset files
│       │   ├── Units/            Warrior, Archer, Mage
│       │   ├── Enemies/          GoblinGrunt, GoblinArcher, GoblinBrute, GoblinChief
│       │   ├── Stages/           Stage_1_1 … Stage_1_3
│       │   ├── ArmyRules.asset
│       │   └── BattleRules.asset
│       ├── Scenes/               ⬜ M1: Boot, MainMenu, Puzzle, WarCamp, Battle
│       ├── Prefabs/              ⬜
│       ├── Art/                  ⬜ sprites, models, materials, VFX (see CREDITS.md)
│       ├── Audio/                ⬜
│       └── Tests/
│           ├── EditMode/         ✅ PuzzleKingdoms.Tests: logic tests (also run by dotnet)
│           └── EditModeData/     🧪 PuzzleKingdoms.Data.Tests: assets match GDD (Unity only)
│
├── Packages/                     ⬜ created with the Unity project (manifest.json)
├── ProjectSettings/              ⬜ created with the Unity project
│
└── Tools/
    └── LogicTests/               ✅ dotnet test project for Logic + EditMode tests
```

## Where does new code go?

| I am writing… | Put it in |
|---|---|
| A game rule (no visuals) | `Scripts/Logic/<Feature>/` + a test in `Tests/EditMode/<Feature>/` |
| A MonoBehaviour, screen or UI | `Scripts/Views/<Feature>/` |
| Numbers a designer will tweak | a field on a ScriptableObject in `Scripts/Data/` + an asset in `Data/` |
| Something every screen needs | `Scripts/Flow/` (ask first: is it really global?) |
| A model, texture or sound | `Art/` or `Audio/` + a row in `CREDITS.md` |
| A design decision | `docs/DECISIONS.md` |

## Dependency direction

```
Views ──► Logic ◄── Data
  │                  ▲
  └──► Flow ─────────┘
```

- `Logic` references nothing (enforced by its asmdef).
- `Data` converts assets into logic objects; logic never sees ScriptableObjects.
- Features do not reach into each other's views. They share data via
  `RunContext` and logic classes.
