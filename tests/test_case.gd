class_name TestCase
extends RefCounted
## Base class for headless unit tests. See tests/run_tests.gd.
## Every method whose name starts with "test_" is run on a fresh instance.

var failures: PackedStringArray = []


func assert_true(condition: bool, message: String = "expected true") -> void:
	if not condition:
		failures.append(message)


func assert_false(condition: bool, message: String = "expected false") -> void:
	if condition:
		failures.append(message)


func assert_eq(actual: Variant, expected: Variant, message: String = "") -> void:
	if typeof(actual) != typeof(expected) or actual != expected:
		failures.append("%s expected <%s> but got <%s>" % [message, str(expected), str(actual)])


func assert_ne(actual: Variant, unexpected: Variant, message: String = "") -> void:
	if typeof(actual) == typeof(unexpected) and actual == unexpected:
		failures.append("%s did not expect <%s>" % [message, str(actual)])
