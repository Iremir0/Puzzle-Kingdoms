class_name BoardModel
extends RefCounted
## Pure Match-3 board rules: no nodes, no animation, seeded randomness.
##
## Coordinates are Vector2i(x, y) with (0, 0) at the top-left; gems fall
## towards larger y. Gem types are plain ints in [0, gem_type_count); the
## meaning of each type is defined by the caller (see PuzzleSession).

const EMPTY := -1
const MIN_MATCH := 3
const MAX_GENERATION_ATTEMPTS := 100
const MAX_CASCADES := 100

var width: int
var height: int
var gem_type_count: int

var _cells: PackedInt32Array
var _rng: RandomNumberGenerator


func _init(
		p_width: int = 6,
		p_height: int = 6,
		p_gem_type_count: int = 4,
		rng: RandomNumberGenerator = null
) -> void:
	assert(p_width >= MIN_MATCH and p_height >= MIN_MATCH, "Board too small")
	assert(p_gem_type_count >= 3, "Need at least 3 gem types to avoid forced matches")
	width = p_width
	height = p_height
	gem_type_count = p_gem_type_count
	_rng = rng
	if _rng == null:
		_rng = RandomNumberGenerator.new()
		_rng.randomize()
	_cells = PackedInt32Array()
	_cells.resize(width * height)
	_cells.fill(EMPTY)


# --- Queries -----------------------------------------------------------------

func is_inside(cell: Vector2i) -> bool:
	return cell.x >= 0 and cell.x < width and cell.y >= 0 and cell.y < height


func get_gem(cell: Vector2i) -> int:
	return _cells[_index(cell)]


func are_adjacent(a: Vector2i, b: Vector2i) -> bool:
	return absi(a.x - b.x) + absi(a.y - b.y) == 1


## Returns the board as rows: rows[y][x] = gem type.
func to_rows() -> Array:
	var rows := []
	for y in height:
		var row := []
		for x in width:
			row.append(get_gem(Vector2i(x, y)))
		rows.append(row)
	return rows


## True if swapping a and b would create at least one match.
func would_swap_match(a: Vector2i, b: Vector2i) -> bool:
	if not (is_inside(a) and is_inside(b) and are_adjacent(a, b)):
		return false
	if get_gem(a) == get_gem(b):
		return false
	_swap(a, b)
	var result := _has_match_at(a) or _has_match_at(b)
	_swap(a, b)
	return result


## Returns one valid swap as [a, b], or an empty array when the board is stuck.
func find_valid_move() -> Array[Vector2i]:
	for y in height:
		for x in width:
			var cell := Vector2i(x, y)
			for offset: Vector2i in [Vector2i.RIGHT, Vector2i.DOWN]:
				var other := cell + offset
				if would_swap_match(cell, other):
					return [cell, other]
	return []


func has_valid_move() -> bool:
	return not find_valid_move().is_empty()


## Finds every match currently on the board, grouped by connected shape.
func find_match_groups() -> Array[MatchGroup]:
	# Union-find over cell indices; -1 means "not part of any match".
	var parent := PackedInt32Array()
	parent.resize(width * height)
	parent.fill(-1)

	for y in height:
		_mark_runs(parent, Vector2i(0, y), Vector2i.RIGHT, width)
	for x in width:
		_mark_runs(parent, Vector2i(x, 0), Vector2i.DOWN, height)

	var groups_by_root := {}
	var groups: Array[MatchGroup] = []
	for y in height:
		for x in width:
			var index := y * width + x
			if parent[index] == -1:
				continue
			var root := _find_root(parent, index)
			if not groups_by_root.has(root):
				var group := MatchGroup.new(_cells[index])
				groups_by_root[root] = group
				groups.append(group)
			groups_by_root[root].cells.append(Vector2i(x, y))
	return groups


# --- Commands ----------------------------------------------------------------

## Fills the board randomly with no ready-made matches and at least one move.
func generate() -> void:
	for attempt in MAX_GENERATION_ATTEMPTS:
		for y in height:
			for x in width:
				_cells[y * width + x] = _pick_type_without_match(x, y)
		if has_valid_move():
			return
	push_warning("BoardModel.generate: could not guarantee a valid move")


## Replaces the board content. rows[y][x] = gem type. Intended for tests,
## tutorials and handcrafted boards. Does not resolve existing matches.
func load_rows(rows: Array) -> void:
	assert(rows.size() == height, "Row count mismatch")
	for y in height:
		assert(rows[y].size() == width, "Column count mismatch")
		for x in width:
			_cells[y * width + x] = rows[y][x]


## Attempts a player swap and fully resolves all cascades.
## Returns a MoveResult with is_valid == false (board untouched) when the swap
## is not allowed or creates no match.
func try_swap(a: Vector2i, b: Vector2i) -> MoveResult:
	var result := MoveResult.new()
	result.swap_a = a
	result.swap_b = b
	if not would_swap_match(a, b):
		return result

	result.is_valid = true
	_swap(a, b)

	for cascade in MAX_CASCADES:
		var groups := find_match_groups()
		if groups.is_empty():
			break
		var step := MoveResult.CascadeStep.new()
		step.groups = groups
		for group in groups:
			for cell in group.cells:
				_cells[_index(cell)] = EMPTY
			result.cleared_by_type[group.gem_type] = (
					result.cleared_by_type.get(group.gem_type, 0) + group.size())
		step.falls = _apply_gravity()
		step.spawns = _refill()
		result.steps.append(step)

	if not has_valid_move():
		shuffle()
		result.reshuffled = true
		result.rows_after_reshuffle = to_rows()
	return result


