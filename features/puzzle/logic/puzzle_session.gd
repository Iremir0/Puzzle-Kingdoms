class_name PuzzleSession
extends RefCounted
## One stage's puzzle phase: a board, a move budget and one RecruitmentMeter
## per reward key (e.g. "warrior", "archer", "mage", "rally").
##
## `gem_reward_keys[gem_type]` names the meter that gem type feeds, so the
## board itself stays generic.

signal rewards_earned(reward_key: String, amount: int)
signal moves_changed(moves_left: int)
signal finished

var board: BoardModel
var moves_left: int
var gem_reward_keys: PackedStringArray
var meters: Dictionary = {}  # reward_key -> RecruitmentMeter


func _init(
		p_board: BoardModel,
		p_moves: int,
		p_gem_reward_keys: PackedStringArray,
		p_meters: Dictionary
) -> void:
	assert(p_gem_reward_keys.size() == p_board.gem_type_count,
			"Every gem type needs a reward key")
	for key in p_gem_reward_keys:
		assert(p_meters.has(key), "Missing meter for reward key '%s'" % key)
	board = p_board
	moves_left = p_moves
	gem_reward_keys = p_gem_reward_keys
	meters = p_meters


func is_finished() -> bool:
	return moves_left <= 0


## Plays a swap. Invalid swaps return an invalid MoveResult and cost no move.
func play_swap(a: Vector2i, b: Vector2i) -> MoveResult:
	if is_finished():
		return MoveResult.new()
	var result := board.try_swap(a, b)
	if not result.is_valid:
		return result

	moves_left -= 1
	for gem_type: int in result.cleared_by_type:
		var key := gem_reward_keys[gem_type]
		var earned: int = meters[key].add_points(result.cleared_by_type[gem_type])
		if earned > 0:
			rewards_earned.emit(key, earned)
	moves_changed.emit(moves_left)
	if is_finished():
		finished.emit()
	return result


func reward_count(reward_key: String) -> int:
	return meters[reward_key].rewards
