# Puzzle Kingdoms

Portrait mobile game: Match-3 recruitment + tactical formation + auto battle.
Engine: Unity 6 LTS (URP), C#. Android first.

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

Game logic is engine-free C#; its tests run without opening Unity:

```bash
dotnet test Tools/LogicTests
```

Inside Unity: *Window → General → Test Runner → EditMode → Run All*
(includes the data-asset tests that need the editor).
