extends TestCase

var _warrior := ArmyFixtures.unit_def(&"warrior")
var _archer := ArmyFixtures.unit_def(&"archer")


func _roster() -> Roster:
	return Roster.new(ArmyFixtures.rules())


func test_recruits_go_to_bench_with_unique_ids() -> void:
	var roster := _roster()
	var added := roster.recruit(_warrior, 3)
	assert_eq(added.size(), 3, "three recruits")
	assert_eq(roster.bench.size(), 3, "on bench")
	assert_eq(roster.formation.count(), 0, "none deployed")
	var ids := {}
	for unit in added:
		assert_eq(unit.tier, 1, "tier 1")
		ids[unit.uid] = true
	assert_eq(ids.size(), 3, "unique ids")


func test_deploy_moves_unit_from_bench_to_formation() -> void:
	var roster := _roster()
	var unit: ArmyUnit = roster.recruit(_warrior, 1)[0]
	assert_true(roster.deploy(unit, Vector2i(1, 0)), "deployed")
	assert_eq(roster.bench.size(), 0, "bench empty")
	assert_true(roster.formation.get_unit(Vector2i(1, 0)) == unit, "in formation")


func test_deploy_onto_occupied_cell_benches_previous_unit() -> void:
	var roster := _roster()
	var a: ArmyUnit = roster.recruit(_warrior, 1)[0]
	var b: ArmyUnit = roster.recruit(_archer, 1)[0]
	roster.deploy(a, Vector2i(0, 0))
	roster.deploy(b, Vector2i(0, 0))
	assert_true(roster.formation.get_unit(Vector2i(0, 0)) == b, "b deployed")
	assert_eq(roster.bench.size(), 1, "one unit on bench")
	assert_true(roster.bench[0] == a, "a back on bench")


func test_deploy_of_deployed_unit_moves_it() -> void:
	var roster := _roster()
	var a: ArmyUnit = roster.recruit(_warrior, 1)[0]
	roster.deploy(a, Vector2i(0, 0))
	assert_true(roster.deploy(a, Vector2i(2, 2)), "moved")
	assert_true(roster.formation.get_unit(Vector2i(0, 0)) == null, "old cell empty")
	assert_true(roster.formation.get_unit(Vector2i(2, 2)) == a, "new cell")


func test_deploy_rejects_unknown_unit_and_bad_cell() -> void:
	var roster := _roster()
	var stranger := ArmyUnit.new(_warrior, 1)
	assert_false(roster.deploy(stranger, Vector2i(0, 0)), "unknown unit")
	var unit: ArmyUnit = roster.recruit(_warrior, 1)[0]
	assert_false(roster.deploy(unit, Vector2i(3, 3)), "outside grid")
	assert_eq(roster.bench.size(), 1, "still on bench")


func test_undeploy_returns_unit_to_bench() -> void:
	var roster := _roster()
	var unit: ArmyUnit = roster.recruit(_warrior, 1)[0]
	roster.deploy(unit, Vector2i(1, 1))
	assert_true(roster.undeploy(Vector2i(1, 1)), "undeployed")
	assert_eq(roster.bench.size(), 1, "back on bench")
	assert_false(roster.undeploy(Vector2i(1, 1)), "nothing left to undeploy")


func test_all_units_counts_bench_and_formation() -> void:
	var roster := _roster()
	var units := roster.recruit(_warrior, 4)
	roster.deploy(units[0], Vector2i(0, 0))
	roster.deploy(units[1], Vector2i(1, 0))
	assert_eq(roster.all_units().size(), 4, "all units")
