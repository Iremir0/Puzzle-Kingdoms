class_name Damage
extends RefCounted
## Damage formula (docs/GDD.md §7): Physical is reduced by Armor, Magic by
## Ward. Results are whole numbers and at least 1.


static func compute(
		base_damage: int, damage_type: UnitDef.DamageType, target: Combatant, multiplier: float
) -> int:
	var mitigation := target.armor
	if damage_type == UnitDef.DamageType.MAGIC:
		mitigation = target.ward
	return maxi(1, roundi(base_damage * multiplier * (1.0 - mitigation)))
