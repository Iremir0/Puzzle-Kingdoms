# features/combat/logic/

Deterministic battle simulation (RefCounted, tested). Fixed 20 ticks/s,
2D logical space, no randomness, no Nodes.

- `battle_simulator.gd`: `step()` advances one tick and returns `BattleEvent`s;
  `run_to_end(rally_ticks)` for headless sims and replays.
- `combatant.gd`: runtime state of a unit (hp, position, target, cooldown).
- `targeting.gd`: nearest enemy, same-lane tie-break.
- `damage.gd`: physical vs. magic, armor and ward.
- `rally.gd`: player-timed army buff.
- `battle_event.gd`: attack / died / rally / finished events for views.
- `battle_result.gd`: victory, survivors, duration, stars.

Numbers come from `data/battle_rules.tres` (`BattleRules`).
