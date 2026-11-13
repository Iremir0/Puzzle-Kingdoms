extends TestCase

var _warrior := ArmyFixtures.unit_def(&"warrior")
var _archer := ArmyFixtures.unit_def(&"archer")


func _roster() -> Roster:
	return Roster.new(ArmyFixtures.rules())


func test_cannot_merge_with_fewer_than_three() -> void:
	var roster := _roster()
	var units := roster.recruit(_warrior, 2)
	roster.recruit(_archer, 1)
	assert_false(roster.can_merge(units[0]), "two warriors are not enough")
	assert_true(roster.merge(units[0]) == null, "merge refused")
	assert_eq(roster.all_units().size(), 3, "nothing consumed")


func test_merge_on_bench_creates_next_tier() -> void:
	var roster := _roster()
	var units := roster.recruit(_warrior, 3)
	assert_true(roster.can_merge(units[0]), "can merge")
	var merged := roster.merge(units[0])
	assert_true(merged != null, "merged")
	assert_eq(merged.tier, 2, "tier 2")
	assert_true(merged.def == _warrior, "same class")
	assert_eq(roster.bench.size(), 1, "only the merged unit remains")
	assert_true(roster.bench[0] == merged, "merged unit on bench")


func test_merged_unit_keeps_deployed_target_cell() -> void:
	var roster := _roster()
	var units := roster.recruit(_warrior, 3)
	roster.deploy(units[0], Vector2i(1, 0))
	var merged := roster.merge(units[0])
	assert_true(roster.formation.get_unit(Vector2i(1, 0)) == merged, "takes target cell")
	assert_eq(roster.bench.size(), 0, "bench copies consumed")


func test_merge_prefers_bench_copies_over_deployed_ones() -> void:
	var roster := _roster()
	var units := roster.recruit(_warrior, 4)
	roster.deploy(units[0], Vector2i(0, 0))
	roster.deploy(units[1], Vector2i(1, 0))
	var merged := roster.merge(units[0])
	assert_true(roster.formation.get_unit(Vector2i(1, 0)) == units[1], "deployed copy kept")
	assert_true(roster.formation.get_unit(Vector2i(0, 0)) == merged, "target cell")
	assert_eq(roster.bench.size(), 0, "both bench copies consumed")


func test_merge_ignores_other_tiers_and_classes() -> void:
	var roster := _roster()
	var units := roster.recruit(_warrior, 3)
	var tier2 := roster.merge(units[0])
	var fresh := roster.recruit(_warrior, 2)
	roster.recruit(_archer, 1)
	assert_false(roster.can_merge(tier2), "only one tier 2 warrior")
	assert_false(roster.can_merge(fresh[0]), "only two tier 1 warriors")


func test_cannot_merge_beyond_max_tier() -> void:
	var roster := _roster()
	var tier3: Array[ArmyUnit] = []
	for i in 3:
		var tier2: Array[ArmyUnit] = []
		for j in 3:
			var units := roster.recruit(_warrior, 3)
			tier2.append(roster.merge(units[0]))
		tier3.append(roster.merge(tier2[0]))
	assert_eq(tier3[0].tier, 3, "tier 3 reached")
	assert_false(roster.can_merge(tier3[0]), "tier 3 is max")
	assert_eq(roster.all_units().size(), 3, "three tier 3 units")
