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

In Unity: *Window → General → Test Runner → EditMode → Run All*. This also
runs `PuzzleKingdoms.Data.Tests`, which checks the ScriptableObject assets.

From the command line (Unity closed; it cannot open a project twice):

```bash
"<Unity>/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml
```

PlayMode (full grey-box loop): same command with `-testPlatform PlayMode`.
Screenshots of every screen (1080×1920 PNGs in `Logs/Screenshots/`, not
committed): add `-testFilter GreyboxScreenshots`.

## 3b. Play the grey-box

Open `Assets/_Project/Scenes/Main.unity` and press Play. In the Game view pick
a portrait resolution (e.g. *1080x1920*). If the scene is missing or broken,
regenerate it: *Puzzle Kingdoms → Build Main Scene*.

After changing `ProjectSetup.cs`, re-run *Puzzle Kingdoms → Apply Project
Settings* (or `-batchmode -quit -executeMethod PuzzleKingdoms.EditorTools.ProjectSetup.Apply`).

## 4. Android build (Milestone 0)

1. *File → Build Profiles → Android → Switch Platform*.
2. *Player Settings*: Company/Product name, package name
   `com.<company>.puzzlekingdoms`, Default Orientation **Portrait**, Scripting
   Backend **IL2CPP**, Target Architectures **ARM64**.
3. Enable *Developer options → USB debugging* on the phone and connect it.
4. *Build Profiles → Build And Run*. Keep the debug keystore for now.
5. Done when the empty project shows in portrait on a real phone.

Do not commit release keystores or passwords.

## 5. Optional: smart merge for scenes and prefabs

Unity ships `UnityYAMLMerge`. Add to your global git config (adjust the path
to your Unity version):

```
[merge "unityyamlmerge"]
    name = Unity SmartMerge
    driver = "'C:/Program Files/Unity/Hub/Editor/<version>/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"
    recursive = binary
```
