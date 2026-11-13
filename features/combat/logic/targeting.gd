class_name Targeting
extends RefCounted
## Target choice (docs/GDD.md §9): the nearest living enemy. Enemies at most
## `same_lane_tie` farther than the nearest one count as tied; among tied
## enemies the attacker's own lane wins, then the shorter distance, then the
## lower id.


static func pick_target(
		attacker: Combatant, candidates: Array[Combatant], same_lane_tie: float
) -> Combatant:
	var nearest := INF
	for other in candidates:
		if other.is_alive() and attacker.is_enemy_of(other):
			nearest = minf(nearest, attacker.position.distance_to(other.position))
	if nearest == INF:
		return null

	var best: Combatant = null
	var best_distance := INF
	for other in candidates:
		if not (other.is_alive() and attacker.is_enemy_of(other)):
			continue
		var distance := attacker.position.distance_to(other.position)
		if distance > nearest + same_lane_tie:
			continue
		if best == null or _is_better(attacker, other, distance, best, best_distance):
			best = other
			best_distance = distance
	return best


static func _is_better(
		attacker: Combatant, a: Combatant, a_distance: float, b: Combatant, b_distance: float
) -> bool:
	var a_same_lane := a.lane == attacker.lane
	var b_same_lane := b.lane == attacker.lane
	if a_same_lane != b_same_lane:
		return a_same_lane
	if not is_equal_approx(a_distance, b_distance):
		return a_distance < b_distance
	return a.id < b.id
