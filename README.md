# Surfer

### Better Among Us... but better.

A client-sided mod that enhances the experience for the popular game Among Us!

<p align="center">
  <a href="https://github.com/zabwie/SurferMod" target="_blank">
    <img src="https://img.shields.io/badge/GitHub%20-%23181717.svg?&style=for-the-badge&logo=github&logoColor=white" width="200" height="50"/>
  </a>
  &nbsp;
  <a href="https://discord.gg/vjYrXpzNAn" target="_blank">
    <img src="https://img.shields.io/badge/Discord%20-%235662f6.svg?&style=for-the-badge&logo=discord&logoColor=white" width="200" height="50"/>
  </a>
</p>

---

## Installation

### First-Time Installation:

1. **Download the Latest Release** from the [Releases](https://github.com/zabwie/SurferMod/releases) page
2. **Extract the Files** to a temporary location
3. **Install to Among Us Folder**:
   - Navigate to your Among Us installation directory
   - Copy ALL files and folders from the extracted zip into your Among Us folder
   - Overwrite any existing files when prompted
4. **Launch Among Us** — you should see "Surfer" in the main menu

### Updating:

1. Go to [Releases](https://github.com/zabwie/SurferMod/releases) and download the latest `Surfer.dll`
2. Replace the existing DLL at `BepInEx/plugins/Surfer.dll`
3. Launch Among Us

---

## Supported Platforms

| Platform | Status |
|----------|--------|
| Steam | ✅ |
| Epic Games | ✅ |
| Microsoft Store | ✅ |
| itch.io | ✅ |
| Android | ⚠️ Unknown |
| iOS | ❌ |
| Xbox / PlayStation / Switch | ❌ |

---

## Features

- **Built-in Client-Sided Anti-Cheat** — Detect and prevent unauthorized actions and RPC calls
- **ImGUI Settings Menu** — Toggle all features from a clean in-game overlay (press `Delete` to open)
- **Host Tools** — Auto-kick low-level players, anti-bot keyword detection, ban lists
- **Chat Improvements** — Longer messages (120 chars), clipboard support, bypass URL censorship, lower rate limits
- **Quality of Life** — Custom zoom, FPS unlock, dark mode chat, lobby player info, lobby timer
- **Commands** — Type `/commands` in chat for the full list
- **Mod News** — In-game announcements from the mod team

---

## Commands

| Command | Description |
|---------|-------------|
| `/help` | Get help with commands |
| `/commands` | List all available commands |
| `/dump` | Dump the entire log to your desktop |
| `/player {id}` | Get a player's information |
| `/players` | Get all player information |
| `/kick {id}` | Kick a player (Host Only) |
| `/ban {id}` | Ban a player (Host Only) |
| `/endgame` | Force end the game (Host Only) |
| `/removeplayer {id}` | Remove player from Anti-Cheat data |
| `/removeall` | Remove all players from Anti-Cheat data |

---

## Anti-Cheat

Surfer includes a built-in client-sided anti-cheat that can:

- Detect and cancel invalid actions and RPCs
- Identify known cheat clients
- Maintain ban lists (player, name pattern, word filter)
- Save cheat detection data for future reference
- All settings configurable from the ImGUI menu

---

## Credits

Surfer is based on **BetterAmongUs**, originally created by:

- **[D1GQ](https://github.com/D1GQ)** — Original head developer

Rebuilt and maintained by the Surfer team.

---

## Disclaimer

**Surfer** is an unofficial, fan-made mod for **Among Us**. It is not affiliated with, endorsed by, or associated with **InnerSloth LLC** or the official **Among Us** game. All trademarks and copyrights related to **Among Us** are the property of **InnerSloth LLC**. Use of this mod is at your own risk.

---

**Important**: Always ensure your game version is supported. Check the [Releases](https://github.com/zabwie/SurferMod/releases) page for compatibility information.
