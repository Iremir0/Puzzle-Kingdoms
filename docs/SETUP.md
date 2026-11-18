# Local Setup

## 1. Tools

| Tool | Version | Notes |
|---|---|---|
| Unity Hub | latest | https://unity.com/download, sign in, then add a license: profile icon → *Manage licenses* → *Add license* → *Get a free personal license* (signing in alone is not enough) |
| Unity Editor | 6.3 LTS **6000.3.28f1** (`ProjectSettings/ProjectVersion.txt`) | install via Hub with **Android Build Support** (incl. OpenJDK and Android SDK & NDK) |
| .NET SDK | 8 | runs logic tests without Unity: `winget install Microsoft.DotNet.SDK.8` |
| Git | any recent | |
| IDE | Visual Studio Community or JetBrains Rider | set it in *Edit → Preferences → External Tools* |
| Claude Code | latest | run `claude` inside the repo folder |

Both developers must use the same Unity version. If one upgrades, both do, in
one commit that also contains the regenerated `ProjectSettings/`.

## 2. Clone and open

```bash
git clone https://github.com/Iremir0/Puzzle-Kingdoms.git
```

Unity Hub → *Add → Add project from disk* → select the cloned folder. The
first open imports everything (a few minutes) and creates `Library/`
(ignored by git).

## 3. Run tests

Without Unity (fast, logic only):

```bash
dotnet test Tools/LogicTests
```

GitHub runs the same command on every push (*Actions* tab on GitHub,
`.github/workflows/logic-tests.yml`), so a forgotten local run is caught there.

In Unity: *Window → General → Test Runner → EditMode → Run All*. This also
runs `PuzzleKingdoms.Data.Tests`, which checks the ScriptableObject assets.

From the command line (Unity closed; it cannot open a project twice):

```bash
"<Unity>/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml
```

PlayMode (full grey-box loop): same command with `-testPlatform PlayMode`.
Screenshots of every screen (1080×1920 PNGs in `Logs/Screenshots/`, not
committed): add `-testFilter GreyboxScreenshots`.

## 3a. Balance runs

```bash
dotnet run -c Release --project Tools/Balance 300
```

Plays every stage 300 times per bot setting on the real `.asset` files and
prints win rate, battle length and army size. Run it after changing any
number in `Assets/_Project/Data/` and compare with the targets in D-026.

## 3b. Play the grey-box

Open `Assets/_Project/Scenes/Main.unity` and press Play. In the Game view pick
a portrait resolution (e.g. *1080x1920*). If the scene is missing or broken,
regenerate it: *Puzzle Kingdoms → Build Main Scene*.

After changing `ProjectSetup.cs`, re-run *Puzzle Kingdoms → Apply Project
Settings* (or `-batchmode -quit -executeMethod PuzzleKingdoms.EditorTools.ProjectSetup.Apply`).

## 4. Android build (Milestone 0)

Player settings (package `com.puzzlekingdoms.game`, portrait, IL2CPP, ARM64,
min API 26) come from `ProjectSetup`; do not set them by hand.

1. Build a development APK: *Puzzle Kingdoms → Build Android APK (dev)*, or in
   batch mode
   `Unity.exe -batchmode -quit -projectPath . -executeMethod PuzzleKingdoms.EditorTools.AndroidBuild.BuildDevApk -logFile Logs/android-build.log`.
   Output: `Builds/Android/PuzzleKingdoms.apk` (debug-signed, gitignored).
   The first build takes several minutes (IL2CPP + Gradle).
2. On the phone: *Settings → About phone*, tap *Build number* 7 times, then
   *Developer options → USB debugging* on. Connect the cable and allow the PC.
3. Install: `<AndroidPlayer>/SDK/platform-tools/adb install -r Builds/Android/PuzzleKingdoms.apk`
   (`<AndroidPlayer>` = `D:/Unity/6000.3.28f1/Editor/Data/PlaybackEngines/AndroidPlayer`),
   or copy the APK to the phone and open it (allow "install unknown apps").
4. Done when the stage map shows in portrait on a real phone and stage 1-1
   plays to the results screen.

If Android Build Support was installed without Hub's JDK/SDK/NDK modules, the
editor expects them under `<AndroidPlayer>/OpenJDK`, `SDK` (platform-tools,
cmdline-tools/latest, build-tools/36.0.0, platforms/android-36, cmake/3.22.1)
and `NDK` (r27c). Hub's download cache has the zips.

Do not commit release keystores or passwords.

## 4a. Release build (PB-17)

A store build is a **signed Android App Bundle** (`.aab`), built with one command. The signing key is the
identity of the game forever: **lose it and you cannot update the game.** It never goes into the repo
(`.gitignore` already ignores `*.keystore`, `*.jks` and `/Builds/`).

1. **Create the key once** (Alican, on one machine): in Unity *Edit → Project Settings → Player → Publishing
   Settings → Keystore Manager → Create new*, or with the JDK's `keytool -genkeypair -v -keystore
   puzzlekingdoms-upload.keystore -alias upload -keyalg RSA -keysize 2048 -validity 10000`. Use a long
   password. Keep the file and the passwords in a password manager **and one second backup that both of you
   can reach**. Plan to use Google Play App Signing: the key you create is only the *upload key*, so a lost
   upload key can be reset with Google. A PKCS12 keystore (the default) uses one password for the store and the key, so set `PK_KEYSTORE_PASS` and `PK_KEY_PASS` to the same value.
2. **Build** with the four variables set in the shell (nothing is read from files in the repo):

   ```
   set PK_KEYSTORE_PATH=D:\keys\puzzlekingdoms-upload.keystore
   set PK_KEYSTORE_PASS=...
   set PK_KEY_ALIAS=upload
   set PK_KEY_PASS=...
   "<Unity>/Editor/Unity.exe" -batchmode -quit -projectPath . -executeMethod PuzzleKingdoms.EditorTools.AndroidBuild.BuildReleaseAab -logFile Logs/android-release.log
   ```

   Output: `Builds/Android/PuzzleKingdoms.aab`. Missing variables stop the build with a clear error. The
   passwords are cleared from the player settings after the build.
3. **Version:** `ProjectSetup.Version` ("0.1.0") and `ProjectSetup.VersionCode` (1); bump both, run
   *Puzzle Kingdoms → Apply Project Settings*, see CONVENTIONS.md.
4. **Install on a phone:** upload the `.aab` to Play Console *internal testing* (or use `bundletool
   build-apks` and `install-apks`), then play stage 1-1.
5. The playtest log (`playtest-log.jsonl`, D-037) is not written in an Android store build; it is written in
   the editor, in development builds and in the Windows playtest build.
6. Icon and splash come from `Puzzle Kingdoms → Build App Icon` (a gold crown over a red gem, drawn in code,
   a placeholder); replace the PNGs in `Assets/_Project/Art/Icon/` when there is a real icon.

## 5. Optional: smart merge for scenes and prefabs

Unity ships `UnityYAMLMerge`. Add to your global git config (adjust the path
to your Unity version):

```
[merge "unityyamlmerge"]
    name = Unity SmartMerge
    driver = "'C:/Program Files/Unity/Hub/Editor/<version>/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"
    recursive = binary
```
