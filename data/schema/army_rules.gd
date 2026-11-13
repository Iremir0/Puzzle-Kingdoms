class_name ArmyRules
extends Resource
## Global War Camp balancing numbers (see docs/GDD.md §6).

## HP and damage multiplier per tier; index 0 is tier 1.
## The array size defines the maximum tier.
@export var tier_multipliers: PackedFloat32Array = PackedFloat32Array([1.0, 1.8, 3.2])
## Identical units (same class and tier) consumed by one merge.
@export var merge_count: int = 3


func max_tier() -> int:
	return tier_multipliers.size()


func tier_multiplier(tier: int) -> float:
	assert(tier >= 1 and tier <= max_tier(), "Tier out of range: %d" % tier)
	return tier_multipliers[tier - 1]
