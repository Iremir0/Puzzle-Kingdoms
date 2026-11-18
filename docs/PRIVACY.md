# Data the game collects

What Puzzle Kingdoms records, why, and where it goes (PB-29, D-047). This list is the source for
the privacy policy and for Google Play's *Data safety* form; change it in the same commit as any
new event.

## Rules

- **Nothing before consent.** On the first launch the game asks (Settings → Data and privacy asks
  again). Every switch starts **off**; "Allow all" or "Save my choice" stores the answer with the
  version of the consent text (`Consent.CurrentVersion`). A new text version asks everyone again.
- **No personal data.** No name, e-mail, contacts, location, device id or advertising id is read by
  the game. The online service (once connected) adds its own random install id.
- **Without consent an event is dropped**, not kept for later.
- The player can switch any part off at any time; from then on nothing more is recorded.

## Play statistics (switch "Play statistics")

| Event | When | Values |
|---|---|---|
| `app_started` | the game opens (or right after the first "yes") | build version, language shown |
| `stage_started` | a stage attempt begins | stage id, attempt number since the game opened |
| `stage_won` / `stage_lost` | an attempt ends | stage id, stars, timed out, seconds on puzzle / War Camp / battle, units recruited, merges, deployed, survivors, Rally earned / used, gold |
| `tutorial_skipped` | "Skip tutorial" | the prompt id |
| `setting_changed` | sound, music, battle speed or language changes | setting name, new value |

Built in `Logic/Analytics/GameEvents.cs` (tested).

## Error reports (switch "Error reports")

Each exception the game logs: its message (first 300 characters), the first line of the stack trace
and the build version; at most 20 per session (`Flow/ErrorReporter.cs`).

## Personalised ads (switch "Personalised ads")

Stored only; there are no ads yet. PB-30 must ask the ad service for non-personalised ads when it is off.

## Where it goes today

| Build | Statistics | Errors |
|---|---|---|
| Editor, Windows playtest build, Android development build | `analytics-log.jsonl` next to the save file | `error-log.jsonl` next to the save file |
| Android store build | nowhere yet (no online service connected) | nowhere yet |

The local playtest log (`playtest-log.jsonl`, D-037) is separate: tester builds only, never sent.

## Not done yet (needs the owner)

- An online service (Unity Gaming Services or Firebase) and its account; the service's own data
  processing terms, and its region.
- The privacy policy page (a public URL is required by Google Play) built from this list.
