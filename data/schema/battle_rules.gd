class_name BattleRules
extends Resource
## Battle simulation and Rally numbers (see docs/GDD.md §9).
## Distances are in battlefield cells.

@export var ticks_per_second: int = 20
## A battle still running after this long is a defeat.
@export var timeout_seconds: float = 60.0
## Distance between the two front rows at spawn.
@export var front_gap: float = 6.0
## Distance between formation rows of one side.
@export var row_spacing: float = 1.0
## Distance between lanes.
@export var lane_spacing: float = 1.2
## Targets this much farther than the nearest one still win if in the same lane.
@export var same_lane_tie_distance: float = 0.5
## Allies closer than this are pushed apart.
@export var separation_distance: float = 0.6
@export var rally_damage_bonus: float = 0.5
@export var rally_attack_speed_bonus: float = 0.5
@export var rally_duration_seconds: float = 5.0


func seconds_to_ticks(seconds: float) -> int:
	return roundi(seconds * ticks_per_second)
