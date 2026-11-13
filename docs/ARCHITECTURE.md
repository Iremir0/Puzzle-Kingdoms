# Architecture

## Layers

```
Data (Resources, .tres)  →  Logic (RefCounted, no Nodes)  →  View (Scenes/Nodes)
                                     ↑
                              Flow (SceneRouter + RunContext)
```

- **Data**: `UnitDef`, `EnemyDef`, `StageDef`, ... edited in the Godot inspector.
- **Logic**: pure GDScript classes (`extends RefCounted`). No `Node`, no timers, no
  input. Deterministic given a seed. Fully unit-tested.
- **View**: scenes that read logic state and play back the *step lists* that the
  logic returns. Views never change game rules.
- **Flow**: `SceneRouter` (autoload) switches screens and hands a `RunContext`
  (stage, recruited army, formation, result) from screen to screen.

## Key patterns

- **Compute, then animate.** `BoardModel.try_swap()` resolves the entire move
  (all cascades) instantly and returns a `MoveResult` with ordered steps. The
  puzzle view animates those steps. Animation timing can never break the rules.
- **Fixed-tick combat.** `BattleSimulator.step()` advances 1/20 s. The view runs
  N steps per frame (2× speed = twice the steps) and renders the state. Player
  input (Rally) is applied at a tick, so a battle is reproducible from
  `(seed, armies, input log)`.
- **Seeded randomness.** Every random decision uses an injected
  `RandomNumberGenerator`. Never the global `randi()`.
- **Signals for view-to-flow communication**, direct calls from view to logic.
- **Autoloads** (keep the list short): `SceneRouter`, `SaveService`, `Audio`.

## Folder layout

Full annotated tree and "where does this go?" table: [STRUCTURE.md](STRUCTURE.md).

```
core/                 autoloads and cross-cutting helpers
features/<feature>/
    logic/            pure rules (tested)
    view/             scenes and node scripts
data/                 .tres game data (units, enemies, stages)
shared/ui/            theme and reusable widgets
assets/               art and audio (see CREDITS.md)
tests/                headless unit tests
docs/                 GDD, architecture, decisions
```

## Testing

`godot --headless --path . -s tests/run_tests.gd`

The runner discovers every `tests/**/test_*.gd`, runs each `test_*` method and
exits non-zero on failure. Test classes extend `TestCase` (`tests/test_case.gd`).
