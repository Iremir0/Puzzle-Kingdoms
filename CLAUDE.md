# Claude Code guidelines for Puzzle Kingdoms

- Talk to the team in Turkish. Code, comments, docs and identifiers in English.
- Start from docs/README.md. Read docs/GDD.md, docs/ARCHITECTURE.md and docs/DECISIONS.md before changing
  gameplay. Record new decisions in docs/DECISIONS.md.
- Engine: Unity 6 LTS, C#. The team is new to Unity: explain editor steps
  plainly when a task needs them.
- Gameplay rules live in `Assets/_Project/Scripts/Logic/` (assembly
  `PuzzleKingdoms.Logic`, no engine references): plain C# classes, no
  MonoBehaviour, no UnityEngine, no coroutines, no `UnityEngine.Random`
  (inject `IRandom`). C# 9 only (Unity's compiler).
- Views (`Assets/_Project/Scripts/Views/`) only display state and play back
  logic results.
- Data (units, enemies, stages, rules) lives in `Assets/_Project/Data/` as
  ScriptableObject assets, never hardcoded.
- Every logic change needs NUnit tests in `Assets/_Project/Tests/EditMode/`;
  run `dotnet test Tools/LogicTests` before committing.
- Never hand-edit scenes or prefabs as text unless unavoidable; keep `.meta`
  files with their assets.
- Small, verifiable increments. Add third-party assets to CREDITS.md first.
- Follow docs/CONVENTIONS.md; put files where docs/STRUCTURE.md says and keep
  its status markers up to date.
