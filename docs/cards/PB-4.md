# PB-4 CI: tests run on GitHub for every push

Owner: Alican · Milestone: M0 · Size: S · Branch: `feature/PB-4-ci` · Replaces: M0-02

## Why
D-034 says a branch is merged into `main` only when the tests pass. Today that
depends on someone remembering to run `dotnet test`. With two people pushing,
one forgotten run puts a broken rule on `main`. A GitHub check makes it
automatic and visible: a green tick or a red cross next to every commit.

## Today
- `dotnet test Tools/LogicTests` compiles `Scripts/Logic/**` and
  `Tests/EditMode/**` without Unity (.NET 8, C# 9,
  [LogicTests.csproj](../../Tools/LogicTests/LogicTests.csproj)); warnings are errors.
- No `.github/` folder yet.
- Unity tests (Data, PlayMode) need a Unity license, so they stay local.

## Build
1. Add `.github/workflows/logic-tests.yml`:
   ```yaml
   name: Logic tests
   on:
     push:
     workflow_dispatch:
   jobs:
     test:
       runs-on: ubuntu-latest
       steps:
         - uses: actions/checkout@v4
         - uses: actions/setup-dotnet@v4
           with:
             dotnet-version: '8.0.x'
         - run: dotnet test Tools/LogicTests --configuration Release
   ```
2. Optional, same workflow: a second job that runs `Tools/Balance` with a small
   run count (e.g. 50) and prints the table, so balance changes are visible in
   the log. Must not fail the build on numbers.
3. Push the branch; open the *Actions* tab on GitHub and check it runs green.
4. Break a test on purpose (change an expected value), push, see it go red,
   revert.
5. One line in CONVENTIONS.md (Git section): "GitHub runs the logic tests on
   every push; do not merge a red branch."

## Look
On GitHub, every commit and branch gets a ✓ or ✗ next to it; click it to see
the test log.

## Files
`.github/workflows/logic-tests.yml` (new), `docs/CONVENTIONS.md`,
`docs/STRUCTURE.md` (add `.github/`).

## In Unity
Nothing; this card does not touch Unity.

## Not in this card
Running Unity tests or building the APK on GitHub (needs a Unity license
secret, GameCI; consider after M1).

## Done when
- [ ] Workflow runs on every push and is green on `main`
- [ ] A deliberately failing test turned it red once (then reverted)
- [ ] CONVENTIONS.md and STRUCTURE.md mention it

## Notes
_Findings, screenshots, numbers, decisions taken while working on this card._
