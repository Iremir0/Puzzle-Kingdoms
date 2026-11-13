class_name Rally
extends RefCounted
## Player-timed army buff (docs/GDD.md §9). One activation at a time; each
## consumes a charge earned on the puzzle board.

var charges: int
var duration_ticks: int
var ticks_left: int = 0


func _init(p_charges: int, p_duration_ticks: int) -> void:
	charges = p_charges
	duration_ticks = p_duration_ticks


func is_active() -> bool:
	return ticks_left > 0


func can_activate() -> bool:
	return charges > 0 and not is_active()


func activate() -> bool:
	if not can_activate():
		return false
	charges -= 1
	ticks_left = duration_ticks
	return true


## Advances one tick. Returns true on the tick the buff runs out.
func tick() -> bool:
	if not is_active():
		return false
	ticks_left -= 1
	return ticks_left == 0
