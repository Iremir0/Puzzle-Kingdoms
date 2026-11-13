class_name MoveResult
extends RefCounted
## Everything that happened during one player swap, in playback order.
## The puzzle view animates `steps`; gameplay systems read `cleared_by_type`.


## One wave of clearing: matched groups disappear, gems fall, new gems spawn.
class CascadeStep:
	var groups: Array[MatchGroup] = []
	var falls: Array[GemFall] = []
	var spawns: Array[GemSpawn] = []


class GemFall:
	var from: Vector2i
	var to: Vector2i

	func _init(p_from: Vector2i, p_to: Vector2i) -> void:
		from = p_from
		to = p_to


class GemSpawn:
	var cell: Vector2i
	var gem_type: int
	## How many cells above the board the gem should start its fall from.
	var drop_distance: int

	func _init(p_cell: Vector2i, p_gem_type: int, p_drop_distance: int) -> void:
		cell = p_cell
		gem_type = p_gem_type
		drop_distance = p_drop_distance


var is_valid: bool = false
var swap_a: Vector2i
var swap_b: Vector2i
var steps: Array[CascadeStep] = []
## gem_type -> number of gems of that type cleared during the whole move.
var cleared_by_type: Dictionary = {}
## True when the board had no valid move afterwards and was reshuffled.
var reshuffled: bool = false
## Board rows after the reshuffle (only set when `reshuffled` is true).
var rows_after_reshuffle: Array = []


## Number of cascade waves after the first clear (0 = no cascade).
func cascade_count() -> int:
	return maxi(steps.size() - 1, 0)


func cleared_count(gem_type: int) -> int:
	return cleared_by_type.get(gem_type, 0)
