extends TestCase

var _def := CombatFixtures.unit_def(&"dummy", 100, 10)


func _enemy(id: int, position: Vector2, lane: int) -> Combatant:
	return CombatFixtures.combatant(id, Combatant.ENEMY, _def, position, lane)


func _attacker(lane: int = 1) -> Combatant:
	return CombatFixtures.combatant(0, Combatant.PLAYER, _def, Vector2.ZERO, lane)


func test_picks_nearest_enemy() -> void:
	var near := _enemy(1, Vector2(0, 2), 0)
	var far := _enemy(2, Vector2(0, 5), 1)
	var candidates: Array[Combatant] = [far, near]
	assert_true(Targeting.pick_target(_attacker(), candidates, 0.5) == near, "nearest wins")


func test_same_lane_wins_ties_within_tolerance() -> void:
	var other_lane := _enemy(1, Vector2(0, 3.0), 0)
	var same_lane := _enemy(2, Vector2(0, 3.4), 1)
	var candidates: Array[Combatant] = [other_lane, same_lane]
	assert_true(Targeting.pick_target(_attacker(1), candidates, 0.5) == same_lane,
			"same lane within 0.5")


func test_same_lane_loses_beyond_tolerance() -> void:
	var other_lane := _enemy(1, Vector2(0, 3.0), 0)
	var same_lane := _enemy(2, Vector2(0, 3.6), 1)
	var candidates: Array[Combatant] = [other_lane, same_lane]
	assert_true(Targeting.pick_target(_attacker(1), candidates, 0.5) == other_lane,
			"nearest beyond tolerance")


func test_lower_id_breaks_exact_ties() -> void:
	var a := _enemy(5, Vector2(-1, 2), 0)
	var b := _enemy(3, Vector2(1, 2), 2)
	var candidates: Array[Combatant] = [a, b]
	assert_true(Targeting.pick_target(_attacker(1), candidates, 0.5) == b, "lower id")


func test_ignores_dead_and_returns_null_when_none() -> void:
	var dead := _enemy(1, Vector2(0, 1), 1)
	dead.hp = 0
	var alive := _enemy(2, Vector2(0, 4), 1)
	var candidates: Array[Combatant] = [dead, alive]
	assert_true(Targeting.pick_target(_attacker(), candidates, 0.5) == alive, "skips dead")
	alive.hp = 0
	assert_true(Targeting.pick_target(_attacker(), candidates, 0.5) == null, "none left")
