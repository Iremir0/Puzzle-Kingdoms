class_name Roster
extends RefCounted
## Every unit recruited during one stage: the unlimited bench plus the 3×3
## formation. A unit is always in exactly one of the two.

signal changed

var rules: ArmyRules
var bench: Array[ArmyUnit] = []
var formation := Formation.new()

var _next_uid: int = 0


func _init(p_rules: ArmyRules) -> void:
	assert(p_rules != null, "Roster needs ArmyRules")
	rules = p_rules


## Adds `count` new tier 1 units to the bench and returns them.
func recruit(def: UnitDef, count: int = 1) -> Array[ArmyUnit]:
	var added: Array[ArmyUnit] = []
	for i in count:
		var unit := _new_unit(def, 1)
		bench.append(unit)
		added.append(unit)
	if count > 0:
		changed.emit()
	return added


func all_units() -> Array[ArmyUnit]:
	var result := formation.units()
	result.append_array(bench)
	return result


func owns(unit: ArmyUnit) -> bool:
	return bench.has(unit) or formation.has_unit(unit)


## Places a bench or deployed unit on `cell`. A unit already on that cell goes
## to the bench if `unit` came from the bench, or swaps cells with it if
## `unit` was deployed. Returns false if nothing changed.
func deploy(unit: ArmyUnit, cell: Vector2i) -> bool:
	if not owns(unit) or not formation.is_inside(cell):
		return false
	var from := formation.find_cell(unit)
	if from != Formation.NO_CELL:
		formation.move(from, cell)
	else:
		bench.erase(unit)
		var displaced := formation.place(unit, cell)
		if displaced != null:
			bench.append(displaced)
	changed.emit()
	return true


## Moves the unit on `cell` back to the bench. Returns false if it was empty.
func undeploy(cell: Vector2i) -> bool:
	if not formation.is_inside(cell) or formation.get_unit(cell) == null:
		return false
	bench.append(formation.remove(cell))
	changed.emit()
	return true


func can_merge(target: ArmyUnit) -> bool:
	return owns(target) and not _merge_set(target).is_empty()


## Merges `target` with identical units, preferring bench copies so the
## formation stays intact. The new unit takes the target's place (its cell, or
## the bench). Returns the new unit, or null if the merge is not possible.
func merge(target: ArmyUnit) -> ArmyUnit:
	if not owns(target):
		return null
	var consumed := _merge_set(target)
	if consumed.is_empty():
		return null

	var merged := _new_unit(target.def, target.tier + 1)
	var target_cell := formation.find_cell(target)
	for unit in consumed:
		var cell := formation.find_cell(unit)
		if cell != Formation.NO_CELL:
			formation.remove(cell)
		else:
			bench.erase(unit)
	if target_cell != Formation.NO_CELL:
		formation.place(merged, target_cell)
	else:
		bench.append(merged)
	changed.emit()
	return merged


func _merge_set(target: ArmyUnit) -> Array[ArmyUnit]:
	var candidates: Array[ArmyUnit] = bench.duplicate()
	candidates.append_array(formation.units())
	return MergeRules.find_merge_set(target, candidates, rules)


func _new_unit(def: UnitDef, tier: int) -> ArmyUnit:
	var unit := ArmyUnit.new(def, tier, _next_uid)
	_next_uid += 1
	return unit
