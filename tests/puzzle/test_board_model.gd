extends TestCase

## Diagonal pattern (x + y) % 4: contains no match and no valid move.
const DEADLOCK_ROWS := [
	[0, 1, 2, 3, 0, 1],
	[1, 2, 3, 0, 1, 2],
	[2, 3, 0, 1, 2, 3],
	[3, 0, 1, 2, 3, 0],
	[0, 1, 2, 3, 0, 1],
	[1, 2, 3, 0, 1, 2],
]


func _make_board(seed_value: int = 1) -> BoardModel:
	var rng := RandomNumberGenerator.new()
	rng.seed = seed_value
	return BoardModel.new(6, 6, 4, rng)


func _rows_with(changes: Dictionary) -> Array:
	var rows := DEADLOCK_ROWS.duplicate(true)
	for cell: Vector2i in changes:
		rows[cell.y][cell.x] = changes[cell]
	return rows


func _count_types(rows: Array) -> Dictionary:
	var counts := {}
	for row in rows:
		for gem_type in row:
			counts[gem_type] = counts.get(gem_type, 0) + 1
	return counts


func _is_full(board: BoardModel) -> bool:
	for row in board.to_rows():
		for gem_type in row:
			if gem_type == BoardModel.EMPTY:
				return false
	return true


func test_generate_has_no_matches_and_a_valid_move() -> void:
	for seed_value in range(1, 51):
		var board := _make_board(seed_value)
		board.generate()
		assert_true(_is_full(board), "board full (seed %d)" % seed_value)
		assert_true(board.find_match_groups().is_empty(), "no matches (seed %d)" % seed_value)
		assert_true(board.has_valid_move(), "has move (seed %d)" % seed_value)


func test_generate_is_deterministic_for_a_seed() -> void:
	var a := _make_board(42)
	var b := _make_board(42)
	a.generate()
	b.generate()
	assert_eq(a.to_rows(), b.to_rows(), "same seed, same board")


func test_deadlock_pattern_has_no_move() -> void:
	var board := _make_board()
	board.load_rows(DEADLOCK_ROWS)
	assert_true(board.find_match_groups().is_empty(), "no matches")
	assert_false(board.has_valid_move(), "no valid move")


func test_l_shape_forms_single_group() -> void:
	var board := _make_board()
	board.load_rows(_rows_with({
		Vector2i(1, 0): 0, Vector2i(2, 0): 0,
		Vector2i(0, 1): 0, Vector2i(0, 2): 0,
	}))
	var groups := board.find_match_groups()
	assert_eq(groups.size(), 1, "group count")
	assert_eq(groups[0].gem_type, 0, "group type")
	assert_eq(groups[0].size(), 5, "L shape size")


func test_separate_matches_form_separate_groups() -> void:
	var board := _make_board()
	board.load_rows(_rows_with({
		Vector2i(5, 0): 0, Vector2i(5, 1): 0, Vector2i(5, 2): 0, Vector2i(5, 3): 0,
		Vector2i(0, 5): 1, Vector2i(1, 5): 1, Vector2i(2, 5): 1,
	}))
	var groups := board.find_match_groups()
	assert_eq(groups.size(), 2, "group count")
	var sizes_by_type := {}
	for group in groups:
		sizes_by_type[group.gem_type] = group.size()
	assert_eq(sizes_by_type, {0: 4, 1: 3}, "sizes by type")


func test_swap_rejected_when_not_adjacent_or_no_match() -> void:
	var board := _make_board()
	board.load_rows(DEADLOCK_ROWS)
	var before := board.to_rows()
	assert_false(board.try_swap(Vector2i(0, 0), Vector2i(2, 0)).is_valid, "not adjacent")
	assert_false(board.try_swap(Vector2i(0, 0), Vector2i(1, 0)).is_valid, "no match")
	assert_false(board.try_swap(Vector2i(5, 5), Vector2i(6, 5)).is_valid, "outside board")
	assert_eq(board.to_rows(), before, "board unchanged")


