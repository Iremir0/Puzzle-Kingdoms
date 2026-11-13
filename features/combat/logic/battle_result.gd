class_name BattleResult
extends RefCounted
## Outcome of a finished battle (docs/GDD.md §10 for stars).

var victory: bool = false
var timed_out: bool = false
var ticks: int = 0
## Player units that started the battle.
var deployed: int = 0
## Player units alive at the end.
var survivors: int = 0


## 0 on defeat; 1 = win, 2 = at least half survived, 3 = nobody lost.
func stars() -> int:
	if not victory:
		return 0
	if survivors >= deployed:
		return 3
	if survivors * 2 >= deployed:
		return 2
	return 1
