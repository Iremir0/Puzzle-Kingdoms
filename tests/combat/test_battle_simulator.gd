extends TestCase

var _army_rules := ArmyFixtures.rules()


func _sim(
		rules: BattleRules, player: Dictionary, enemy: Dictionary, rally_charges: int = 0
) -> BattleSimulator:
	return BattleSimulator.new(rules, _army_rules, CombatFixtures.formation(player),
			CombatFixtures.formation(enemy), rally_charges)


## Rules where both front rows start 2 cells apart, so ranged units shoot at once.
func _close_rules() -> BattleRules:
	var rules := CombatFixtures.rules()
	rules.front_gap = 2.0
	return rules


func _archer(interval: float = 0.5) -> UnitDef:
	return CombatFixtures.unit_def(&"archer", 100, 10, interval, 4.0, 1.0)


## Never moves and never reaches anything: a target dummy.
func _dummy(hp: int = 100000) -> UnitDef:
	return CombatFixtures.unit_def(&"dummy", hp, 1, 1.0, 0.1, 0.0)


func _attacks(events: Array[BattleEvent]) -> Array[BattleEvent]:
	return events.filter(func(e: BattleEvent) -> bool: return e.type == BattleEvent.Type.ATTACK)


func _run_collect(sim: BattleSimulator, ticks: int) -> Array[BattleEvent]:
	var all: Array[BattleEvent] = []
	for i in ticks:
		all.append_array(sim.step())
	return all


func test_spawn_positions_follow_formation_cells() -> void:
	var rules := CombatFixtures.rules()
	var sim := _sim(rules, {Vector2i(1, 0): _dummy()}, {Vector2i(0, 2): _dummy()})
	var player := sim.combatants[0]
	var enemy := sim.combatants[1]
	assert_eq(player.side, Combatant.PLAYER, "player first")
	assert_eq(player.position, Vector2(0, -3), "player front center")
	assert_eq(enemy.position, Vector2(-1.2, 5), "enemy back left")
	assert_eq(enemy.lane, 0, "enemy lane")


func test_melee_advances_and_stops_at_range() -> void:
	var warrior := CombatFixtures.unit_def(&"warrior", 1000, 1, 1.0, 1.0, 1.0)
	var sim := _sim(CombatFixtures.rules(), {Vector2i(1, 0): warrior}, {Vector2i(1, 0): _dummy()})
	var first_attack_tick := -1
	for i in 200:
		if not _attacks(sim.step()).is_empty():
			first_attack_tick = sim.tick
			break
	assert_true(first_attack_tick > 0, "warrior reached the dummy")
	var distance := sim.combatants[0].position.distance_to(sim.combatants[1].position)
	assert_true(distance <= 1.0 + 0.001 and distance > 0.9, "stopped at range (%f)" % distance)
	# 6 cells apart, 1 cell range, 1 cell/s at 20 ticks/s: 5 cells take 100 ticks.
	assert_true(absi(first_attack_tick - 101) <= 1, "arrival tick %d" % first_attack_tick)


func test_attacks_follow_attack_interval() -> void:
	var sim := _sim(_close_rules(), {Vector2i(1, 0): _archer(0.5)}, {Vector2i(1, 0): _dummy()})
	var attacks := _attacks(_run_collect(sim, 100))
	var ticks: Array[int] = []
	for event in attacks:
		ticks.append(event.tick)
	assert_eq(ticks, [1, 11, 21, 31, 41, 51, 61, 71, 81, 91] as Array[int], "every 10 ticks")


func test_victory_when_enemies_are_wiped_out() -> void:
	var strong := CombatFixtures.unit_def(&"strong", 1000, 50, 0.5, 4.0, 1.0)
	var weak := CombatFixtures.unit_def(&"weak", 40, 1, 1.0, 1.0, 1.0)
	var sim := _sim(CombatFixtures.rules(), {Vector2i(1, 0): strong},
			{Vector2i(0, 0): weak, Vector2i(2, 0): weak})
	var result := sim.run_to_end()
	assert_true(sim.is_finished(), "finished")
	assert_true(result.victory, "victory")
	assert_false(result.timed_out, "not a timeout")
	assert_eq([result.deployed, result.survivors], [1, 1], "survivors")
	assert_eq(result.stars(), 3, "three stars")


func test_defeat_when_player_is_wiped_out() -> void:
	var weak := CombatFixtures.unit_def(&"weak", 40, 1, 1.0, 1.0, 1.0)
	var strong := CombatFixtures.unit_def(&"strong", 1000, 50, 0.5, 4.0, 1.0)
	var result := _sim(CombatFixtures.rules(), {Vector2i(1, 0): weak},
			{Vector2i(1, 0): strong}).run_to_end()
	assert_false(result.victory, "defeat")
	assert_eq(result.survivors, 0, "no survivors")
	assert_eq(result.stars(), 0, "no stars")


func test_timeout_is_a_defeat() -> void:
	var rules := CombatFixtures.rules()
	rules.timeout_seconds = 2.0
	var sim := _sim(rules, {Vector2i(1, 0): _dummy()}, {Vector2i(1, 0): _dummy()})
	var result := sim.run_to_end()
	assert_true(result.timed_out, "timed out")
	assert_false(result.victory, "defeat")
	assert_eq(result.ticks, 40, "2 s at 20 ticks/s")


