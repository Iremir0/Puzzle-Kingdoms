extends TestCase


func _unit(id: StringName = &"warrior") -> ArmyUnit:
	return ArmyUnit.new(ArmyFixtures.unit_def(id), 1)


func test_new_formation_is_empty() -> void:
	var formation := Formation.new()
	assert_eq(formation.count(), 0, "count")
	assert_eq(formation.empty_cells().size(), 9, "nine empty cells")
	assert_true(formation.get_unit(Vector2i(1, 1)) == null, "center empty")


func test_place_and_remove() -> void:
	var formation := Formation.new()
	var unit := _unit()
	assert_true(formation.place(unit, Vector2i(2, 0)) == null, "nothing displaced")
	assert_true(formation.get_unit(Vector2i(2, 0)) == unit, "unit placed")
	assert_eq(formation.find_cell(unit), Vector2i(2, 0), "find cell")
	assert_true(formation.remove(Vector2i(2, 0)) == unit, "remove returns unit")
	assert_eq(formation.count(), 0, "empty again")
	assert_eq(formation.find_cell(unit), Formation.NO_CELL, "no longer found")


func test_place_on_occupied_cell_returns_displaced_unit() -> void:
	var formation := Formation.new()
	var first := _unit()
	var second := _unit(&"archer")
	formation.place(first, Vector2i(0, 0))
	assert_true(formation.place(second, Vector2i(0, 0)) == first, "first displaced")
	assert_true(formation.get_unit(Vector2i(0, 0)) == second, "second placed")
	assert_eq(formation.count(), 1, "one unit")


func test_move_swaps_cells() -> void:
	var formation := Formation.new()
	var a := _unit()
	var b := _unit(&"archer")
	formation.place(a, Vector2i(0, 0))
	formation.place(b, Vector2i(0, 2))
	formation.move(Vector2i(0, 0), Vector2i(0, 2))
	assert_true(formation.get_unit(Vector2i(0, 2)) == a, "a moved back")
	assert_true(formation.get_unit(Vector2i(0, 0)) == b, "b moved front")
	formation.move(Vector2i(0, 0), Vector2i(1, 1))
	assert_true(formation.get_unit(Vector2i(1, 1)) == b, "move into empty cell")
	assert_true(formation.get_unit(Vector2i(0, 0)) == null, "source emptied")


func test_cells_outside_grid_are_rejected() -> void:
	var formation := Formation.new()
	assert_false(formation.is_inside(Vector2i(3, 0)), "x too large")
	assert_false(formation.is_inside(Vector2i(0, -1)), "y negative")
	assert_true(formation.is_inside(Vector2i(2, 2)), "corner inside")


func test_units_listed_front_to_back_left_to_right() -> void:
	var formation := Formation.new()
	var back := _unit()
	var front_right := _unit()
	var front_left := _unit()
	formation.place(back, Vector2i(0, 2))
	formation.place(front_right, Vector2i(2, 0))
	formation.place(front_left, Vector2i(0, 0))
	var units := formation.units()
	assert_eq(units.size(), 3, "three units")
	assert_true(units[0] == front_left and units[1] == front_right and units[2] == back, "order")
