# Puzzle Kingdoms

Portrait mobile game: Match-3 recruitment + tactical formation + auto battle.
Engine: Godot 4.6 (Mobile renderer), GDScript. Android first.

## Docs
Start with the [documentation index](docs/README.md).

- [Game Design Document](docs/GDD.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Project Structure](docs/STRUCTURE.md)
- [Conventions](docs/CONVENTIONS.md)
- [Local Setup](docs/SETUP.md)
- [Decision Log](docs/DECISIONS.md)
- [Roadmap](docs/ROADMAP.md)
- [Credits & licenses](CREDITS.md)

## Running tests

```bash
godot --headless --path . --import          # first time: builds class cache
godot --headless --path . -s tests/run_tests.gd
```
