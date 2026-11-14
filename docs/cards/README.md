# Cards

One file per board card: `PB-<n>.md`. The board ([../BOARD.md](../BOARD.md))
keeps the one-line summary, owner and status; the card file keeps the details.
Mockups live in [img/](img/) as SVG (GitHub and VS Code show them inline;
open them in a browser to zoom).

Every card file has the same sections:

| Section | What goes in it |
|---|---|
| Why | the player-facing reason; what is worse without it |
| Today | what already exists, with links to the code |
| Build | the steps, in the order to do them |
| Look | mockup and the free assets to prototype with |
| Files | where the work happens (see STRUCTURE.md) |
| In Unity | editor steps, for people new to Unity |
| Not in this card | scope fence, so the card stays finishable |
| Done when | the checklist; a card is Done when every box is ticked |
| Notes | filled while working: findings, screenshots, numbers |

Prototype assets: only use assets whose license allows commercial use, and add
each one to [CREDITS.md](../../CREDITS.md) **before** committing it. Third-party
files go in their own folder under `Assets/` (not in `_Project/`). Every link
suggested in a card was a free source at the time of writing; check the license
page again before downloading.

A new card: copy the sections above, take the next free number, add the row
to the board.
