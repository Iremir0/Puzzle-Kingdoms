# Local Setup

## 1. Tools

| Tool | Version | Notes |
|---|---|---|
| Godot | 4.6 stable, **standard** (not .NET) | https://godotengine.org/download |
| Git | any recent | |
| Claude Code | latest | run `claude` inside the repo folder |
| Android Studio or command-line tools | latest | only for Android export (step 4) |
| JDK | 17 | required by Godot's Android export |

Both developers must use the same Godot version. If one upgrades, both do,
and `config/features` in `project.godot` is updated in the same commit.

## 2. Clone and open

```bash
git clone https://github.com/iremir0/puzzle-kingdoms.git
cd puzzle-kingdoms
```

Open `project.godot` in the Godot editor once so the `.godot/` cache is built.

## 3. Run tests

```bash
godot --headless --path . --import              # first time / after adding classes
godot --headless --path . -s tests/run_tests.gd
```

Exit code 0 means all tests passed. On Windows use the console executable
(`Godot_v4.6-stable_win64_console.exe`) so output is printed.

> Status: the puzzle tests were written without a Godot binary available and
> have **not been run yet**. Running and fixing them is the first task.

## 4. Android export (Milestone 0)

1. Install Android SDK (Platform-Tools, Build-Tools, a recent Platform) and JDK 17.
2. Godot: *Editor → Editor Settings → Export → Android*: set Java SDK and
   Android SDK paths.
3. *Project → Install Android Build Template* is **not** needed yet.
4. *Project → Export → Add… → Android*. Keep the debug keystore for now.
5. Enable *USB debugging* on the phone, connect it, and use the Android
   "Remote Debug" button in the editor toolbar to run on the device.
6. Done when the placeholder main menu shows in portrait on a real phone.

Do not commit release keystores or passwords. `export_presets.cfg` may be
committed once it contains no secrets.
