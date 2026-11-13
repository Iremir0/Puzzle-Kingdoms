# Conventions

## Language

- Team communication: Turkish.
- Code, identifiers, comments, docs, commit messages: English.

## GDScript

- Follow the official GDScript style guide. Tabs for indentation.
- Static typing everywhere: `var hp: int`, `func f(x: int) -> void`, `:=` when
  the type is obvious.
- Files and folders: `snake_case`. Classes: `PascalCase` via `class_name`.
  Constants: `UPPER_SNAKE_CASE`. Signals: past tense (`unit_died`).
- Private members start with `_`.
- Doc comments with `##` on every class and public function that is not obvious.
- One class per file. Small inner classes are allowed for plain data (see
  `MoveResult`).

## Logic vs. view

- `logic/` classes `extend RefCounted`; no Nodes, timers, input or `await`.
- Randomness: always an injected `RandomNumberGenerator`, never `randi()`.
- Views call logic directly; logic reports back through return values or
  signals.

## Data

- Balancing numbers live in `data/*.tres`, not in code.
- Changing a number in the GDD? Change the resource too, in the same commit.

## Tests

- Test files: `tests/<feature>/test_<topic>.gd`, `extends TestCase`.
- Test methods: `test_<behaviour>()`, one behaviour per test.
- Use fixed seeds and handcrafted boards (`load_rows`) for exact assertions.
- All tests must pass before a commit:
  `godot --headless --path . -s tests/run_tests.gd`

## Git

- Default branch `main` stays playable.
- Feature branches: `feature/<short-name>`, fixes: `fix/<short-name>`.
- Small commits, imperative subject line under 72 characters
  (`Add merge rules for army units`).
- Pull request for every change; the other developer reviews.
- Never commit `.godot/`, builds (`*.apk`, `*.aab`) or keystores.

## Assets

- Add a row to `CREDITS.md` before committing any third-party file.
- Never use assets copied from existing games.
- Prefer `.glb` for 3D, `.png` for 2D, `.ogg` for audio.
