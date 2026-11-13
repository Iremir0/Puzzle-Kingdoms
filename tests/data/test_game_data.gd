extends TestCase
## Validates the shipped .tres data against docs/GDD.md and basic invariants.

const UNIT_PATHS := [
	"res://data/units/warrior.tres",
	"res://data/units/archer.tres",
	"res://data/units/mage.tres",
	"res://data/enemies/goblin_grunt.tres",
	"res://data/enemies/goblin_archer.tres",
	"res://data/enemies/goblin_brute.tres",
	"res://data/enemies/goblin_chief.tres",
]
const STAGE_PATHS := [
	"res://data/stages/stage_1_1.tres",
	"res://data/stages/stage_1_2.tres",
	"res://data/stages/stage_1_3.tres",
]


func test_unit_defs_load_with_sane_values() -> void:
	for path: String in UNIT_PATHS:
		var unit := load(path) as UnitDef
		assert_true(unit != null, "loads %s" % path)
		if unit == null:
			continue
		assert_eq(String(unit.id), path.get_file().get_basename(), "id matches file name")
		assert_true(unit.max_hp > 0 and unit.damage > 0, "%s has hp and damage" % unit.id)
		assert_true(unit.attack_interval > 0.0 and unit.move_speed > 0.0, "%s timings" % unit.id)
		assert_true(unit.armor < 1.0 and unit.ward < 1.0, "%s mitigation below 100 percent" % unit.id)


func test_player_units_match_gdd() -> void:
	var warrior := load("res://data/units/warrior.tres") as UnitDef
	assert_eq([warrior.max_hp, warrior.damage], [120, 12], "warrior")
	var archer := load("res://data/units/archer.tres") as UnitDef
	assert_eq([archer.max_hp, archer.damage, archer.attack_range], [60, 10, 4.0], "archer")
	var mage := load("res://data/units/mage.tres") as UnitDef
	assert_eq([mage.max_hp, mage.damage], [55, 22], "mage")
	assert_eq(mage.damage_type, UnitDef.DamageType.MAGIC, "mage deals magic damage")


func test_army_rules_match_gdd() -> void:
	var rules := load("res://data/army_rules.tres") as ArmyRules
	assert_eq(rules.max_tier(), 3, "max tier")
	assert_eq(rules.merge_count, 3, "merge count")
	assert_true(is_equal_approx(rules.tier_multiplier(2), 1.8), "tier 2 multiplier")
	assert_true(is_equal_approx(rules.tier_multiplier(3), 3.2), "tier 3 multiplier")


func test_stages_have_valid_enemy_formations() -> void:
	for path: String in STAGE_PATHS:
		var stage := load(path) as StageDef
		assert_true(stage != null, "loads %s" % path)
		if stage == null:
			continue
		assert_eq(String(stage.id), path.get_file().get_basename(), "id matches file name")
		assert_true(stage.moves > 0, "%s has moves" % stage.id)
		assert_false(stage.enemies.is_empty(), "%s has enemies" % stage.id)
		var used := {}
		for slot in stage.enemies:
			assert_true(slot.unit != null, "%s slot has a unit" % stage.id)
			var inside := slot.cell.x >= 0 and slot.cell.x < 3 and slot.cell.y >= 0 and slot.cell.y < 3
			assert_true(inside, "%s cell %s inside 3x3" % [stage.id, slot.cell])
			assert_false(used.has(slot.cell), "%s cell %s used once" % [stage.id, slot.cell])
			used[slot.cell] = true


func test_stage_enemy_counts_match_gdd() -> void:
	var expected := {
		"stage_1_1": {"goblin_grunt": 3},
		"stage_1_2": {"goblin_grunt": 3, "goblin_archer": 2},
		"stage_1_3": {"goblin_brute": 2, "goblin_archer": 2, "goblin_chief": 1},
	}
	for path: String in STAGE_PATHS:
		var stage := load(path) as StageDef
		var counts := {}
		for slot in stage.enemies:
			var key := String(slot.unit.id)
			counts[key] = counts.get(key, 0) + 1
		assert_eq(counts, expected[String(stage.id)], "%s enemies" % stage.id)
