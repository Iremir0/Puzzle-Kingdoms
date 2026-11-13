class_name UnitDef
extends Resource
## Static definition of a unit class (player or enemy) at tier 1.
## Higher tiers are derived with ArmyRules.tier_multipliers.

enum DamageType { PHYSICAL, MAGIC }

@export var id: StringName
@export var display_name: String
@export var max_hp: int = 100
@export var damage: int = 10
@export var damage_type: DamageType = DamageType.PHYSICAL
## Seconds between attacks.
@export var attack_interval: float = 1.0
## Attack range in battlefield cells.
@export var attack_range: float = 1.0
## Movement speed in cells per second.
@export var move_speed: float = 1.0
## Fraction of Physical damage blocked, in [0, 1).
@export_range(0.0, 0.95) var armor: float = 0.0
## Fraction of Magic damage blocked, in [0, 1).
@export_range(0.0, 0.95) var ward: float = 0.0
## Display color for grey-box views.
@export var color: Color = Color.WHITE
