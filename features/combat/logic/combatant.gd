class_name Combatant
extends RefCounted
## Runtime state of one unit during a battle. Stats are fixed at spawn from
## the ArmyUnit and its tier; `hp`, `position`, `target_id` and `cooldown`
## change every tick.

const PLAYER := 0
const ENEMY := 1
const NO_TARGET := -1

var id: int
var side: int
var unit: ArmyUnit
## Formation cell the unit spawned from (x = lane, y = row).
var cell: Vector2i
var lane: int

var max_hp: int
var hp: int
var damage: int
var damage_type: UnitDef.DamageType
var attack_interval: float
var attack_range: float
var move_speed: float
var armor: float
var ward: float

var position: Vector2 = Vector2.ZERO
## Target is stored by id to avoid reference cycles between combatants.
var target_id: int = NO_TARGET
## Seconds until the next attack is ready; may carry a small negative rest.
var cooldown: float = 0.0


func _init(
		p_id: int, p_side: int, p_unit: ArmyUnit, p_cell: Vector2i, army_rules: ArmyRules
) -> void:
	id = p_id
	side = p_side
	unit = p_unit
	cell = p_cell
	lane = p_cell.x
	var def := p_unit.def
	max_hp = p_unit.max_hp(army_rules)
	hp = max_hp
	damage = p_unit.damage(army_rules)
	damage_type = def.damage_type
	attack_interval = def.attack_interval
	attack_range = def.attack_range
	move_speed = def.move_speed
	armor = def.armor
	ward = def.ward


func is_alive() -> bool:
	return hp > 0


func is_enemy_of(other: Combatant) -> bool:
	return other.side != side
