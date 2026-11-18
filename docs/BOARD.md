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
| [PB-10](cards/PB-10.md) | Art direction decision | Alican (prepares), both decide | M3 | M | `feature/PB-10-art-assets` | Decision made (D-041); all 7 characters made with Meshy Pro (own, private licence) and in `Art/Characters/`, army render in `ArtSpike`; left: phone screenshots (PB-1), rigs are for PB-12 |
| [PB-11](cards/PB-11.md) | 3D battle scene | Alican | M4 | L | `feature/PB-11-battle-3d` | Third card for Alican (the two-card limit was relaxed on purpose). In `main`: 3D battle plays with the Meshy models, T3 glow, 2D behind a toggle (D-042); built in code, no `Main.unity` edit; left: phone check and fps (PB-1), the real game screen, animations are PB-12 |
| [PB-12](cards/PB-12.md) | Battle life: animations and effects | Alican | M4 | L | `feature/PB-12-battle-life` | In `main`: rigged Meshy models, one shared Animator (idle, walk, sword/bow/spell attack, hit, death), Rally ring + glow + shake, hit dust, goblin poof, coin pop, battle sounds (D-043); left: 30+ fps on a phone (PB-1) and a look at it in the real game |
| [PB-16](cards/PB-16.md) | Final polish and balance | Alican | M6 | M | `feature/PB-16-final-polish` | In `main`: one font (Lilita One), six-step type scale, three button styles, contrast tests (D-044); left: playtest round 2 and the balance pass (need testers, after PB-5) |
| [PB-17](cards/PB-17.md) | Release build | Alican | M6 | S | `feature/PB-17-release-build` | In `main`: version 0.1.0 (1), `BuildReleaseAab`, placeholder icon and splash, SETUP 4a, release key created outside the repo; left: back the key up in two places, a signed AAB and a phone install |
| [PB-29](cards/PB-29.md) | Analytics, remote config, crash reports, consent | Alican | M6 | M | `feature/PB-29-analytics` | Growth wave 1 (D-047). In `main`: consent question (opt-in, asked again from Settings), events and error reports to local files with consent only, remote-config stage overrides, docs/PRIVACY.md; left: an online service and its account (owner's choice) |
| [PB-19](cards/PB-19.md) | Stage tool and stage goals | Alican | M7 | M | `feature/PB-19-stage-tool` | Growth wave 1 (D-048). In `main`: goals (break stones, win within), painted start boards, one balance runner, Stage Editor window, The Quarry (1-4, not in the campaign); left: open the window in the editor and play The Quarry by hand |
| [PB-18](cards/PB-18.md) | Chapter 2: Goblin Fort | Alican | M7 | L | `feature/PB-18-chapter-2` | Wave 2 (D-045). PB-20 is in `main`. Built (D-051): 4 enemy behaviours, stages 2-1 to 2-10 balanced, chapter map; left: the 4 Meshy models (Alican approves credits), a look at the map layout, a phone playthrough |

## Ready (next up, in priority order)

| ID | Card | Owner | Milestone | Size | In short |
|---|---|---|---|---|---|
| [PB-2](cards/PB-2.md) | Play the new rules in Unity and close the review | Emir | M1 | S | Merge into `main` and the Test Runner (with Data tests) are done; left: the play checklist for 1-1 to 1-3 (4×4 grid, stone gems, Merge all, enemy strength, PB-7 feel, PB-8 sound) |
| [PB-6](cards/PB-6.md) | Go / pivot decision | both | M1 | S | Owner gave "go" before the playtests (D-049): wave 2 runs; the pillar scores are still to do from the first five PB-5 sessions; a pivot then stops wave 2 |

## Backlog

Not started. Each card is a milestone slice that ends in something playable.

| ID | Card | Milestone | Size | In short |
|---|---|---|---|---|
| [PB-15](cards/PB-15.md) | Phone performance and layout | M6 | L | Profile on 2 phones, fix top 3 hotspots, safe area, 4 aspect ratios |

### Growth (D-045): wave 2 started on the owner's go (D-049): PB-20 → PB-18 → PB-22 → PB-23 → PB-21
Waves 2-4 of the growth plan; wave 1 (PB-29, PB-32, PB-19) is above. Mockups of every
card: [growth screens page](https://claude.ai/artifact/PgA4zDSKLCXPzAjwZBPsLH) (Alican's,
private until shared).

| ID | Card | Wave | Milestone | Size | In short |
|---|---|---|---|---|---|
| [PB-21](cards/PB-21.md) | New classes: Defender and Support | 2 | M7 | L | Tank and healer, their gems, up to 5 gem types per stage |
| [PB-22](cards/PB-22.md) | Kingdom and buildings | 2 | M7 | L | Gold buys building levels, small permanent class bonuses, tab bar |
| [PB-23](cards/PB-23.md) | Commander heroes | 2 | M7 | L | A Rally variant per commander, earned from stages, no loot boxes |
| [PB-24](cards/PB-24.md) | Daily quests, achievements, star chests | 3 | M8 | M | 3-4 goals a day, a chest every 30 stars |
| [PB-25](cards/PB-25.md) | Daily Challenge | 3 | M8 | M | Same seed for everyone, one try a day, local first, online after the backend decision |
| [PB-26](cards/PB-26.md) | Expedition mode (roguelite) | 3 | M8 | L | 5 battles in a row, army and wounds carry over, reward picks |
| [PB-30](cards/PB-30.md) | Economy and revenue | 3 | M8 | L | Crowns, rewarded ads, ad-free pack, season pass; power never sold |
| [PB-31](cards/PB-31.md) | Cloud save, Google Play Games | 3 | M8 | M | Save follows the player to a new phone, conflict choice |
| [PB-33](cards/PB-33.md) | Real music and sound | 3 | M8 | M | Music per screen, 15 effects replaced |
| [PB-27](cards/PB-27.md) | Async PvP: defence armies | 4 | M9 | L | Save a defence, others attack it; server re-runs battles to verify |
| [PB-28](cards/PB-28.md) | Battle replays and sharing | 4 | M9 | M | Replays from inputs, timeline, share a clip |

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
| [PB-32](cards/PB-32.md) | Turkish and English: one table `Resources/Strings/tr.txt`, Settings → Language, phone language by default, coverage tests (D-046) (2026-11-18) |
| [PB-20](cards/PB-20.md) | New gems and obstacles: chains, ice, special combos, boss stones, Stage Editor covers, drafts 2-1 and 2-2 (D-050) (2026-11-18) |

---

## Suggested split (so you rarely touch the same files)

- **Alican — rules and data:** `Scripts/Logic/`, `Data/`, tests, `Tools/Balance`
  (PB-14, the undo logic in PB-9, balance in PB-16).
- **Emir — Unity and presentation:** `Scripts/Views/`, scenes, prefabs, Android,
  animation and VFX (PB-1, PB-3, PB-7, PB-8, PB-9 view, PB-11, PB-12, PB-15).
- Anything that crosses the line (a new rule that needs a new view) is agreed
  in the check-in: Alican ships the logic + tests first, Emir builds the view on top.
