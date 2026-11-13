# core/

Cross-cutting code used by every feature. Keep it small.

Planned:
- `scene_router.gd` (autoload): switches screens, carries the `RunContext`.
- `run_context.gd`: data passed between screens during one stage attempt
  (stage, recruited units, formation, Rally charges, battle result).
- `save_service.gd` (autoload): JSON save in `user://`, versioned.
- `audio.gd` (autoload): sound effects and music.

Rule: only these three autoloads. Anything else belongs to a feature.
