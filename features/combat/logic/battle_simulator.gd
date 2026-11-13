class_name BattleSimulator
extends RefCounted
## Deterministic fixed-tick battle (docs/GDD.md §9, ARCHITECTURE.md).
##
## Battlefield space is measured in cells. The player side spawns at negative
## y, the enemy side at positive y; x = 0 is the center lane. Each `step()`
## advances one tick (1 / ticks_per_second s) in three phases:
##   1. every living unit, in id order, picks a target and either attacks
##      (if in range and ready) or moves towards it;
##   2. allies that overlap are pushed apart;
##   3. all hits of the tick land at once, so units killed this tick still
##      strike back (simultaneous resolution).
## A battle is reproducible from (armies, rules, input_log).

const EPSILON := 0.000001

var rules: BattleRules
var tick: int = 0
## Player units first (front to back, left to right), then enemies.
var combatants: Array[Combatant] = []
var rally: Rally
## Ticks at which the player activated Rally (before that tick's step).
var input_log := PackedInt32Array()
## Set when the battle is over.
var result: BattleResult = null

var _timeout_ticks: int
var _pending_events: Array[BattleEvent] = []


func _init(
		p_rules: BattleRules,
		army_rules: ArmyRules,
		player: Formation,
		enemy: Formation,
		rally_charges: int
) -> void:
	rules = p_rules
	_timeout_ticks = rules.seconds_to_ticks(rules.timeout_seconds)
	rally = Rally.new(rally_charges, rules.seconds_to_ticks(rules.rally_duration_seconds))
	_spawn_side(player, Combatant.PLAYER, army_rules)
	_spawn_side(enemy, Combatant.ENEMY, army_rules)


func is_finished() -> bool:
	return result != null


## Battlefield position of a formation cell for the given side.
func cell_to_position(side: int, cell: Vector2i) -> Vector2:
	var depth := rules.front_gap / 2.0 + cell.y * rules.row_spacing
	var x := (cell.x - 1) * rules.lane_spacing
	return Vector2(x, -depth if side == Combatant.PLAYER else depth)


func get_combatant(id: int) -> Combatant:
	if id < 0 or id >= combatants.size():
		return null
	return combatants[id]


func alive_count(side: int) -> int:
	var count := 0
	for c in combatants:
		if c.side == side and c.is_alive():
			count += 1
	return count


## Starts Rally before the next tick. Returns false if not possible.
func activate_rally() -> bool:
	if is_finished() or not rally.activate():
		return false
	input_log.append(tick)
	_pending_events.append(BattleEvent.new(BattleEvent.Type.RALLY_STARTED, tick + 1))
	return true


## Advances one tick and returns what happened, in playback order.
func step() -> Array[BattleEvent]:
	var events: Array[BattleEvent] = []
	if is_finished():
		return events
	events.append_array(_pending_events)
	_pending_events.clear()
	tick += 1

	var dt := 1.0 / rules.ticks_per_second
	var hits: Array[BattleEvent] = []
	for c in combatants:
		if c.is_alive():
			_act(c, dt, hits)
	_separate_allies()

	var died: Array[Combatant] = []
	for hit in hits:
		var target := combatants[hit.target_id]
		if target.is_alive():
			target.hp -= hit.amount
			if not target.is_alive():
				died.append(target)
		events.append(hit)
	for c in died:
		c.hp = 0
		events.append(BattleEvent.new(BattleEvent.Type.DIED, tick, c.id))

	if rally.tick():
		events.append(BattleEvent.new(BattleEvent.Type.RALLY_ENDED, tick))
	_check_finished(events)
	return events


## Runs the battle to its end. `rally_ticks` replays recorded Rally input.
func run_to_end(rally_ticks: PackedInt32Array = PackedInt32Array()) -> BattleResult:
	while not is_finished():
		if rally_ticks.has(tick):
			activate_rally()
		step()
	return result


func _spawn_side(formation: Formation, side: int, army_rules: ArmyRules) -> void:
	for y in Formation.SIZE:
		for x in Formation.SIZE:
			var cell := Vector2i(x, y)
			var unit := formation.get_unit(cell)
			if unit == null:
				continue
			var c := Combatant.new(combatants.size(), side, unit, cell, army_rules)
			c.position = cell_to_position(side, cell)
			combatants.append(c)


func _act(c: Combatant, dt: float, hits: Array[BattleEvent]) -> void:
	var target := _current_target(c)
	if target == null:
		return
	var buffed := c.side == Combatant.PLAYER and rally.is_active()
	var distance := c.position.distance_to(target.position)
	var in_range := distance <= c.attack_range + EPSILON

	if in_range and c.cooldown <= EPSILON:
		var multiplier := 1.0 + (rules.rally_damage_bonus if buffed else 0.0)
		var amount := Damage.compute(c.damage, c.damage_type, target, multiplier)
		hits.append(BattleEvent.new(BattleEvent.Type.ATTACK, tick, c.id, target.id, amount))
		c.cooldown += c.attack_interval
	elif not in_range:
		var step_length := minf(c.move_speed * dt, distance - c.attack_range)
		c.position = c.position.move_toward(target.position, step_length)

	var speed := 1.0 + (rules.rally_attack_speed_bonus if buffed else 0.0)
	c.cooldown -= dt * speed
	if c.cooldown < 0.0 and not in_range:
		c.cooldown = 0.0  # no stored attacks while walking


func _current_target(c: Combatant) -> Combatant:
	var target := get_combatant(c.target_id)
	if target == null or not target.is_alive():
		target = Targeting.pick_target(c, combatants, rules.same_lane_tie_distance)
		c.target_id = target.id if target != null else Combatant.NO_TARGET
	return target


func _separate_allies() -> void:
	var min_distance := rules.separation_distance
	for i in combatants.size():
		var a := combatants[i]
		if not a.is_alive():
			continue
		for j in range(i + 1, combatants.size()):
			var b := combatants[j]
			if b.side != a.side or not b.is_alive():
				continue
			var offset := b.position - a.position
			var distance := offset.length()
			if distance >= min_distance:
				continue
			var direction := offset / distance if distance > EPSILON else Vector2.RIGHT
			var push := direction * (min_distance - distance) / 2.0
			a.position -= push
			b.position += push


func _check_finished(events: Array[BattleEvent]) -> void:
	var player_alive := alive_count(Combatant.PLAYER)
	var enemy_alive := alive_count(Combatant.ENEMY)
	var timed_out := tick >= _timeout_ticks
	if player_alive > 0 and enemy_alive > 0 and not timed_out:
		return
	result = BattleResult.new()
	result.victory = enemy_alive == 0 and player_alive > 0
	result.timed_out = timed_out and not result.victory and player_alive > 0 and enemy_alive > 0
	result.ticks = tick
	result.survivors = player_alive
	for c in combatants:
		if c.side == Combatant.PLAYER:
			result.deployed += 1
	events.append(BattleEvent.new(BattleEvent.Type.FINISHED, tick))