func test_mutual_wipe_is_a_defeat() -> void:
	var rules := CombatFixtures.rules()
	rules.front_gap = 1.0
	var glass := CombatFixtures.unit_def(&"glass", 5, 10, 1.0, 1.0, 1.0)
	var result := _sim(rules, {Vector2i(1, 0): glass}, {Vector2i(1, 0): glass}).run_to_end()
	assert_eq(result.ticks, 1, "both hit on the first tick")
	assert_false(result.victory, "simultaneous wipe is a defeat")


func test_dead_units_stop_acting_and_emit_one_death() -> void:
	var strong := CombatFixtures.unit_def(&"strong", 1000, 50, 0.5, 4.0, 1.0)
	var weak := CombatFixtures.unit_def(&"weak", 40, 1, 1.0, 4.0, 1.0)
	var sim := _sim(_close_rules(), {Vector2i(1, 0): strong}, {Vector2i(1, 0): weak})
	var events := _run_collect(sim, 50)
	var deaths := events.filter(func(e: BattleEvent) -> bool: return e.type == BattleEvent.Type.DIED)
	assert_eq(deaths.size(), 1, "one death event")
	assert_eq(deaths[0].source_id, 1, "enemy died")
	var finished := events.filter(func(e: BattleEvent) -> bool: return e.type == BattleEvent.Type.FINISHED)
	assert_eq(finished.size(), 1, "one finish event")
	assert_true(sim.step().is_empty(), "no events after the end")


func test_rally_boosts_player_damage_and_logs_input() -> void:
	var sim := _sim(_close_rules(), {Vector2i(1, 0): _archer(0.5)}, {Vector2i(1, 0): _dummy()}, 1)
	assert_true(sim.activate_rally(), "rally activated")
	assert_false(sim.activate_rally(), "cannot stack")
	var events := sim.step()
	assert_eq(events[0].type, BattleEvent.Type.RALLY_STARTED, "start event first")
	assert_eq(_attacks(events)[0].amount, 15, "+50% damage")
	assert_eq(sim.input_log, PackedInt32Array([0]), "input logged at tick 0")


func test_rally_speeds_up_attacks_and_expires() -> void:
	var sim := _sim(_close_rules(), {Vector2i(1, 0): _archer(0.5)}, {Vector2i(1, 0): _dummy()}, 1)
	sim.activate_rally()
	var events := _run_collect(sim, 140)
	var ended := events.filter(func(e: BattleEvent) -> bool: return e.type == BattleEvent.Type.RALLY_ENDED)
	assert_eq(ended.size(), 1, "rally ended once")
	assert_eq(ended[0].tick, 100, "after 5 s")
	var during := _attacks(events).filter(func(e: BattleEvent) -> bool: return e.tick <= 100)
	# 0.5 s interval at 1.5x speed: one attack every 6.67 ticks → 15 in 100 ticks.
	assert_eq(during.size(), 15, "attacks during rally")
	var after := _attacks(events).filter(func(e: BattleEvent) -> bool: return e.tick > 100)
	assert_eq(after[0].amount, 10, "normal damage after rally")


func test_same_setup_same_battle() -> void:
	var warrior := CombatFixtures.unit_def(&"warrior", 120, 12, 1.0, 1.0, 1.2)
	var archer := CombatFixtures.unit_def(&"archer", 60, 10, 0.8, 4.0, 1.0)
	var grunt := CombatFixtures.unit_def(&"grunt", 90, 10, 1.0, 1.0, 1.2)
	var player := {Vector2i(0, 0): warrior, Vector2i(2, 0): warrior, Vector2i(1, 2): archer}
	var enemy := {Vector2i(0, 0): grunt, Vector2i(1, 0): grunt, Vector2i(2, 0): grunt}
	var logs: Array[String] = []
	for run in 2:
		var sim := _sim(CombatFixtures.rules(), player, enemy, 1)
		var log := []
		while not sim.is_finished():
			if sim.tick == 60:
				sim.activate_rally()
			for e in sim.step():
				log.append([e.tick, e.type, e.source_id, e.target_id, e.amount])
		logs.append(str(log))
	assert_eq(logs[0], logs[1], "identical event logs")


func test_stars_follow_survivor_ratio() -> void:
	var result := BattleResult.new()
	result.victory = true
	result.deployed = 4
	result.survivors = 4
	assert_eq(result.stars(), 3, "all alive")
	result.survivors = 2
	assert_eq(result.stars(), 2, "half alive")
	result.survivors = 1
	assert_eq(result.stars(), 1, "win")
	result.victory = false
	assert_eq(result.stars(), 0, "defeat")


func test_stage_1_1_is_winnable_with_a_basic_army() -> void:
	var stage := load("res://data/stages/stage_1_1.tres") as StageDef
	var warrior := load("res://data/units/warrior.tres") as UnitDef
	var archer := load("res://data/units/archer.tres") as UnitDef
	var player := CombatFixtures.formation({
		Vector2i(0, 0): warrior, Vector2i(1, 0): warrior, Vector2i(2, 0): warrior,
		Vector2i(0, 2): archer, Vector2i(2, 2): archer,
	})
	var sim := BattleSimulator.new(load("res://data/battle_rules.tres"),
			load("res://data/army_rules.tres"), player,
			Formation.from_enemy_slots(stage.enemies), 0)
	var result := sim.run_to_end()
	assert_true(result.victory, "basic army beats stage 1-1")
