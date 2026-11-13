extends TestCase


func test_tier_scales_hp_and_damage() -> void:
	var rules := ArmyFixtures.rules()
	var def := ArmyFixtures.unit_def(&"warrior", 120, 12)
	var t1 := ArmyUnit.new(def, 1)
	var t2 := ArmyUnit.new(def, 2)
	var t3 := ArmyUnit.new(def, 3)
	assert_eq([t1.max_hp(rules), t1.damage(rules)], [120, 12], "tier 1")
	assert_eq([t2.max_hp(rules), t2.damage(rules)], [216, 22], "tier 2 (x1.8, rounded)")
	assert_eq([t3.max_hp(rules), t3.damage(rules)], [384, 38], "tier 3 (x3.2, rounded)")


func test_same_kind_compares_class_and_tier() -> void:
	var warrior := ArmyFixtures.unit_def(&"warrior")
	var archer := ArmyFixtures.unit_def(&"archer")
	var a := ArmyUnit.new(warrior, 1)
	assert_true(a.is_same_kind(ArmyUnit.new(warrior, 1)), "same class and tier")
	assert_false(a.is_same_kind(ArmyUnit.new(warrior, 2)), "different tier")
	assert_false(a.is_same_kind(ArmyUnit.new(archer, 1)), "different class")
