# MalumMenu LowKick Implementation Analysis

## Repository Location
- "LowKick" feature is NOT in main `scp222thj/MalumMenu` repo
- It's in the fork `ApeMV/AmongUsRevamped` (ApeMV is a top MalumMenu contributor)
- Clone URL: https://github.com/ApeMV/AmongUsRevamped
- Branch: main
- HEAD SHA (at analysis time): 3c380c1b518448cc5e5db5e8026937813e30cabf

## Core Source File
- **File**: `Patches/JoinAndLeavePatches.cs` (lines 116-142)
- **Class**: `SetLevelPatch` - Harmony Prefix on `PlayerControl.SetLevel(uint level)`
- **Permalink**: https://github.com/ApeMV/AmongUsRevamped/blob/3c380c1b518448cc5e5db5e8026937813e30cabf/Patches/JoinAndLeavePatches.cs#L116-L142

## Config Options (defined in `Modules/CustomOptionsHolder.cs`)
- `KickLowLevelPlayer` (Integer 0-100, default 0): Level threshold
- `TempBanLowLevelPlayer` (Boolean, default false): Ban instead of kick
- `DontKickLevelOnes` (Boolean, default false): Skip level 1 for glitched lobbies

## Implementation Details
1. **Level comparison**: `level < Options.KickLowLevelPlayer.GetInt() - 1`
   - Adjusts for Among Us storing level 0-indexed (displayed level = stored level + 1)
   - Config value 0 = feature disabled

2. **Kick vs Ban**: `AmongUsClient.Instance.KickPlayer(clientId, false)` = kick, `true` = ban
   - Controlled by `TempBanLowLevelPlayer` option

3. **No cooldown/rate limiting**: Only `HandledLevelKicks` list prevents duplicate kicks

4. **Host protection**: Explicit check `__instance.Data.ClientId != AmongUsClient.Instance.HostId`

5. **Level 1 exception**: `DontKickLevelOnes` skips if `level == 0` (displayed level 1)

## AutoKickStart (separate feature)
- Also in `ChatPatches.cs` (line 364)
- Kicks players who spam "start"/"begin" in chat
- Has rate limiting via `AutoKickStartTimes` (configurable threshold)
- Configurable kick vs ban via `AutoKickStartAsBan`
