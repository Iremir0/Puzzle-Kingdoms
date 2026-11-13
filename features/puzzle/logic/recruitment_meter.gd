class_name RecruitmentMeter
extends RefCounted
## Converts cleared gems into rewards: every `points_per_reward` points
## grant one reward (a unit, a Rally charge, ...). Remainders carry over.
## When `max_rewards` is reached, further points go to `overflow_points`.

const UNLIMITED := -1

var points_per_reward: int
var max_rewards: int
var points: int = 0
var rewards: int = 0
var overflow_points: int = 0


func _init(p_points_per_reward: int = 3, p_max_rewards: int = UNLIMITED) -> void:
	assert(p_points_per_reward > 0, "points_per_reward must be positive")
	points_per_reward = p_points_per_reward
	max_rewards = p_max_rewards


## Adds points and returns how many new rewards were earned.
func add_points(amount: int) -> int:
	var earned := 0
	for i in amount:
		if is_full():
			overflow_points += 1
			continue
		points += 1
		if points >= points_per_reward:
			points -= points_per_reward
			rewards += 1
			earned += 1
	return earned


func is_full() -> bool:
	return max_rewards != UNLIMITED and rewards >= max_rewards


## Progress towards the next reward in [0, 1).
func progress() -> float:
	return float(points) / float(points_per_reward)
