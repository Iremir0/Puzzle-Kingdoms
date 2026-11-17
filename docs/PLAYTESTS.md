# Playtests

How we run playtests and what came out of them. Round 1 is card
[PB-5](cards/PB-5.md); its results feed the go / pivot decision
([PB-6](cards/PB-6.md)). Tester-facing text (what you say, the form) is in
Turkish because our testers are; everything else is in English.

## Round 1: what we want to learn

The M1 exit question: does the core loop work without us explaining it?
Each question maps to a GDD pillar (GDD §2):

| # | Question | Pillar | Where we see it |
|---|---|---|---|
| Q1 | Do players look at the enemy while matching, and match on purpose? | Puzzle is the recruitment office | observer sheet, form Q2, `recruitedByClass` in the log |
| Q2 | After a loss, can they say why they lost? | Readable cause and effect | form Q3, observer notes |
| Q3 | Do they use Rally, and do they care when? | Auto battle, not idle | `rallyEarned` / `rallyUsed`, observer sheet |
| Q4 | Do they find Merge / Merge all and the formation rows? | War Camp choices | `merges`, `formation`, observer sheet |
| Q5 | Is a stage 2-4 minutes? Is the difficulty curve 1-1 < 1-2 < 1-3? | Short sessions | `puzzleSeconds` + `campSeconds` + `battleSeconds`, wins per stage |
| Q6 | What confuses them? Stone gems, Blast/Prism, the English UI? | — | form Q5, observer notes |

Targets from D-026 to compare against: 1-1 won almost always, 1-2 mostly,
1-3 by 60-80% of players within a few tries; battles 20-40 s.

## Before the round

- [ ] Builds made from the same `main` commit; write the commit here: `________`
  - Windows: *Puzzle Kingdoms → Build Windows Playtest* (or batch mode
    `-executeMethod PuzzleKingdoms.EditorTools.PlaytestBuild.BuildWindows`),
    then zip `Builds/Windows/PuzzleKingdoms/` and share the zip.
  - Android: *Puzzle Kingdoms → Build Android APK (dev)* (needs PB-1 checked).
- [ ] 5+ testers booked, 20 minutes each; at least 3 who have not seen the game,
      at least 2 who play mobile puzzle games.
- [ ] One person runs the session (facilitator), ideally a second takes notes.
- [ ] **Fresh start on every machine you reuse:** *Settings → Reset progress* on
      the stage map (or delete `profile.json`), set sound and music back on, and
      move the old log away (paths below), or the tester starts with unlocked stages.

## Session script (facilitator)

1. Say (and nothing more about the rules):
   > "Bu, bulmaca çözerek ordu kurduğun bir oyun. Oynarken aklından geçenleri
   > sesli söyle; doğru ya da yanlış yok, oyunu test ediyoruz, seni değil.
   > Takılırsan da önce kendin dene, bir dakika sonra yardım ederim."
2. The tester plays 1-1, 1-2 and 1-3 in order; retries are fine. Stop after
   about 15 minutes even if 1-3 is not won.
3. Do not help unless they are stuck for over a minute. If you help, write down
   what you said; that is a finding.
4. Fill the observer sheet while they play.
5. Then the form (5 minutes), then collect the log file.

### Observer sheet (one per tester)

Tick or write a short note. "?" = could not see.

| | 1-1 | 1-2 | 1-3 |
|---|---|---|---|
| Looked at the enemy panel during the puzzle | | | |
| Matched for a class on purpose ("I need archers") | | | |
| Understood stone gems without help | — | | |
| Made a Blast (2×2) or Prism (5 in a row) | | | |
| Found Merge / Merge all | | | |
| Placed units by row on purpose (archers back) | | | |
| Used Rally; when (start / clash / late / never) | | | |
| Read the results screen (stars, gold) | | | |
| Hesitated or got stuck (where?) | | | |
| Said something worth quoting | | | |

### Feedback form (tester fills in)

Copy into Google Forms or print. Answers go to the tester's section below.

1. Bu oyun ne hakkında? Bir cümleyle anlat.
2. Taş eşleştirmenin savaşla ilgisi neydi? Eşleştirirken neye göre karar verdin?
3. Kaybettiğin bir bölüm olduysa: neden kaybettin? (Kazandıysan: neden kazandın?)
4. 1-5 arası puan ver (1 = hiç, 5 = çok):
   - Eğlenceli miydi? 1-1: __ 1-2: __ 1-3: __
   - Ne yapacağın anlaşılır mıydı? __
   - Zorluk (1 = çok kolay, 3 = tam kararında, 5 = çok zor) 1-1: __ 1-2: __ 1-3: __
5. Kafanı karıştıran ne oldu?
6. En çok neyi sevdin?
7. 4. bölüm olsa oynar mıydın? (evet / belki / hayır) Neden?
8. Telefonda oynadıysan model: ________ · Normalde hangi telefon oyunlarını oynarsın? ________

## The playtest log

The game writes one JSON line per finished stage attempt to
`playtest-log.jsonl`, next to the save file (D-037). Nothing leaves the device.
Collect the file after each session and keep it with the tester's notes
(`docs/playtests/round1/T1.jsonl`, … ; contains no personal data).

| Platform | Folder (save `profile.json` and `playtest-log.jsonl`) |
|---|---|
| Windows build | `%USERPROFILE%\AppData\LocalLow\Puzzle Kingdoms Team\Puzzle Kingdoms\` |
| Unity editor | same as Windows |
| Android | `/sdcard/Android/data/com.puzzlekingdoms.game/files/` → `adb pull` that path |

Fields of one line:

| Field | Meaning |
|---|---|
| `session` | when the game was started (UTC), groups one sitting |
| `attempt` | 1, 2, 3 … within the session |
| `finishedUtc` | when the battle ended |
| `stage` | stage id (`stage_1_1` …) |
| `victory`, `timedOut`, `stars` | outcome |
| `puzzleSeconds`, `campSeconds`, `battleSeconds` | time on the puzzle, in the War Camp, battle length (simulated) |
| `recruited`, `recruitedByClass` | units recruited, per class in gem order |
| `merges` | merges done (each turns 3 units into 1) |
| `deployed`, `survivors`, `dismissed` | units fielded, alive at the end, sent home from the bench |
| `rallyEarned`, `rallyUsed` | Rally charges earned on the board / used in the battle |
| `formation` | rows front to back, e.g. `W1 W1 W2 W1 \| . M1 M1 . \| . . . . \| A1 . . A1` |
| `gold` | gold earned |

Summary of one or more log files (wins, times, merges, Rally per stage):

```bash
dotnet run --project Tools/PlaytestSummary -- docs/playtests/round1/*.jsonl
```

## Round 1 results

Date: ____ · Build commit: ____ · Facilitator: ____

### Summary

| Tester | Device | New to game? | 1-1 won/tries | 1-2 | 1-3 | Fun (avg) | Clear | Looked at enemy? | Used Rally? | Merged? |
|---|---|---|---|---|---|---|---|---|---|---|
| T1 | | | | | | | | | | |
| T2 | | | | | | | | | | |
| T3 | | | | | | | | | | |
| T4 | | | | | | | | | | |
| T5 | | | | | | | | | | |

### Top problems

| # | Problem | Testers who hit it | Card |
|---|---|---|---|
| 1 | | | |

### Answers to Q1-Q6

_Fill after the round, one or two sentences each, with the evidence._

### Per tester

#### T1
- Device / background:
- Observer sheet:
- Form answers:
- Log: `docs/playtests/round1/T1.jsonl`
- Quotes:
