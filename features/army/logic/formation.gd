class_name Formation
extends RefCounted
## The 3×3 battle formation.
##
## Cells are Vector2i(x, y): x = lane (0 left, 1 center, 2 right),
## y = row (0 front, 1 middle, 2 back). The front row faces the enemy.

const SIZE := 3
const NO_CELL := Vector2i(-1, -1)

var _slots: Array[ArmyUnit] = []


func _init() -> void:
	_slots.resize(SIZE * SIZE)


## Builds an enemy formation from a stage's slots.
static func from_enemy_slots(slots: Array[EnemySlot]) -> Formation:
	var result := Formation.new()
	for slot in slots:
		result.place(ArmyUnit.new(slot.unit, slot.tier), slot.cell)
	return result


func is_inside(cell: Vector2i) -> bool:
	return cell.x >= 0 and cell.x < SIZE and cell.y >= 0 and cell.y < SIZE


func get_unit(cell: Vector2i) -> ArmyUnit:
	return _slots[_index(cell)]


## Puts a unit on a cell and returns the unit that was there (or null).
func place(unit: ArmyUnit, cell: Vector2i) -> ArmyUnit:
	var index := _index(cell)
	var displaced := _slots[index]
	_slots[index] = unit
	return displaced


## Empties a cell and returns the unit that was there (or null).
func remove(cell: Vector2i) -> ArmyUnit:
	return place(null, cell)


## Swaps the contents of two cells (either may be empty).
func move(from: Vector2i, to: Vector2i) -> void:
	var a := _index(from)
	var b := _index(to)
	var tmp := _slots[a]
	_slots[a] = _slots[b]
	_slots[b] = tmp


func find_cell(unit: ArmyUnit) -> Vector2i:
	var index := _slots.find(unit) if unit != null else -1
	if index == -1:
		return NO_CELL
	return Vector2i(index % SIZE, index / SIZE)


func has_unit(unit: ArmyUnit) -> bool:
	return find_cell(unit) != NO_CELL


## Deployed units ordered front to back, left to right.
func units() -> Array[ArmyUnit]:
	var result: Array[ArmyUnit] = []
	for unit in _slots:
		if unit != null:
			result.append(unit)
	return result


func count() -> int:
	return units().size()


func empty_cells() -> Array[Vector2i]:
	var result: Array[Vector2i] = []
	for index in _slots.size():
		if _slots[index] == null:
			result.append(Vector2i(index % SIZE, index / SIZE))
	return result


func _index(cell: Vector2i) -> int:
	assert(is_inside(cell), "Cell outside formation: %s" % cell)
	return cell.y * SIZE + cell.x
