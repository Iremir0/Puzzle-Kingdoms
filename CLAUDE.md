# Claude Code guidelines for Puzzle Kingdoms

- Talk to the team in Turkish. Code, comments, docs and identifiers in English.
- Read docs/GDD.md, docs/ARCHITECTURE.md and docs/DECISIONS.md before changing
  gameplay. Record new decisions in docs/DECISIONS.md.
- Gameplay rules live in `features/*/logic/` as `RefCounted` classes: no Nodes,
  no timers, no global `randi()` (inject a `RandomNumberGenerator`).
- Views (`features/*/view/`) only display state and play back logic results.
- Data (units, enemies, stages) lives in `data/` as Resources, never hardcoded.
- Every logic change needs tests in `tests/`; run
  `godot --headless --path . -s tests/run_tests.gd` before committing.
- Small, verifiable increments. Add third-party assets to CREDITS.md first.
- Use static typing in GDScript; tabs for indentation.
