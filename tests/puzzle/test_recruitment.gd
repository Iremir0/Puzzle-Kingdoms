extends TestCase


func test_three_points_make_one_reward() -> void:
	var meter := RecruitmentMeter.new(3)
	assert_eq(meter.add_points(3), 1, "earned")
	assert_eq(meter.rewards, 1, "rewards")
	assert_eq(meter.points, 0, "remainder")


func test_remainder_carries_over() -> void:
	var meter := RecruitmentMeter.new(3)
	assert_eq(meter.add_points(4), 1, "first add")
	assert_eq(meter.points, 1, "remainder")
	assert_eq(meter.add_points(2), 1, "second add completes a reward")
	assert_eq(meter.rewards, 2, "total")


func test_cap_sends_points_to_overflow() -> void:
	var meter := RecruitmentMeter.new(3, 1)
	assert_eq(meter.add_points(5), 1, "only one reward")
	assert_true(meter.is_full(), "full")
	assert_eq(meter.overflow_points, 2, "overflow")


func _make_session(moves: int) -> PuzzleSession:
	var rng := RandomNumberGenerator.new()
	rng.seed = 21
	var board := BoardModel.new(6, 6, 4, rng)
	board.generate()
	var meters := {
		"warrior": RecruitmentMeter.new(3),
		"archer": RecruitmentMeter.new(3),
		"mage": RecruitmentMeter.new(3),
		"rally": RecruitmentMeter.new(3, 3),
	}
	return PuzzleSession.new(board, moves,
			PackedStringArray(["warrior", "archer", "mage", "rally"]), meters)


func test_session_spends_moves_only_on_valid_swaps() -> void:
	var session := _make_session(2)
	var invalid := session.play_swap(Vector2i(0, 0), Vector2i(3, 3))
	assert_false(invalid.is_valid, "invalid swap")
	assert_eq(session.moves_left, 2, "no move spent")

	var move := session.board.find_valid_move()
	assert_true(session.play_swap(move[0], move[1]).is_valid, "valid swap")
	assert_eq(session.moves_left, 1, "one move spent")


func test_session_finishes_and_rejects_further_swaps() -> void:
	var session := _make_session(1)
	var move := session.board.find_valid_move()
	session.play_swap(move[0], move[1])
	assert_true(session.is_finished(), "finished")
	move = session.board.find_valid_move()
	assert_false(session.play_swap(move[0], move[1]).is_valid, "no swaps after finish")


func test_session_rewards_match_cleared_gems() -> void:
	var session := _make_session(12)
	var total_cleared := {}
	while not session.is_finished():
		var move := session.board.find_valid_move()
		var result := session.play_swap(move[0], move[1])
		for gem_type in result.cleared_by_type:
			total_cleared[gem_type] = total_cleared.get(gem_type, 0) + result.cleared_by_type[gem_type]
	var keys := ["warrior", "archer", "mage"]
	for gem_type in 3:
		var cleared: int = total_cleared.get(gem_type, 0)
		assert_eq(session.reward_count(keys[gem_type]), cleared / 3,
				"%s units" % keys[gem_type])
