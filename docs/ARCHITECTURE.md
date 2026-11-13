# Architecture

## Layers

```
Data (ScriptableObject assets)  →  Logic (plain C#, no engine)  →  Views (MonoBehaviours, scenes)
                                          ↑
                                Flow (SceneRouter + RunContext)
```

- **Data** (`PuzzleKingdoms.Data`): `UnitDefinition`, `StageDefinition`,
  `ArmyRulesAsset`, `BattleRulesAsset`, edited in the Unity Inspector. Each
  asset builds the matching plain logic object (`UnitDef`, `StageDef`, ...).
- **Logic** (`PuzzleKingdoms.Logic`): plain C# classes in an assembly with
  `noEngineReferences: true`, so Unity types cannot leak in. No
  MonoBehaviour, no coroutines, no input, no time. Deterministic given a seed.
  Fully unit-tested, also outside Unity.
- **Views**: MonoBehaviours and scenes that read logic state and play back the
  *step lists* the logic returns. Views never change game rules.
- **Flow**: `SceneRouter` switches screens and hands a `RunContext` (stage,
  recruited army, formation, result) from screen to screen.

## Key patterns

- **Compute, then animate.** `BoardModel.TrySwap()` resolves the entire move
  (all cascades) instantly and returns a `MoveResult` with ordered steps. The
  puzzle view animates those steps. Animation timing can never break the rules.
- **Fixed-tick combat.** `BattleSimulator.Step()` advances 1/20 s and returns
  `BattleEvent`s. The view runs N steps per frame (2× speed = twice the steps)
  and renders the state. Rally is applied at a tick, so a battle is
  reproducible from `(armies, rules, InputLog)`.
- **Seeded randomness.** Every random decision uses an injected `IRandom`
  (`SeededRandom`, PCG32). Never `UnityEngine.Random` or `System.Random`.
- **C# events for logic-to-view notifications**, direct calls from view to logic.
- **Few singletons.** Only `SceneRouter`, `SaveService` and `Audio` may be
  global (one bootstrap scene, `DontDestroyOnLoad`).

## Assemblies

| Assembly | References | Notes |
|---|---|---|
| `PuzzleKingdoms.Logic` | none | `noEngineReferences`, also compiled by `Tools/LogicTests` |
| `PuzzleKingdoms.Data` | Logic, UnityEngine | ScriptableObjects |
| `PuzzleKingdoms.Views` (planned) | Logic, Data, UnityEngine | scenes, UI |
| `PuzzleKingdoms.Tests` | Logic, NUnit | EditMode, also run by `dotnet test` |
| `PuzzleKingdoms.Data.Tests` | Logic, Data, UnityEditor | EditMode, Unity only |

Folder layout and "where does this go?": [STRUCTURE.md](STRUCTURE.md).

## Testing

```bash
dotnet test Tools/LogicTests
```

`Tools/LogicTests/LogicTests.csproj` compiles the logic and its tests from
`Assets/` with C# 9 (Unity's language level) and runs them with NUnit. The
same test files run in Unity's Test Runner (EditMode), together with the
data-asset tests that need the editor.
