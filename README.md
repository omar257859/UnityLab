# Unity 2D Labs (Unity 2021.3.10f1)

## First time only
1. Open the project in Unity Hub.
2. Menu **Labs → Create Level 1** (creates `Assets/Scenes/Level1`).
3. Open `Assets/Scenes/Level1` and press **Play**. ← → move, Space jump, mouse wheel zoom.

After that, Level1 is a normal scene: add the next labs to it by hand.
**Labs → Create Level 1** never overwrites an existing Level1.

## Folders
| Folder | What's inside |
|---|---|
| `Assets/Scenes` | Level1 |
| `Assets/Scripts` | All lab scripts (one file per script, like the slides) |
| `Assets/Art` | Course art: Bobby, ground, sky, clouds, water, terrain, tree, bushes, mushroom |
| `Assets/Animations` | Bobby animations + Animator (Lab 3) |
| `Assets/Materials` | Rough Material + Bounciness (Lab 4) |
| `Assets/Particles` | Particle materials (Lab 4) |
| `Assets/Prefabs` | Red Mushroom (checkpoint), Slime (enemy) |
| `Assets/Editor` | The one-time "Create Level 1" tool |

## Which lab is which object in Level1
| Lab | Objects | Scripts |
|---|---|---|
| 2 | Main Platform, Bobby, groundCheck, Main Camera | PlayerController |
| 3 | Bobby's Animator (Idle, Run, JumpingMotion blend tree) | PlayerController |
| 4 | Bouncy Platform, Floaty Platform 1, Lake/Water Glitter, camera | CameraFollow, CameraZoom |
| 5 | LevelManager, Red Mushroom (+2), Lake, Drop, Trapdoor, BrickBlock, Moving Platform | LevelManager, Checkpoint, DeadlyDrop, BrickBreak, RisingPlatform |
| 6 | Slime between Tiny Wall 1/2, Bobby's PlayerStats | PlayerStats, EnemyController, SlimeEnemy |

## Notes
- `jumpHeight` is 11 so Bobby can reach the platforms with gravity scale 2.
- No enemy art was provided, so the Slime uses a placeholder sprite; drag your own into its Sprite Renderer.
- If the trapdoor opens on its own, change its Hinge Joint 2D **Motor Speed** from −100 to 100.
- `_old_version_backup` (outside Assets) is the previous version; delete it when you don't need it.
