# What is left for the owners

Everything the code, tests and docs could do without a phone, an account, a file or a decision is built. This page
collects what only you can do, in one place, ordered by what unblocks the most. Tick a box here when it is done and
note it in the card. Card numbers link to [BOARD.md](BOARD.md); decisions are in [DECISIONS.md](DECISIONS.md).

Last updated 2026-11-19 (after PB-33).

## 1. Put the game on a phone and look at it (nothing else is as important)

Most "left:" notes on the board say "on a phone". Do these in one sitting with the dev APK
([SETUP.md](SETUP.md), PB-1).

- [ ] **Install the dev APK and play** (PB-1, Emir): 1-1 to 1-4, then Chapter 2.
- [ ] **Feel check:** swipes, animations and hint pulse (PB-7); fps of the 3D battle, aim for 30+ (PB-11, PB-12).
- [ ] **Look at every screen on a phone shape.** None of these were seen on a phone, only in tests and screenshots:
  Settings (rows were re-spaced for the Cloud row), the two buttons above the map (Daily, Expedition), Kingdom,
  Commanders, Quests, Shop, the Daily Challenge, the Expedition road, Replays, the replay timeline, the puzzle's
  "extra moves" button and the War Camp's "extra Rally" button.
- [ ] **Play the checklist for 1-1 to 1-3** (PB-2, Emir): 4×4 grid, stone gems, Merge all, enemy strength, feel, sound.
- [ ] **Open the Stage Editor and play The Quarry by hand** (PB-19).
- [ ] **A new person plays 1-1 without help** (PB-13), and you watch.
- [ ] **Phone performance round** (PB-15, not started): profile on two phones, fix the top three hotspots, check 4 screen
  shapes and the safe area.
- [ ] Take **phone screenshots** for the art decision record (PB-10).

## 2. Playtests and the go / pivot decision

- [ ] **Run the first five playtest sessions** ([PLAYTESTS.md](PLAYTESTS.md), PB-5 kit).
- [ ] **Score the pillars and record "go" or "pivot"** (PB-6, both). The owner already said "go" (D-049); the scores decide
  whether wave 2 and 3 continue.
- [ ] **Playtest round 2 and the balance pass** (PB-16). Needs testers.

## 3. Accounts and store setup

Only you can create these. Each one unlocks a card that is otherwise finished in the game.

- [ ] **Google Play Console app** (PB-17): create the app, fill the store listing. Also unlocks cloud save.
- [ ] **Back up the release key in two places** (PB-17). Losing it means you can never update the game.
- [ ] **A signed AAB installed on a phone** (PB-17).
- [ ] **Check the current Google Play policies and fees** for ads, purchases and data. These change; nobody here verified them.
- [ ] **Cloud save (PB-31):** import the Google Play Games plugin (add it to [CREDITS.md](../CREDITS.md) first), connect it
  behind `ICloudSave`, test on two phones. The conflict rule and dialog are done.
- [ ] **Ads and purchases (PB-30):** choose and add a rewarded-ad SDK and Google Play Billing (CREDITS.md first), create
  the Supporter product ("about $2.99") in the Play Console, connect them behind `IAds` and `IBilling`. Personalised
  ads only after the consent question (PB-29).
- [ ] **Analytics and crash reports online (PB-29):** pick a service and open its account. Events and errors go to local
  files only until then. The options and what we will read: [ANALYTICS.md](ANALYTICS.md) (PB-36).
- [ ] **A backend, if you want online features:** needed for the Daily Challenge ranking (PB-25 step 4) and async PvP
  (PB-27, not started). Decide, then cards can be split.

## 4. Files and art that need your tools

- [ ] **Audio files** (PB-33): get or generate the music (4 tracks) and the 15 effects as described in
  [AUDIO.md](AUDIO.md), write each in CREDITS.md, drag them into `Data/SoundBank.asset`, listen on a phone.
- [ ] **Meshy models (credits are yours):**
  - Defender and Support units, with rigs (PB-21, about 106 credits).
  - Rigs and animations for the four Chapter 2 goblins (PB-18).
  - Three commander models and real 3D effects for Shield Wall, Arrow Rain and Fireball (PB-23).
- [ ] **Optional art with ChatGPT:** a drawn road for the Expedition screen (PB-26), art for the quests (PB-24),
  walled versions of the meadow and camp battle grounds, a store icon and splash instead of the placeholders (PB-17).
  Check the service terms before the commercial release, as noted in CREDITS.md.

## 5. Decisions only you can make

- [ ] **Crowns and a season pass:** add a card for them, or leave the Shop as it is (D-064 left them out).
- [ ] **Sharing a replay clip** (rest of PB-28): choose a recorder plugin and try it on an Android phone.
- [ ] **Store price of the Supporter pack** and whether to keep the rewarded-ad amounts (`Data/Monetization.asset`).
- [ ] **Merge or review:** all finished cards are already in `main` (PB-21, PB-24, PB-25, PB-26, PB-28, PB-30, PB-31,
  PB-33). Emir can read them through the card files and notes.

## Where each thing is explained

| Topic | Read |
|---|---|
| Building and installing the APK | [SETUP.md](SETUP.md) |
| Audio method and licences | [AUDIO.md](AUDIO.md) |
| Consent, events, privacy | [PRIVACY.md](PRIVACY.md) |
| Why a choice was made | [DECISIONS.md](DECISIONS.md) |
| Third-party files | [../CREDITS.md](../CREDITS.md) |
