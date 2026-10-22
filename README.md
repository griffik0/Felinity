# Felinity

A cozy, singleplayer, top-down 2D cat life-sim built in Unity 6. Create your cat, explore a cat village, forage, complete quests, earn CatCoins, and upgrade your cat tree home, all before the sun goes down.

> **Status:** Early development (v0.0.1). The core gameplay loop is playable; art, content, and polish are in progress.

---

## Concept

Everything in the world is cat-sized and cat-themed. There are no human tools or props: you pounce, scratch, nudge, and forage with paws and whiskers.

**Core loop:** explore → forage and scratch → complete quests → earn CatCoins → upgrade your cat tree → return home before curfew.

## Features

### Playable now
- **Character creation:** pick a coat color and name, with a live animated preview. Coat colors use a palette-swap shader, so one set of sprites supports every color.
- **8-directional movement** with direction-aware idle and walk animations and a smooth-follow Cinemachine camera.
- **Day/night cycle:** a 24-hour in-game clock drives a 2D global light. Get home before night or you'll "doze off" and be carried back to your cat tree.
- **Interaction system:** a shared `IInteractable` interface powers scratching posts, forage patches, NPCs, and your home.
- **Foraging:** foraged items physically pop out of the bush and chase you when you're close. Walk away and they wait on the ground until you return.
- **Inventory:** a 7-slot toolbar plus a 24-slot general inventory in a Stardew-style grid with item icons, stack counts, and hover tooltips. Items fill the toolbar first, then overflow to the general inventory.
- **Quests:** ScriptableObject-driven quests with a live quest log.
- **NPC dialogue:** Gerald, an elderly grey tabby, offers a "collect 3 Wild Berries" quest through a sequential dialogue system.
- **Cat tree progression:** Cardboard Box → Basic Scratching Post → Cozy Cat Tree → Luxury Cat Tower, paid for with CatCoins and Sturdy Branches.
- **Save/load:** JSON saves covering coins, inventory, cat appearance, tree tier, quest state, and world position, with a Continue button on the title screen.
- **Audio:** fading background music and a central SFX manager with hooks across every major interaction. SFX clips are being added.
- **Menus:** title screen, pause menu, and inventory overlay.
- **World:** PixelLab-generated tilesets (grass, dirt path, water) with water collision.

### Planned
- Achievements
- Cosmetics and a CatCoin shop
- More NPCs, quests, and village buildings (cat-designed shops and cottages)
- Cat-native activities such as Pond Pouncing (fishing alternative), digging, and toy play
- NPC daily walking routes
- Expanded map and a full art polish pass

## Controls

| Action | Input |
|---|---|
| Move | `WASD` / Arrow keys |
| Interact | `E` |
| Select toolbar slot | Mouse wheel or `1`-`7` |
| Open/close inventory | `Tab` |
| Pause | `Esc` |

## Tech Stack

| Area | Tool |
|---|---|
| Engine | Unity 6 (6000.4.2f1), **Universal 2D (URP 2D Renderer)** |
| Camera | Cinemachine (orthographic, top-down) |
| Art | [PixelLab](https://pixellab.ai) (AI pixel art), Aseprite (cleanup) |
| Music | [ACE-Step](https://github.com/ace-step/ACE-Step) |
| Sound effects | ElevenLabs SFX |
| Dev assistant | Cursor, connected via [MCP for Unity](https://github.com/CoplayDev/unity-mcp) |

## Getting Started

### Prerequisites
- Unity Hub with **Unity 6000.4.2f1** (or a compatible Unity 6 version)
- [Git LFS](https://git-lfs.com) (binary assets are tracked with LFS)

### Setup
```bash
git lfs install
git clone https://github.com/<your-username>/<your-repo>.git
```
1. Open the project folder in Unity Hub.
2. Let Unity import assets and compile scripts on first launch (this can take a few minutes).
3. Open `Assets/Scenes/TitleScreen.unity` and press **Play**.

### Scene order
`TitleScreen` → `CharacterCreation` → `MainWorld`

## Project Structure

```
Assets/
├── Art/
│   ├── Sprites/        # Cat, Props, Icons, Tiles
│   └── Animations/
├── Audio/
│   └── SFX/
├── Editor/             # One-off utility scripts (e.g. sprite scaling tools)
├── Prefabs/
├── Scenes/
├── Scripts/
│   ├── Player/         # Movement, appearance, toolbar, curfew
│   ├── Systems/        # Inventory, saving, audio, day/night, UI
│   ├── Quests/         # Quest data and manager
│   └── NPC/            # Dialogue and NPC controllers
└── Shaders/            # Palette-swap sprite shader
```

## Architecture Notes

- **Singletons** (`InventorySystem`, `QuestManager`, `DayNightCycle`, `SaveManager`, `MusicManager`, `SFXManager`, `HomeUpgradeManager`, etc.) communicate through C# events rather than direct references.
- **ScriptableObjects** define items (`ItemData`), quests (`QuestData`), and sound clips (`SFXLibrary`). Saves reference them by **name string**, since ScriptableObjects can't be serialized directly to JSON.
- **Map and gameplay are separate layers.** PixelLab output is ground art only (Tilemaps). All interactive objects (scratching posts, forage patches, the cat tree, NPCs) are prefabs placed by hand in Unity with their own colliders and scripts.
- **Collision:** the water Tilemap layer has `Tilemap Collider2D` + `Composite Collider2D` + a Static `Rigidbody2D`. Walkable layers have no collider.

## Art Pipeline

All art is generated with PixelLab in a consistent Stardew Valley-inspired style and cleaned up in Aseprite.

- Characters use **Quadruped / Standard mode**; props and icons use the **Objects / Simple Creator** tool.
- Terrain tilesets use **Flat shading** (Basic shading caused a raised-rim artifact at material boundaries).
- Structures like docks and bridges are static props, not tilesets.
- A shared 24-color palette is planned for a unified polish pass.

## Roadmap

- [x] Movement, camera, day/night, curfew
- [x] Interaction, foraging, inventory, toolbar
- [x] Quests and NPC dialogue
- [x] Cat tree upgrades
- [x] Character creation
- [x] Save/load
- [x] Music and SFX framework
- [x] Tilemap world with water collision
- [ ] Remaining cat tree tier art
- [ ] Village buildings and map decoration
- [ ] Achievements
- [ ] Cosmetics and shop
- [ ] Additional NPCs and quests
- [ ] SFX assets and day/night music variants
- [ ] Palette polish pass

## Contributing

This is a personal project and isn't currently accepting contributions, but feedback and bug reports are welcome via Issues.

## License

TBD. Add a license before accepting outside contributions or distributing builds.

## Credits

- Pixel art generated with [PixelLab](https://pixellab.ai)
- Music generated with [ACE-Step](https://github.com/ace-step/ACE-Step)
- Sound effects generated with ElevenLabs
- Inspired by *Stardew Valley*
