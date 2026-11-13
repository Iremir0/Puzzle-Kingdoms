extends TestCase


func test_activation_consumes_a_charge() -> void:
	var rally := Rally.new(2, 100)
	assert_true(rally.activate(), "activated")
	assert_eq(rally.charges, 1, "one left")
	assert_true(rally.is_active(), "active")


func test_cannot_stack_activations() -> void:
	var rally := Rally.new(2, 100)
	rally.activate()
	assert_false(rally.activate(), "already active")
	assert_eq(rally.charges, 1, "charge kept")


func test_cannot_activate_without_charges() -> void:
	var rally := Rally.new(0, 100)
	assert_false(rally.can_activate(), "no charges")
	assert_false(rally.activate(), "refused")


func test_expires_after_duration() -> void:
	var rally := Rally.new(1, 3)
	rally.activate()
	assert_false(rally.tick(), "tick 1")
	assert_false(rally.tick(), "tick 2")
	assert_true(rally.tick(), "ends on tick 3")
	assert_false(rally.is_active(), "inactive")
	assert_false(rally.tick(), "no further end events")
