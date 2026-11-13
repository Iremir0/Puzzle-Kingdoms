extends TestCase


func _target(armor: float, ward: float) -> Combatant:
	var def := CombatFixtures.unit_def(&"dummy", 100, 1)
	def.armor = armor
	def.ward = ward
	return CombatFixtures.combatant(0, Combatant.ENEMY, def, Vector2.ZERO)


func test_physical_damage_is_reduced_by_armor() -> void:
	var target := _target(0.5, 0.0)
	assert_eq(Damage.compute(16, UnitDef.DamageType.PHYSICAL, target, 1.0), 8, "half blocked")


func test_magic_damage_ignores_armor_and_uses_ward() -> void:
	assert_eq(Damage.compute(22, UnitDef.DamageType.MAGIC, _target(0.5, 0.0), 1.0), 22,
			"armor ignored")
	assert_eq(Damage.compute(20, UnitDef.DamageType.MAGIC, _target(0.0, 0.25), 1.0), 15,
			"ward applies")


func test_multiplier_scales_damage() -> void:
	assert_eq(Damage.compute(12, UnitDef.DamageType.PHYSICAL, _target(0.0, 0.0), 1.5), 18,
			"rally bonus")


func test_damage_is_at_least_one() -> void:
	assert_eq(Damage.compute(1, UnitDef.DamageType.PHYSICAL, _target(0.9, 0.0), 1.0), 1,
			"minimum damage")
