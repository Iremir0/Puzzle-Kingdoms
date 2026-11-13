class_name MatchGroup
extends RefCounted
## A connected set of same-type gems cleared together.
## Horizontal and vertical runs that share a cell (L/T shapes) form one group.

var gem_type: int
var cells: Array[Vector2i] = []


func _init(p_gem_type: int = -1) -> void:
	gem_type = p_gem_type


func size() -> int:
	return cells.size()
