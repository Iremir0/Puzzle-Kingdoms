extends SceneTree
## Minimal headless test runner.
## Usage: godot --headless --path . -s tests/run_tests.gd
## Exit code is 0 when all tests pass, 1 otherwise.

const TEST_ROOT := "res://tests"


func _init() -> void:
	var files := _find_test_files(TEST_ROOT)
	files.sort()
	var passed := 0
	var failed := 0

	for path in files:
		var script: GDScript = load(path)
		if script == null:
			printerr("FAIL  could not load %s" % path)
			failed += 1
			continue
		for method in script.get_script_method_list():
			var name: String = method["name"]
			if not name.begins_with("test_"):
				continue
			var test_case = script.new()
			test_case.call(name)
			if test_case.failures.is_empty():
				passed += 1
			else:
				failed += 1
				printerr("FAIL  %s :: %s" % [path.get_file(), name])
				for failure in test_case.failures:
					printerr("        %s" % failure)

	print("\n%d passed, %d failed" % [passed, failed])
	quit(1 if failed > 0 else 0)


func _find_test_files(dir_path: String) -> PackedStringArray:
	var result: PackedStringArray = []
	var dir := DirAccess.open(dir_path)
	if dir == null:
		return result
	for sub_dir in dir.get_directories():
		result.append_array(_find_test_files(dir_path.path_join(sub_dir)))
	for file in dir.get_files():
		if file.begins_with("test_") and file.ends_with(".gd") and file != "test_case.gd":
			result.append(dir_path.path_join(file))
	return result
