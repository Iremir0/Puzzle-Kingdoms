# features/army/logic/

Pure War Camp rules (RefCounted, tested).

Planned:
- `army_unit.gd`: one unit instance (UnitDef + tier).
- `roster.gd`: bench of recruited units.
- `formation.gd`: 3×3 grid; row = front/middle/back, column = lane.
- `merge_rules.gd`: 3 identical units of tier N → 1 unit of tier N+1 (max 3).
