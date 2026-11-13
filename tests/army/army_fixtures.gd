class_name ArmyFixtures
extends RefCounted
## Shared builders for army tests. Uses in-code defs so tests do not depend
## on balancing values in data/.


static func unit_def(id: StringName, hp: int = 100, damage: int = 10) -> UnitDef:
	var def := UnitDef.new()
	def.id = id
	def.display_name = String(id).capitalize()
	def.max_hp = hp
	def.damage = damage
	return def


static func rules() -> ArmyRules:
	var result := ArmyRules.new()
	result.tier_multipliers = PackedFloat32Array([1.0, 1.8, 3.2])
	result.merge_count = 3
	return result
