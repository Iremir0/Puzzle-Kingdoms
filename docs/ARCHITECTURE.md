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
- **Flow**: `GameApp` owns the `PlayerProfile` and the current `StageRun`,
  swaps screens and saves through `ProfileStore`. Grey-box: one scene, screens
  are code-built uGUI panels (D-025).

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
- **Few singletons.** Only `GameApp` (and later `Audio`) may be global.

## Assemblies

| Assembly | References | Notes |
|---|---|---|
| `PuzzleKingdoms.Logic` | none | `noEngineReferences`, also compiled by `Tools/LogicTests` |
| `PuzzleKingdoms.Data` | Logic, UnityEngine | ScriptableObjects |
| `PuzzleKingdoms.Views` | Logic, Data, UnityEngine.UI | screens; get callbacks, never call Flow |
| `PuzzleKingdoms.Flow` | Logic, Data, Views | `GameApp`, `ProfileStore` |
| `PuzzleKingdoms.Editor` | Logic, Data, Flow, URP | `ProjectSetup`, `SceneBuilder` |
| `PuzzleKingdoms.Tests` | Logic, NUnit | EditMode, also run by `dotnet test` |
| `PuzzleKingdoms.Data.Tests` | Logic, Data, UnityEditor | EditMode, Unity only |
| `PuzzleKingdoms.PlayMode.Tests` | Logic, Data, Views, Flow | full loop smoke test; screenshot capture (explicit) |

Folder layout and "where does this go?": [STRUCTURE.md](STRUCTURE.md).

## Testing

```bash
dotnet test Tools/LogicTests
```

`Tools/LogicTests/LogicTests.csproj` compiles the logic and its tests from
`Assets/` with C# 9 (Unity's language level) and runs them with NUnit. The
same test files run in Unity's Test Runner (EditMode), together with the
data-asset tests that need the editor.
