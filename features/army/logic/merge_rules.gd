class_name MergeRules
extends RefCounted
## Decides which units a merge consumes (docs/GDD.md §6):
## `rules.merge_count` identical units (class + tier) become one unit of the
## next tier, up to `rules.max_tier()`.


## Returns the units consumed when merging into `target`, target first, or an
## empty array if the merge is not possible. `candidates` is searched in
## order, so callers list the units they would rather give up first.
static func find_merge_set(
		target: ArmyUnit, candidates: Array[ArmyUnit], rules: ArmyRules
) -> Array[ArmyUnit]:
	var result: Array[ArmyUnit] = []
	if target == null or target.tier >= rules.max_tier():
		return result
	result.append(target)
	for unit in candidates:
		if result.size() == rules.merge_count:
			break
		if unit != target and target.is_same_kind(unit):
			result.append(unit)
	if result.size() < rules.merge_count:
		result.clear()
	return result
