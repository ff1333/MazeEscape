# Maze Escape / 迷宫逃生

Current candidate: **v1.3.1**. This release changes the public product name from Fogbound Maze / 迷雾逃生 to Maze Escape / 迷宫逃生 so the title matches the full ten-level experience. Chinese remains the default UI language. All v1.3.0 maze, lighting, reload, combat, localization and audio features are retained.

Maze Escape is a Unity 6 third-person and first-person survival maze game.
The player starts at the title screen, selects an unlocked level, then chooses
an SMG or long blade outside a procedurally generated maze,
enters through the start gate, survives zombie attacks, and reaches the exit.

## Portfolio Scope

- Ten deterministic, progressively harder maze levels
- Title screen, ten-level selection and locally saved sequential completion
- Animated survivor and two zombie models from the Quaternius CC0 kit
- Runtime maze generation with guaranteed start-to-exit connectivity
- Branch-rich Growing Tree mazes with early route choices
- Fog-of-war mini-map showing visited cells and unexplored exit mouths without revealing the solution
- Switchable first-person and third-person cameras
- Ranged and melee weapons with distinct risk/reward
- Grid A* navigation and explicit zombie AI states
- Normal and elite zombie variants backed by object pooling
- Fog, day/night cycling, miasma damage and safe-light zones
- Keyboard/mouse and Android touch controls
- Windows, WebGL and Android release targets

## Technology

- Unity `6000.3.18f1`
- C# and the Unity Input System
- Unity Test Framework

Development evidence, architecture notes, tests and release instructions live
under `docs/`.

## Screenshots

![v1.3.1 title screen](docs/test-results/v1.3.1/native/title-zh.png)

![Language and audio settings](docs/test-results/v1.3.0/native/v1.3.0-settings-en.png)

![Sequential level selection](docs/images/v1.1.0-levels.png)

![Survivor and zombies](docs/images/v1.1.0-actors.png)

## Current Validation

- EditMode: **15/15 PASS**; PlayMode: **35/35 PASS**.
- Windows/WebGL: **0 errors / 0 warnings**; Android: **0 errors / 24 build-chain warnings**.
- Current verification: [v1.3.1 evidence](docs/test-results/v1.3.1/README.md).
- Platform results and device-testing limitations: [v1.2.2 evidence](docs/test-results/v1.2.2/README.md).
- Current development log: [safe lights and reload feedback](docs/devlogs/10-safe-lights-and-reload-feedback.md).
- Release attachments: `Builds/Packages/v1.2.2/` (ignored by Git).

## Historical v1.1.0 Results

- Local version: `v1.1.0` campaign menus and animated character art
- EditMode tests: `10/10 PASS`
- PlayMode tests: `21/21 PASS`, including menu flow, saved unlocks, animation and physical corridors
- Windows Release Player: title, levels, loadout, actors, small level menu and first-person six-cell walks `PASS`
- Windows/WebGL/Android BuildReport: `0 errors / 0 warnings`
- All three packages generated and SHA-256 verified; WebGL interaction and Android device acceptance remain `PENDING`
- Release attachment directory: `Builds/Packages/v1.1.0/` (ignored by Git)
- Start here: [`docs/README.md`](docs/README.md)
- Playtest bug-fix log: [`docs/devlogs/04-first-playtest-bugfix.md`](docs/devlogs/04-first-playtest-bugfix.md)
- Current development log: [`docs/devlogs/07-campaign-menu-and-character-art.md`](docs/devlogs/07-campaign-menu-and-character-art.md)
- Current illustrated guide: [`docs/09_V1_1_0_MENU_AND_CHARACTERS.md`](docs/09_V1_1_0_MENU_AND_CHARACTERS.md)
- Art license and attribution: [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md)
