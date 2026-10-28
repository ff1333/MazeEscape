# Fogbound Maze - Game Design Baseline

## Player Promise

Choose a weapon, enter an unfamiliar maze, read the light and sound around you,
and survive long enough to find the exit.

## Core Loop

1. Open the title screen and press START.
2. Select an unlocked level from the ten-level campaign.
3. Spawn outside that maze holding the long blade, with a bolt-action sniper rifle available to switch to.
4. Enter through the start gate and navigate while managing health and enemies.
5. Reach the exit alive, save completion locally, and unlock the next level.
6. Retry, replay a cleared level, return to level selection, or continue.

Level 1 is initially unlocked. Completing level N unlocks N+1, up to level 10.
Entering a level or dying never unlocks another. Progress persists locally;
there is no account or cross-device cloud save. Prior genuine unlock progress
is migrated, but the old selected-level preference is not proof of completion.

## Control Scheme

| Action | Desktop | Mobile |
| --- | --- | --- |
| Move | WASD | Left virtual stick |
| Look | Mouse | Drag right half |
| Attack | Left mouse | Attack button |
| Reload | R | Reload button |
| Sprint | Left Shift | Sprint button |
| Camera | V | Camera button |
| Pause / release mouse | Escape | Pause button |

The exploration map starts with only the entrance known. Traversed cells stay
marked so players can retrace their route, while undiscovered passages and the
exit remain hidden until reached. This eases navigation without solving the maze.

## Difficulty Curve

- Levels 1-3 teach navigation and combat.
- Level 4 introduces meaningful fog.
- Level 5 introduces elite zombies.
- Level 6 introduces a day/night cycle.
- Level 7 introduces miasma and safe-light zones.
- Levels 8-10 combine every system with larger mazes and higher pressure.

Every level uses a different deterministic maze seed and always has a valid
path. The generator evaluates multiple candidates using route length, turns,
dead ends and junctions; later levels use larger grids and more candidate
selection so complexity increases as part of the difficulty curve.
Enemy spawns exclude the start area and cells immediately around the player.
As of v1.2.2, safe lights use a separate seeded shuffle over all maze cells,
with a minimum two-cell spacing. The entrance remains a refuge; lamps in side
routes and dead ends are not clues to the exit. Placement does not read the
solution. Automated exposure checks audit the four default miasma levels;
they do not guarantee survival while fighting or exploring every wrong turn.

## Release Boundary

Version 1.0.0 includes the complete ten-level single-player campaign. Online
multiplayer, inventory crafting, live-service progression and paid assets are
explicitly outside the release scope.