## Rearranges the existing gems so the board has no matches and a valid move.
## Falls back to a fresh board if no such arrangement is found quickly.
func shuffle() -> void:
	for attempt in MAX_GENERATION_ATTEMPTS:
		for i in range(_cells.size() - 1, 0, -1):
			var j := _rng.randi_range(0, i)
			var tmp := _cells[i]
			_cells[i] = _cells[j]
			_cells[j] = tmp
		if find_match_groups().is_empty() and has_valid_move():
			return
	generate()


# --- Internals ---------------------------------------------------------------

func _index(cell: Vector2i) -> int:
	assert(is_inside(cell), "Cell outside board: %s" % cell)
	return cell.y * width + cell.x


func _swap(a: Vector2i, b: Vector2i) -> void:
	var ia := _index(a)
	var ib := _index(b)
	var tmp := _cells[ia]
	_cells[ia] = _cells[ib]
	_cells[ib] = tmp


func _pick_type_without_match(x: int, y: int) -> int:
	var options: Array[int] = []
	for gem_type in gem_type_count:
		if x >= 2 and _cells[y * width + x - 1] == gem_type \
				and _cells[y * width + x - 2] == gem_type:
			continue
		if y >= 2 and _cells[(y - 1) * width + x] == gem_type \
				and _cells[(y - 2) * width + x] == gem_type:
			continue
		options.append(gem_type)
	return options[_rng.randi_range(0, options.size() - 1)]


func _has_match_at(cell: Vector2i) -> bool:
	var gem_type := get_gem(cell)
	if gem_type == EMPTY:
		return false
	var horizontal := 1 + _count_same(cell, Vector2i.LEFT, gem_type) \
			+ _count_same(cell, Vector2i.RIGHT, gem_type)
	if horizontal >= MIN_MATCH:
		return true
	var vertical := 1 + _count_same(cell, Vector2i.UP, gem_type) \
			+ _count_same(cell, Vector2i.DOWN, gem_type)
	return vertical >= MIN_MATCH


func _count_same(start: Vector2i, direction: Vector2i, gem_type: int) -> int:
	var count := 0
	var cell := start + direction
	while is_inside(cell) and get_gem(cell) == gem_type:
		count += 1
		cell += direction
	return count


## Scans one line and unions the cells of every run of MIN_MATCH or more.
func _mark_runs(parent: PackedInt32Array, start: Vector2i, direction: Vector2i, length: int) -> void:
	var run_start := 0
	for i in range(1, length + 1):
		var run_continues := i < length \
				and get_gem(start + direction * i) != EMPTY \
				and get_gem(start + direction * i) == get_gem(start + direction * run_start)
		if run_continues:
			continue
		var run_length := i - run_start
		if run_length >= MIN_MATCH and get_gem(start + direction * run_start) != EMPTY:
			var first := _index(start + direction * run_start)
			if parent[first] == -1:
				parent[first] = first
			for k in range(run_start + 1, i):
				var index := _index(start + direction * k)
				if parent[index] == -1:
					parent[index] = index
				_union(parent, first, index)
		run_start = i


func _find_root(parent: PackedInt32Array, index: int) -> int:
	while parent[index] != index:
		parent[index] = parent[parent[index]]
		index = parent[index]
	return index


func _union(parent: PackedInt32Array, a: int, b: int) -> void:
	var root_a := _find_root(parent, a)
	var root_b := _find_root(parent, b)
	if root_a != root_b:
		parent[root_b] = root_a


## Moves gems down into empty cells. Returns the moves in column order.
func _apply_gravity() -> Array[MoveResult.GemFall]:
	var falls: Array[MoveResult.GemFall] = []
	for x in width:
		var write_y := height - 1
		for y in range(height - 1, -1, -1):
			var gem_type := _cells[y * width + x]
			if gem_type == EMPTY:
				continue
			if y != write_y:
				_cells[write_y * width + x] = gem_type
				_cells[y * width + x] = EMPTY
				falls.append(MoveResult.GemFall.new(Vector2i(x, y), Vector2i(x, write_y)))
			write_y -= 1
	return falls


## Fills empty cells (all at the top after gravity) with random gems.
func _refill() -> Array[MoveResult.GemSpawn]:
	var spawns: Array[MoveResult.GemSpawn] = []
	for x in width:
		var empty_count := 0
		while empty_count < height and _cells[empty_count * width + x] == EMPTY:
			empty_count += 1
		for y in empty_count:
			var gem_type := _rng.randi_range(0, gem_type_count - 1)
			_cells[y * width + x] = gem_type
			spawns.append(MoveResult.GemSpawn.new(Vector2i(x, y), gem_type, empty_count))
	return spawns
