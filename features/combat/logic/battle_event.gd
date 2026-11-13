class_name BattleEvent
extends RefCounted
## Something that happened during one simulator tick, for views to play back.

enum Type {
	ATTACK,         ## source hit target for `amount` damage
	DIED,           ## source died
	RALLY_STARTED,
	RALLY_ENDED,
	FINISHED,       ## battle over; read BattleSimulator.result
}

var type: Type
var tick: int
var source_id: int = Combatant.NO_TARGET
var target_id: int = Combatant.NO_TARGET
var amount: int = 0


func _init(
		p_type: Type,
		p_tick: int,
		p_source_id: int = Combatant.NO_TARGET,
		p_target_id: int = Combatant.NO_TARGET,
		p_amount: int = 0
) -> void:
	type = p_type
	tick = p_tick
	source_id = p_source_id
	target_id = p_target_id
	amount = p_amount
