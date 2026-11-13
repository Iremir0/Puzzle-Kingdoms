# features/army/logic/

Pure War Camp rules (RefCounted, tested).

- `army_unit.gd`: one unit instance (UnitDef + tier); tier-scaled hp/damage.
- `formation.gd`: 3×3 grid; `Vector2i(lane, row)`, row 0 = front.
- `roster.gd`: unlimited bench + formation; recruit, deploy, undeploy, merge.
- `merge_rules.gd`: 3 identical units of tier N → 1 unit of tier N+1 (max 3).
