# Team Board

Two developers: **Alican** and **Emir**. This file is the single task board;
the long-term plan stays in [ROADMAP.md](ROADMAP.md).

Every card has a detail file in [cards/](cards/): why it matters, what exists
today (with links to the code), the steps, a mockup and free prototype assets,
the files to touch, the Unity editor steps and the "Done when" checklist.
**Read the card file before you start.**

## Working rules

- Cards are called **PB-1, PB-2, ...** The number never changes and is never
  reused. The *Milestone* column says where a card belongs in the ROADMAP.
- One person owns a card at a time. To take a card, move it to *In Progress*,
  put your name in the Owner column and push that change first.
- Max **2 cards In Progress per person**. Finish before starting.
- One card = one branch off the latest `main`, named with the card:
  `feature/PB-3-war-camp-visuals` (fixes: `fix/PB-3-...`). The card file
  suggests a name. Commits only, no pull requests; the other person reviews by
  reading the branch, then it is merged back into `main` once the tests pass
  (D-034, D-035).
- A card is *Done* only when every box of its "Done when" list is ticked. Tick
  the boxes in the card file as you go and write findings in its Notes.
- Conflicts: scenes and prefabs are binary-ish. Only one person edits
  `Main.unity` at a time; say so in the card's Notes.
- Sync: 10-minute check-in at the start of each work session, move cards then.
- Third-party assets: add them to [CREDITS.md](../CREDITS.md) before committing.

Size: S ≤ 1 day, M ≈ 2-3 days, L ≈ 1 week. Cards are deliberately big: one card
is a piece of the game you can play or show, not a single code change.

---

## In Progress

| ID | Card | Owner | Milestone | Size | Branch | Notes |
|---|---|---|---|---|---|---|
| [PB-1](cards/PB-1.md) | Run the game on a real phone | Emir | M0 | M | — | Dev APK builds (38 MB, IL2CPP ARM64, 2026-11-14); still to do: install and play on a phone |
| [PB-7](cards/PB-7.md) | Puzzle feel: input and animations | Emir | M2 | L | `feature/PB-7-puzzle-feel` | Animations, drag-follow swipe and hint pulse built, tested and in `main`; left: feel check on a phone (PB-1 build) |
| [PB-13](cards/PB-13.md) | Stage map and tutorial | Alican | M5 | M | `feature/PB-13-map-tutorial` | Map with nodes, stars, arrow and stage card; tutorial prompts for 1-1 to 1-3 from the stage assets, skip + reset (D-040); left: a new person plays 1-1 without help |

## Ready (next up, in priority order)

| ID | Card | Owner | Milestone | Size | In short |
|---|---|---|---|---|---|
| [PB-2](cards/PB-2.md) | Play the new rules in Unity and close the review | Emir | M1 | S | Merge into `main` and the Test Runner (with Data tests) are done; left: the play checklist for 1-1 to 1-3 (4×4 grid, stone gems, Merge all, enemy strength, PB-7 feel, PB-8 sound) |
| [PB-6](cards/PB-6.md) | Go / pivot decision | both | M1 | S | Score the GDD pillars against the playtest evidence, record in DECISIONS.md. After PB-5 |

## Backlog

Not started. Each card is a milestone slice that ends in something playable.

| ID | Card | Milestone | Size | In short |
|---|---|---|---|---|
| [PB-10](cards/PB-10.md) | Art direction decision | M3 | M | Constraints, 3 options (CC0 / store packs / freelancer), phone spike, `docs/ART.md` |
| [PB-11](cards/PB-11.md) | 3D battle scene | M4 | L | Isometric orthographic view on the same simulator, placeholder models, HP bars, projectiles, HUD. Mockup inside |
| [PB-12](cards/PB-12.md) | Battle life: animations and effects | M4 | L | Mixamo idle/walk/attack/hit/death, Rally ring and aura, hit flash, battle sounds |
| [PB-15](cards/PB-15.md) | Phone performance and layout | M6 | L | Profile on 2 phones, fix top 3 hotspots, safe area, 4 aspect ratios |
| [PB-16](cards/PB-16.md) | Final polish and balance | M6 | M | Font, type and spacing scale, second playtest round, balance pass |
| [PB-17](cards/PB-17.md) | Release build | M6 | S | Icon, splash, release keystore kept outside the repo, signed AAB command |

### Post-MVP ideas (do not start before the go decision, PB-6)
Class levels with gold, Defender/Support classes, Daily Challenge, Expedition
mode, Commander heroes (see GDD §10).

---

## Done

| ID | Task |
|---|---|
| M0-D1 | Design decisions, docs, test runner, Unity 6 project |
| M0-D2 | Dev APK build command and Android toolchain; `main` brought up to date, ticket branch workflow (D-034) |
| M1-D1 | Puzzle logic, special matches, data assets, grey-box views |
| M1-D2 | War camp logic and view |
| M1-D3 | Battle simulator, Rally, 2D debug view |
| M1-D4 | Stage flow, progression, JSON save, PlayMode smoke test |
| M1-D5 | Balance pass with `Tools/Balance` (D-026) |
| M1-D6 | Gem shapes, special matches (Blast, Prism), code-drawn unit silhouettes, pointsPerUnit 8 (D-027, D-028, D-029) |
| M1-D7 | Stars and gold on the results screen, star and save-version rules tested (`ProgressionTests`) |
| M1-D8 | Enemy army preview in the War Camp and on the puzzle screen |
| [PB-3](cards/PB-3.md) | Difficulty badges on stage select and enemy panels; silhouettes in the War Camp grid, bench and enemy preview (2026-11-16) |
| [PB-8](cards/PB-8.md) | Match pop rising with cascades, Blast/Prism/stone/invalid/ding/horn sounds and a music loop (generated, D-036); Blast ring, Prism beams, stone chips, special flash, board shake; mute button (2026-11-16) |
| M1-D9 | Merge all and merge preview, 4×4 formation, stone gems, rising enemy strength (D-030 to D-033); waits for the Unity play-through in PB-2 |
| [PB-4](cards/PB-4.md) | CI: GitHub runs the logic tests and a balance table on every push (2026-11-14) |
| [PB-5](cards/PB-5.md) | Playtest kit: Windows build, session script, form, playtest log (D-037), summary tool (2026-11-17) |
| [PB-9](cards/PB-9.md) | War Camp drag and drop, grouped scrolling bench, merge animation, Undo (D-038) (2026-11-17) |
| [PB-14](cards/PB-14.md) | Crash-safe saves, settings.json (D-039), settings panel with reset progress (2026-11-17) |

---

## Suggested split (so you rarely touch the same files)

- **Alican — rules and data:** `Scripts/Logic/`, `Data/`, tests, `Tools/Balance`
  (PB-14, the undo logic in PB-9, balance in PB-16).
- **Emir — Unity and presentation:** `Scripts/Views/`, scenes, prefabs, Android,
  animation and VFX (PB-1, PB-3, PB-7, PB-8, PB-9 view, PB-11, PB-12, PB-15).
- Anything that crosses the line (a new rule that needs a new view) is agreed
  in the check-in: Alican ships the logic + tests first, Emir builds the view on top.
