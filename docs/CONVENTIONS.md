# Conventions

## Language

- Team communication: Turkish.
- Code, identifiers, comments, docs, commit messages: English.

## C#

- C# 9 (Unity 6's compiler). `dotnet test` enforces it for logic.
- 4 spaces, Allman braces, one type per file (small nested data types are fine).
- `PascalCase` for types, methods, properties, public fields and constants;
  `_camelCase` for private fields; `camelCase` for locals and parameters.
- Serialized fields on ScriptableObjects/MonoBehaviours: `[SerializeField]
  private` with `camelCase` names; expose read-only properties.
- Namespaces follow folders: `PuzzleKingdoms.Logic.Puzzle`,
  `PuzzleKingdoms.Data`, `PuzzleKingdoms.Views.Battle`, ...
- XML doc comments (`///`) on every public type and non-obvious member.
- Events are past tense or describe the change: `Changed`, `RewardsEarned`.

## Logic vs. views

- `Scripts/Logic/`: plain classes, no `UnityEngine`, no coroutines/async,
  no static mutable state.
- Randomness: always an injected `IRandom`. Never `UnityEngine.Random` or
  `System.Random`.
- Views call logic directly; logic reports back through return values or
  C# events.

## Data

- Balancing numbers live in ScriptableObject assets under
  `Assets/_Project/Data/`, not in code.
- Changing a number in the GDD? Change the asset too, in the same commit.
- Asset file names: `PascalCase` (`GoblinGrunt.asset`, `Stage_1_2.asset`);
  ids inside assets: `snake_case` (`goblin_grunt`).

## Tests

- NUnit. Logic tests: `Tests/EditMode/<Feature>/<Topic>Tests.cs`; method
  names describe one behaviour (`MergePrefersBenchCopiesOverDeployedOnes`).
- Use fixed seeds and handcrafted boards (`LoadRows`) for exact assertions.
- All tests must pass before a commit: `dotnet test Tools/LogicTests`
  (and the Unity Test Runner when data assets changed).

## Unity

- Always commit `.meta` files together with their assets; never delete a
  `.meta` by hand.
- Prefer prefabs over scene objects; keep scenes small to limit merge conflicts.
- Asset Serialization mode: Force Text (default).

## Git

- Default branch `main` stays playable.
- Every piece of work is a task in [BOARD.md](BOARD.md); its ID is milestone
  plus number, e.g. `M1-01`.
- Branch off the latest `main`, named after the card:
  `feature/M1-01-<short-name>`, fixes `fix/M1-01-<short-name>`.
- Merge back into `main` only when `dotnet test Tools/LogicTests` and the
  Unity EditMode/PlayMode tests pass. Then move the card to *Bitti*.
- Small commits, imperative subject line under 72 characters
  (`Add merge rules for army units`).
- Pull request for every change; the other developer reviews.
- Never commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, builds
  (`*.apk`, `*.aab`) or keystores.

## Assets

- Add a row to `CREDITS.md` before committing any third-party file.
- Never use assets copied from existing games.
- Prefer `.fbx`/`.glb` for 3D, `.png` for 2D, `.ogg` for audio.
