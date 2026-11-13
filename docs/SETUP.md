# Local Setup

## 1. Tools

| Tool | Version | Notes |
|---|---|---|
| Unity Hub | latest | https://unity.com/download, sign in with a Unity account (free Personal license) |
| Unity Editor | 6 LTS, exact version in `ProjectSettings/ProjectVersion.txt` | install via Hub with **Android Build Support** (incl. OpenJDK and Android SDK & NDK) |
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
