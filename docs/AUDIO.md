# Audio: how music and sound get into the game (PB-33)

Plain steps for a team that is new to Unity. The code side is done (D-066); this page is the method for getting
the sounds themselves, cheaply and legally.

## What is built

- **Music per screen, cross-faded.** `SoundBank` has a default loop plus four optional music fields: **Map Music**
  (map, Kingdom, Commanders, Quests, Shop, Daily, Expedition, Replays), **Puzzle Music** (puzzle and War Camp), **Battle
  Music** and **Results Music** (a short piece, plays once). A field left empty falls back to the default loop, so the
  game works with one track or with five. Changing screens fades the old music out and the new one in
  (**Music Fade Seconds**, 1.2 s by default). Only one or two music streams ever play.
- **Import rules that keep it cheap on a phone.** Files dropped into `Assets/_Project/Audio/Music/` import as
  streamed Vorbis (not kept in memory); files in `Assets/_Project/Audio/Sfx/` import as mono Vorbis decoded on load
  (no delay when they play). This happens by itself on first import (`AudioImportRules`); *Puzzle Kingdoms → Apply
  Audio Import Rules* applies it to files that are already there.
- **Budget.** Music: 4 tracks of 60–120 s at about 100 KB/s of Vorbis is under 3 MB each, 12 MB in all at most.
  Effects: 15 clips under 0.3 MB each. Keep the whole `Audio/` folder under 20 MB.

## Sound map

| Where | File goes to | What it should feel like |
|---|---|---|
| Map and menus | `Music/map.ogg` | Calm, warm fantasy, a gentle theme with a few instruments, loops without a break |
| Puzzle and War Camp | `Music/puzzle.ogg` | Light, playful, steady pulse, not tiring after minutes of play |
| Battle | `Music/battle.ogg` | Drums and low strings, more drive, still not drowning the effects |
| Results | `Music/results.ogg` | 10–20 s victory fanfare; a quieter defeat can follow as a second file later |
| match, blast/prism created and fired, stone crack, invalid swap, unit recruited, Rally charge | `Sfx/` | Short, bright "pop", "glass", "crunch"; under 0.5 s; the match sound is pitched up per cascade by the code |
| sword hit, bow shot, magic cast, unit died, Rally start | `Sfx/` | Punchy and dry; the battle plays many at once, so keep them short and not shrill |

## Where to get them (free, safe to ship)

Check every file's licence yourself on the day you download it; terms change.

1. **No-AI base, always allowed: CC0 packs.** [Kenney](https://kenney.nl) audio packs (UI Audio, RPG Audio, Impact Sounds,
   Digital Audio) are CC0: free for commercial use, no credit needed. Also OpenGameArt with the *CC0* filter. This
   alone covers the 15 effects.
2. **Free music with a clear licence.** Pixabay Music is free for commercial use without credit under the Pixabay Content
   License (you may ship it in the game, not resell the file by itself); uploads before 2019 are CC0. Keep a link to each
   track.
3. **AI, free to run yourself.** Stability AI's **Stable Audio 3.0** open weights (Small SFX, Small, Medium; released May 2026)
   are published under the Stability AI Community License: you own the outputs and may use them commercially while the
   organisation earns under $1M a year. The older Stable Audio Open 1.0 card still points to a non-commercial licence, so
   use 3.0 and read the licence text on the model page of the version you download. They run on your own computer,
   so there is no credit limit and no website to depend on. Good for the effects (Small SFX) and for music loops. Needs a
   computer with a recent graphics card and a short setup (Python); ask for help before you start.
4. **AI websites: usually not free for a shipped game.** The free tiers of the popular music websites (for example Suno)
   are for non-commercial use; commercial use needs a paid plan, and what you may do depends on the plan on the day you
   generate. Do not use a free-tier track in the store build.
5. **Copyright note.** Purely AI-made music may not be protectable by copyright, and other people can get similar output.
   For a game this is acceptable, but a little human editing (trim, EQ, a loop point) helps and costs nothing.

**Recommended path:** effects from Kenney (CC0), music from Pixabay or generated with Stable Audio 3.0, a few lines in
CREDITS.md for every file. Replace by hand later if a composer joins.

## Steps for one new sound

1. Get the file. Edit it in [Audacity](https://www.audacityteam.org) (free): trim silence, normalise to about −16 dB for
   music and −6 dB peak for effects, and for music make the loop point clean (end flows into start).
2. Export as OGG or WAV and name it as in the table (`map.ogg`, `sword-hit.ogg`, ...).
3. Add a line to `CREDITS.md` **first**: file, source link, author or tool and model version, licence, date, and for AI the
   prompt you used.
4. Drag the file into `Assets/_Project/Audio/Music/` or `.../Sfx/` in the Project window. The import settings apply
   themselves.
5. Select `Assets/_Project/Data/SoundBank.asset`, drag the clip into its field in the Inspector (Map Music, Puzzle Music,
   ...). Press Play to hear it. Volumes are in the same asset (**Music Volume**, **Effects Volume**).
6. Listen on a phone speaker and on headphones. If music hides the effects, lower **Music Volume**.

## Prompts to start from (for an AI tool)

- Map: "calm medieval fantasy village theme, acoustic guitar, soft flute and light strings, 90 BPM, seamless loop".
- Puzzle: "playful light fantasy puzzle game music, plucked strings and marimba, steady rhythm, 110 BPM, seamless loop".
- Battle: "energetic medieval fantasy battle music, war drums, low strings, brass accents, 140 BPM, seamless loop".
- Results: "short triumphant fantasy fanfare, brass and timpani, 12 seconds, no loop".
- Effects: "short bright puzzle gem match pop", "dry sword hit on armour", "wooden bow shot with arrow whoosh", "small magic
  spell cast sparkle", "goblin falls, soft thud".

## Not in this card

Voice, dynamic music layers, a separate defeat piece (add a field when there is one).
