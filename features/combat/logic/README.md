# features/combat/logic/

Deterministic battle simulation (RefCounted, tested). Fixed 20 ticks/s,
2D logical space, no randomness, no Nodes.

Planned:
- `battle_simulator.gd`: `step()` advances one tick, emits battle events.
- `combatant.gd`: runtime state of a unit (hp, position, target, cooldown).
- `targeting.gd`: nearest enemy, same-lane tie-break.
- `damage.gd`: physical vs. magic, armor and ward.
- `rally.gd`: player-timed army buff.
- `battle_result.gd`: winner, survivors, duration, stars.
