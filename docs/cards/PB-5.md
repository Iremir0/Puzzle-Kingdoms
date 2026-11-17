# PB-5 Playtest round

Owner: Alican · Milestone: M1 · Size: M · Branch: `feature/PB-5-playtest` · Replaces: M1-03, M1-04 ·
Needs: PB-1 (phone build), PB-2 (rules verified)

## Why
M1 ends with a go / pivot decision (PB-6). That decision must rest on what real
players did, not on what we think. The bots in `Tools/Balance` say 1-3 is hard
for a random player (42-54% wins); only people show whether the core idea works:
*do players look at the enemy while matching gems, and does it feel like their
choice when they win or lose?*

## Today
- The full loop is playable: stage map → puzzle → War Camp → battle → results.
- Nothing for testers: no build they can install, no form, no `docs/PLAYTESTS.md`.

## Build
1. **Build:** dev APK from PB-1 (Android testers) and, for others, a Windows
   build (*File → Build Profiles → Windows → Build*), zipped.
2. **Testers:** 5 or more, at least 3 who did not see the game before; at least
   2 who play mobile puzzle games.
3. **Session script** (15-20 min, one tester at a time, in person or on a call
   with screen share):
   - Say only: "It is a puzzle game where you build an army. Think aloud." Do not
     explain rules unless they are stuck for over a minute.
   - They play 1-1, 1-2, 1-3 (retries allowed).
   - Watch and note, do not help: Where do they hesitate? Do they look at the
     enemy panel during the puzzle? Do they find Merge / Merge all? Do they use
     Rally, and when? Do they understand stone gems?
4. **Form** (after playing, one page; Google Forms or paper):
   1. What was the game about, in one sentence?
   2. How did the gem matching relate to the battle? (open)
   3. When you lost: why did you lose? (open; checks pillar 2)
   4. Rate 1-5: fun, clarity, difficulty (1-1, 1-2, 1-3 separately)
   5. What was confusing?
   6. Would you play stage 4? (yes / maybe / no)
   7. Phone model (if on a phone)
5. **Collect** into `docs/PLAYTESTS.md`: one section per tester (anonymised: T1,
   T2…), then a summary table (wins/attempts per stage, scores, top 5 problems
   with how many testers hit each).
6. Turn the top problems into cards.

## Look
`docs/PLAYTESTS.md` summary table example:

| Tester | Device | 1-1 | 1-2 | 1-3 | Fun | Clear | Looked at enemy? | Used Rally? |
|---|---|---|---|---|---|---|---|---|
| T1 | Pixel 7 | 1/1 | 1/2 | 0/3 | 4 | 3 | rarely | no |

## Files
`docs/PLAYTESTS.md` (new), form questions inside it, STRUCTURE.md entry.

## In Unity
Windows build: *File → Build Profiles*, pick *Windows*, *Switch Platform* (takes
a while), then *Build*. Switch back to Android afterwards.

## Not in this card
Fixing what testers find (new cards), balance changes (decided after, with numbers).

## Done when
- [x] Kit ready: Windows build, session script, observer sheet, form, playtest
      log and summary tool, `docs/PLAYTESTS.md` (2026-11-14)
- [ ] Android build checked on a phone (PB-1) if phone testers take part
- [ ] 5+ testers played all three stages; notes written during the session
- [ ] Forms and log files collected; `docs/PLAYTESTS.md` has per-tester notes and the summary table
- [ ] Top problems are cards on the board

## Notes
- 2026-11-14 (Alican, kit):
  - **Playtest log** (D-037): every finished stage attempt appends one JSON
    line to `playtest-log.jsonl` next to the save (stage, win, stars, time per
    screen, recruits per class, merges, Rally earned/used, formation, gold).
    Logic `StageAttempt` + 7 tests; Flow `PlaytestLog`; the PlayMode loop test
    checks one line is written. Summary: `Tools/PlaytestSummary`.
  - **Windows build:** *Puzzle Kingdoms → Build Windows Playtest*
    (`PlaytestBuild.BuildWindows`), 91 MB, zipped 34 MB without the Burst debug
    folder. Started it once: the stage map shows.
  - **Blue-screen bug fixed:** `SceneBuilder` loaded GameContent *before*
    `NewScene`, which unloads unused assets, so the saved scene had an empty
    `content` (every fresh clone and build started on a blue screen). It now
    loads after `NewScene`, and both build commands call
    `SceneBuilder.EnsureReady()` first. `Main.unity` regenerated and committed
    (tool-generated, no hand edits).
  - Unity batch run on this branch: EditMode 150/150, PlayMode 1/1 (+1 skipped:
    screenshots).
  - The build and the editor share the save folder on the same PC: delete
    `profile.json` before a tester sits down (PLAYTESTS.md, "Before the round").
  - Left for people: book 5+ testers, run the sessions, collect forms and logs.
