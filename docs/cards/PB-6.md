# PB-6 Go / pivot decision

Owner: both · Milestone: M1 · Size: S · Branch: `feature/PB-6-go-decision` · Replaces: M1-05 ·
Needs: PB-5

## Why
M2-M6 are about 11 weeks of polish and art. Spending them on a loop that testers
do not enjoy would be the most expensive mistake of the project. The roadmap puts
an explicit decision here so that it is made on purpose.

## Today
The grey-box loop, the GDD pillars (GDD §2) and, after PB-5, the playtest results.

## Build
1. Read `docs/PLAYTESTS.md` together (1 hour, both).
2. Score each GDD pillar 1-5 against the evidence:
   - **The puzzle is the recruitment office:** did testers look at the enemy and
     match on purpose?
   - **Readable cause and effect:** could they say why they lost?
   - **Auto battle, not idle:** did they use Rally and care when?
   - **Short sessions:** 2-4 min per stage?
3. Decide one of:
   - **Go:** continue to M2 as planned.
   - **Go with changes:** continue, but first fix named problems (add cards before PB-7).
   - **Pivot:** change a core part (e.g. puzzle feeds battle differently), with a
     new grey-box round.
   - **Stop.**
4. Write it as a decision in DECISIONS.md (next D-number): the choice, the scores,
   the 3 strongest pieces of evidence, what changes on the board.
5. Reorder the board to match.

## Look
No visuals.

## Files
`docs/DECISIONS.md`, `docs/BOARD.md`, maybe `docs/ROADMAP.md` and `docs/GDD.md`.

## Not in this card
Implementing the changes it decides (they become cards).

## Done when
- [ ] Decision with pillar scores and evidence recorded in DECISIONS.md
- [x] Board reordered (or new cards added) to match the decision (wave 2 order, D-049)

## Notes
_Findings, screenshots, numbers, decisions taken while working on this card._

2026-11-18 — owner's go before the playtests (D-049): Alican asked to continue as if the sessions had
been played. Nothing was scored and no evidence was written down, because there is none yet; wave 2
started. The first box stays open until the pillars are scored from five real sessions.
