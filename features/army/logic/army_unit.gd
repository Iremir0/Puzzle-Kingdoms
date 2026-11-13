class_name ArmyUnit
extends RefCounted
## One recruited unit: a class definition plus a tier.
## Combat-time state (current hp, position) lives in the combat feature.

const NO_UID := -1

var def: UnitDef
var tier: int
## Unique within a Roster; lets views track a unit across moves and merges.
var uid: int = NO_UID


func _init(p_def: UnitDef, p_tier: int = 1, p_uid: int = NO_UID) -> void:
	assert(p_def != null, "ArmyUnit needs a UnitDef")
	def = p_def
	tier = p_tier
	uid = p_uid


func max_hp(rules: ArmyRules) -> int:
	return roundi(def.max_hp * rules.tier_multiplier(tier))


func damage(rules: ArmyRules) -> int:
	return roundi(def.damage * rules.tier_multiplier(tier))


## True when both units share class and tier, i.e. they can merge together.
func is_same_kind(other: ArmyUnit) -> bool:
	return other != null and other.def == def and other.tier == tier
