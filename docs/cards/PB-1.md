# PB-1 Run the game on a real phone

Owner: Emir · Milestone: M0 · Size: M · Branch: `feature/PB-1-phone-build` · Replaces: M0-01

## Why
The game is made for Android phones but has only ever run in the Unity editor.
The first run on a phone always finds something: a too-small touch target, a
layout that breaks on a tall screen, slow frames, a crash in IL2CPP that the
editor never shows. Finding these now is cheap; finding them in M6 is not. The
release card (PB-17) and the phone playtests (PB-5) wait for this card.

## Today
- The dev APK builds: 38 MB, IL2CPP, ARM64 (2026-11-14). Menu
  *Puzzle Kingdoms → Build Android APK (dev)*, code in
  [AndroidBuild.cs](../../Assets/_Project/Scripts/Editor/AndroidBuild.cs).
- Player settings (package `com.puzzlekingdoms.game`, portrait, min API 26)
  come from [ProjectSetup.cs](../../Assets/_Project/Scripts/Editor/ProjectSetup.cs).
- Steps are in [SETUP.md §4](../SETUP.md#4-android-build-milestone-0).
- Not done: installing it and playing it on a phone.

## Build
1. Build the dev APK from the menu (first build takes several minutes).
2. Turn on USB debugging on the phone (SETUP.md §4 step 2) and install with
   `adb install -r Builds/Android/PuzzleKingdoms.apk`, or copy the APK over
   and open it.
3. Play 1-1 from the stage map to the results screen. Then 1-2 and 1-3.
4. While playing, write down in the card Notes:
   - phone model, Android version, screen size;
   - does the whole UI fit (nothing cut off at the top/bottom, notch);
   - can you hit gems and War Camp cells with a thumb without mis-taps;
   - swipe on the board: does it register every time;
   - frame rate: smooth, or stutters during cascades/battles;
   - app start time; does progress survive closing and reopening the app.
5. Open `adb logcat -s Unity` while playing and copy any errors/exceptions.
6. Every problem: fix it if it takes minutes, otherwise add a card to the board.
7. Update SETUP.md §4 with anything you had to do that is not written there.

## Look
Nothing new to draw. Take 3 phone screenshots (stage map, puzzle, battle) and
put them in the card Notes or `docs/cards/img/pb-01-*.png`; later cards
compare against them.

## Files
`docs/SETUP.md`, maybe `Scripts/Editor/AndroidBuild.cs` or `ProjectSetup.cs`
if a setting is wrong. No gameplay code.

## In Unity
- Build: top menu *Puzzle Kingdoms → Build Android APK (dev)*. The progress bar
  shows Gradle; if it fails, the reason is in the Console window (red lines).
- If Unity says the Android SDK/NDK is missing: *Edit → Preferences → External
  Tools*, the Android section must point at the folders in SETUP.md §4.
- Profiling later (PB-15) needs a *Development Build*; the dev APK already is one.

## Not in this card
Fixing layout for every phone (PB-15), release signing (PB-17), performance work
beyond noting it (PB-15).

## Done when
- [ ] APK installed on at least one real phone; 1-1 played to the results screen
- [ ] 1-2 and 1-3 played on the phone too
- [ ] Notes list device, problems found, logcat errors (or "none")
- [ ] Problems are fixed or are new cards on the board
- [ ] SETUP.md §4 matches what you actually had to do

## Notes
_Findings, screenshots, numbers, decisions taken while working on this card._