func test_valid_swap_clears_match_and_refills() -> void:
	var board := _make_board(7)
	# Swapping (2,0) and (2,1) completes 0-0-0 on row 0.
	board.load_rows(_rows_with({Vector2i(1, 0): 0, Vector2i(2, 1): 0, Vector2i(2, 0): 2}))
	assert_true(board.find_match_groups().is_empty(), "precondition: no match")

	var result := board.try_swap(Vector2i(2, 0), Vector2i(2, 1))
	assert_true(result.is_valid, "swap valid")
	assert_true(result.steps.size() >= 1, "at least one step")

	var first: MoveResult.CascadeStep = result.steps[0]
	assert_eq(first.groups.size(), 1, "one group")
	assert_eq(first.groups[0].size(), 3, "three gems")
	assert_eq(first.falls.size(), 0, "nothing above row 0 to fall")
	assert_eq(first.spawns.size(), 3, "three spawns")
	assert_true(result.cleared_count(0) >= 3, "cleared type 0")

	assert_true(_is_full(board), "board full after move")
	assert_true(board.find_match_groups().is_empty(), "board stable after move")


func test_gravity_moves_gems_down() -> void:
	var board := _make_board(3)
	# Swapping (2,2) and (2,3) completes 0-0-0 on row 2; rows 0-1 fall by one.
	board.load_rows(_rows_with({
		Vector2i(0, 2): 0, Vector2i(1, 2): 0, Vector2i(2, 2): 1, Vector2i(3, 2): 1,
		Vector2i(2, 3): 0,
	}))
	assert_true(board.find_match_groups().is_empty(), "precondition: no match")

	var result := board.try_swap(Vector2i(2, 2), Vector2i(2, 3))
	assert_true(result.is_valid, "swap valid")
	var first: MoveResult.CascadeStep = result.steps[0]

	var moves := {}
	for fall in first.falls:
		moves[fall.from] = fall.to
	var expected := {}
	for x in 3:
		expected[Vector2i(x, 1)] = Vector2i(x, 2)
		expected[Vector2i(x, 0)] = Vector2i(x, 1)
	assert_eq(moves, expected, "falls")

	assert_eq(first.spawns.size(), 3, "spawn count")
	for spawn in first.spawns:
		assert_eq(spawn.cell.y, 0, "spawn on top row")
		assert_eq(spawn.drop_distance, 1, "drop distance")


func test_same_seed_same_move_same_outcome() -> void:
	var rows := _rows_with({Vector2i(1, 0): 0, Vector2i(2, 1): 0, Vector2i(2, 0): 2})
	var a := _make_board(99)
	var b := _make_board(99)
	a.load_rows(rows)
	b.load_rows(rows)
	var ra := a.try_swap(Vector2i(2, 0), Vector2i(2, 1))
	var rb := b.try_swap(Vector2i(2, 0), Vector2i(2, 1))
	assert_eq(ra.cleared_by_type, rb.cleared_by_type, "same clears")
	assert_eq(a.to_rows(), b.to_rows(), "same board")


func test_shuffle_resolves_deadlock_and_keeps_gems() -> void:
	var board := _make_board(5)
	board.load_rows(DEADLOCK_ROWS)
	board.shuffle()
	assert_true(board.find_match_groups().is_empty(), "no matches after shuffle")
	assert_true(board.has_valid_move(), "has move after shuffle")
	assert_eq(_count_types(board.to_rows()), _count_types(DEADLOCK_ROWS), "same gems")


func test_moves_always_leave_a_playable_board() -> void:
	# Plays many random valid moves and checks the board invariants each time.
	var board := _make_board(11)
	board.generate()
	for i in 200:
		var move := board.find_valid_move()
		assert_eq(move.size(), 2, "move available (iteration %d)" % i)
		if move.size() != 2:
			return
		var result := board.try_swap(move[0], move[1])
		assert_true(result.is_valid, "valid move accepted")
		assert_true(_is_full(board), "full")
		assert_true(board.find_match_groups().is_empty(), "stable")
