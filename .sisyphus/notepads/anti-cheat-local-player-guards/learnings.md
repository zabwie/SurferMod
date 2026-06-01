# Anti-Cheat Local Player Guards

## Summary
Added `IsLocalPlayer()` guards to all anti-cheat detection points that could kick/ban/flag the mod user.

## Files Changed: 5

### 1. `src/Modules/AntiCheat/BetterAntiCheat.cs` (4 guards)
- **`Update()`** — Added `if (player.IsLocalPlayer()) continue;` in the iteration loop to prevent the local player from being kicked by their own cheat data matching
- **`CheckRPC()`** — Changed `player.IsLocalPlayer() && player.IsHost()` → `player.IsLocalPlayer()` to protect all local players, not just hosts
- **`CheckCancelRPC()`** — Changed `player.IsLocalPlayer() && player.IsHost()` → `player.IsLocalPlayer()` to protect all local players, not just hosts
- **`HandleCheatRPCBeforeCheck()`** — Added `if (player == null || player.IsLocalPlayer()) return;` to prevent cheat RPC detection from firing on the local player

### 2. `src/Patches/Gameplay/Anticheat/PlatformSpoofPatch.cs` (1 guard)
- Added `if (player.IsLocalPlayer()) return;` after finding the player from platform data to prevent reporting/flagging the local player for platform spoofing

### 3. `src/Modules/AntiCheat/RPCHandlers/SendChatHandler.cs` (1 guard)
- Added `if (sender.IsLocalPlayer()) return;` at the start of `Handle()` to prevent kicking the local player for using banned words

### 4. `src/Mono/ExtendedPlayerInfo.cs` (1 guard)
- Added `if (epi._Data.Object.IsLocalPlayer()) continue;` in the RPC sent PS tracking loop to prevent flagging the local player

### 5. `src/Patches/Gameplay/Player/PlayerJoinAndLeftPatch.cs` (1 guard)
- Added `if (player.IsLocalPlayer()) return;` before ban list checks in `OnPlayerJoined` to prevent the host's own ban list from affecting them

## Files Already Guarded (no changes needed):
- `BetterNotificationManager.NotifyCheat` — Already had `player.IsLocalPlayer() return false`
- `AntiBotPatch` — Already had `sourcePlayer.PlayerId == LocalPlayer.PlayerId`
- `AutoKickPatch` — Already had `player.PlayerId == local.PlayerId`
- `CheckPlayerLevelPatch` — Already had `!__instance.IsLocalPlayer()`
- `BetterAntiCheat.HandleRPC` — Already had `player.IsLocalPlayer()`
- `NetworkManager.PlayerRpc` — Already had `!player.IsLocalPlayer()` in log guard
- `VoteBanSystemPatch` — Goes through NotifyCheat which is already guarded

## Total Guards Added: 8 (across 5 files)
