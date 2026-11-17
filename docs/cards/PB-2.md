# PB-2 Play the new rules in Unity and close the review

Owner: Emir (carried over from M1-08; swap in the check-in if Alican takes it) · Milestone: M1 · Size: S · Branch: `feature/PB-2-verify-new-rules` ·
Replaces: M1-08, M1-06, M1-07, M1-01, M1-02

## Why
Four features went into `main` on 2026-11-14 after passing the automated tests,
but three of them were never played by a person: the 4×4 formation, stone gems
and rising enemy strength. Tests check the rules; only playing checks that the
screen is readable and the stages feel right. Until this card is done these
four features are "probably fine".

## Today
| Feature | Decision | Played? |
|---|---|---|
| Merge all button and merge preview | D-030 | yes (Alican) |
| 4×4 formation (16 slots), enemy armies rebuilt | D-031 | no |
| Stone gems: first match only breaks the stone | D-032 | no |
| Enemy HP ×1.0 / ×1.1 / ×1.2 per stage, all HP ×1.4 | D-033 | no |

- 130 EditMode tests pass with `dotnet test Tools/LogicTests`.
- Emir merged `feature/enemy-scaling` into `main` and ran the Unity Test Runner
  on the result (2026-11-14), including the Data tests
  ([GameDataTests.cs](../../Assets/_Project/Tests/EditModeData/GameDataTests.cs))
  that were changed for the new HP values, stage compositions, stone counts and
  multipliers.
- The `.meta` files for the new test files are committed.
- What is left is the play-through below.

## Build
1. `git switch main`, `git pull`, open the project in Unity.
2. Quick re-run of the Unity Test Runner (EditMode and PlayMode) on the latest
   `main`, in case something landed since; note the counts.
3. Play the checklist below in order. Write what you see in the Notes, also
   when it is fine.
4. For every problem: if it is a one-line fix, fix it on this branch with a
   test; otherwise add a card.

### Play checklist
**1-1 Border Watch** (6 Grunts, no stones, HP ×1.0)
- [ ] Puzzle: no half-stone gems on the board.
- [ ] War Camp: the 4×4 grid is fully visible, cells are tappable, the enemy
      preview at the top shows 4 Grunts in front and 2 behind.
- [ ] Battle: 4 lanes, units do not overlap badly, it lasts roughly 15-25 s.

**1-2 Arrow Rain** (5 Grunts + 3 Archers, 3 stones, HP ×1.1)
- [ ] Puzzle: exactly 3 gems are half stone (colour and shape visible, the
      right half grey cracked rock). Never two next to each other.
- [ ] Match a stone gem: it stays, the rock disappears, the label says
      "Stone cracked!", it gives no meter points. Match it again: it clears.
- [ ] A stone gem falls with its gem when the gems below clear.
- [ ] Merge all: the line under the grid lists what will merge; the button is
      greyed out when nothing can merge.

**1-3 The Chief's Guard** (Brute, Chief, Brute + 2 Archers, 5 stones, HP ×1.2)
- [ ] The Chief is clearly bigger/stronger; Brutes take little damage from
      Warriors and Archers (50% armor), Mages hurt them.
- [ ] Win once with a planned army (Mages in, Rally used). Lose once on purpose:
      the retry works.
- [ ] Battle length roughly 30-45 s without Rally.

**All stages**
- [ ] The results screen shows stars and gold; the stage map unlocks the next stage.
- [ ] Close and reopen Play mode: stars and gold are still there.
- [ ] Puzzle feel (PB-7, added 2026-11-16): gems slide, pop and fall; an invalid
      swap leans and shakes; a short drag moves the gem and snaps it back.
- [ ] Sound (PB-8, added 2026-11-16; nobody has listened yet): music loops, the
      pop rises with cascades, stone crack, "nope", ding and horn are audible and
      not too loud; the Sound/Muted button bottom right silences everything.

## Look
What it should look like now: half-stone gem = the gem's colour and shape with
the right half drawn as speckled grey rock with a jagged edge
(`GemShapes.GetStoneHalf`).
War Camp and Battle as in Unity today; PB-3 improves the War Camp.

## Files
Normally none. Fixes go where the bug is; logic fixes need a test in
`Tests/EditMode/`.

## In Unity
- Test Runner: *Window → General → Test Runner*. Tabs *EditMode* and
  *PlayMode*; press *Run All* in each. Green tick = pass, red cross = fail
  (click it to see why).
- Play: open `Assets/_Project/Scenes/Main.unity` (double-click in the Project
  window), set the Game view to *1080x1920* (dropdown at the top of the Game
  view), press the ▶ button.
- If the screen stays blue: the `GameApp` object in the Hierarchy needs
  `GameContent` in its *Content* field (drag `Assets/_Project/Data/GameContent.asset`
  onto it), or run *Puzzle Kingdoms → Build Main Scene*.

## Not in this card
New features or balance changes. If a stage feels too hard or too easy, write it
down for the playtests (PB-5); balance is decided on tester data, not one run.

## Done when
- [x] Unity Test Runner (with Data tests) green on the merged `main` (Emir, 2026-11-14)
- [ ] Quick re-run on the latest `main` before playing; counts in Notes
- [ ] Every box of the play checklist ticked or turned into a card
- [ ] Board: the In Review entries are removed and this card is Done

## Notes
_Findings, screenshots, numbers, decisions taken while working on this card._
