class_name EnemySlot
extends Resource
## One enemy placed in a stage's 3×3 enemy formation.

@export var unit: UnitDef
@export_range(1, 3) var tier: int = 1
## Formation cell: x = lane (0 left … 2 right), y = row (0 front … 2 back).
@export var cell: Vector2i = Vector2i.ZERO
