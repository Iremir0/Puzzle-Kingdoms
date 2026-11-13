class_name StageDef
extends Resource
## One campaign stage: puzzle settings, enemy army and rewards.

const NO_SEED := 0

@export var id: StringName
@export var display_name: String
@export var moves: int = 12
## Fixed board seed for reproducible stages; NO_SEED means random.
@export var board_seed: int = NO_SEED
@export var enemies: Array[EnemySlot] = []
@export var gold_reward: int = 0
