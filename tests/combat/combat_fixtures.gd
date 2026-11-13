class_name CombatFixtures
extends RefCounted
## Shared builders for combat tests. In-code values keep tests independent of
## balancing in data/.


static func rules() -> BattleRules:
	return BattleRules.new()


static func unit_def(
		id: StringName,
		hp: int,
		damage: int,
		interval: float = 1.0,
		attack_range: float = 1.0,
		speed: float = 1.0
) -> UnitDef:
	var def := UnitDef.new()
	def.id = id
	def.max_hp = hp
	def.damage = damage
	def.attack_interval = interval
	def.attack_range = attack_range
	def.move_speed = speed
	return def


static func formation(placements: Dictionary) -> Formation:
	## placements: Vector2i cell -> UnitDef (tier 1).
	var result := Formation.new()
	for cell: Vector2i in placements:
		result.place(ArmyUnit.new(placements[cell], 1), cell)
	return result


static func combatant(
		id: int, side: int, def: UnitDef, position: Vector2, lane: int = 1
) -> Combatant:
	var unit := ArmyUnit.new(def, 1)
	var result := Combatant.new(id, side, unit, Vector2i(lane, 0), ArmyFixtures.rules())
	result.position = position
	return result
