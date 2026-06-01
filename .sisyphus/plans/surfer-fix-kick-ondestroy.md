# Surfer: Fix Game-Start Kick + Missing base.OnDestroy + Off-by-One

## TL;DR

> Three surgical fixes: (1) `PlayerInfoDisplay.ValidateFriendCode` kicks non-host players during game start because friend codes are empty while initializing — add `DataIsCollected` check. (2) 3 `OnDestroy` overrides never call `base.OnDestroy()` → `GC.SuppressFinalize` skipped → potential IL2CPP crash. (3) `CosmeticsLayerPatch` off-by-one: `>` should be `>=`.
>
> **Effort**: Quick (3 files)

---

## Issue 1: Kick During Game Start

**File**: `src/Mono/PlayerInfoDisplay.cs`, `ValidateFriendCode()` method

**Root cause**: When host starts a game, PlayerControls are destroyed and recreated. During the initialization window, `_player.Data.FriendCode` is null/empty. `TryKick()` fires on the host's machine for ALL non-host players with empty friend codes.

**Fix**: Add a guard before `TryKick()` — only kick if player data is fully collected:
```csharp
void TryKick()
{
    if (!GameState.IsHost || !BetterGameSettings.InvalidFriendCode.GetBool()) return;
    if (_player == null || _player.Data == null || !_player.Data.IsIncomplete == false) return; // wait for data
    string kickMessage = ...;
    _player.Kick(true, kickMessage, true);
}
```

The `_player.Data.IsIncomplete` check ensures data is fully loaded before validating. If data is still loading, skip the kick.

---

## Issue 2: Missing base.OnDestroy() in 3 Overrides

**Files**: `ExtendedPlayerControl.cs`, `ExtendedPlayerInfo.cs`, `BetterPingTracker.cs`

**Root cause**: These override `OnDestroy` but never call `base.OnDestroy()`. `SurferBehaviour.OnDestroy` calls `GC.SuppressFinalize(this)` to prevent IL2CPP finalization crashes. Without the base call, `GC.SuppressFinalize` never runs.

**Fix**: Add `base.OnDestroy()` as the FIRST line in each override:
```csharp
// ExtendedPlayerControl.cs line 60-63:
private void OnDestroy()
{
    base.OnDestroy();  // ← ADD THIS
    this.UnregisterExtension();
}

// ExtendedPlayerInfo.cs line 53-56:
private void OnDestroy()
{
    base.OnDestroy();  // ← ADD THIS
    this.UnregisterExtension();
}

// BetterPingTracker.cs line 92-98:
private void OnDestroy()
{
    base.OnDestroy();  // ← ADD THIS
    if (Instance == this) Instance = null;
}
```

---

## Issue 3: CosmeticsLayerPatch Off-by-One

**File**: `src/Patches/Gameplay/Player/CosmeticsLayerPatch.cs`, line 14

**Root cause**: `if (__instance.bodyMatProperties.ColorId > Palette.PlayerColors.Length) return true;` uses `>` instead of `>=`. If `ColorId == Palette.PlayerColors.Length`, the condition passes (it shouldn't), and line 16 accesses `Palette.PlayerColors[ColorId]` which throws `IndexOutOfRangeException`.

**Fix**: Change `>` to `>=`:
```csharp
if (__instance.bodyMatProperties.ColorId >= Palette.PlayerColors.Length) return true;
```

---

## TODOs

- [x] 1. **Fix kick + base.OnDestroy + off-by-one** (3 files)

  **File A**: `src/Mono/PlayerInfoDisplay.cs` — add `!_player.Data.IsIncomplete` check to `TryKick()`
  
  **File B**: `src/Mono/ExtendedPlayerControl.cs` — add `base.OnDestroy();` at top of `OnDestroy`
  
  **File C**: `src/Mono/ExtendedPlayerInfo.cs` — add `base.OnDestroy();` at top of `OnDestroy`
  
  **File D**: `src/Mono/BetterPingTracker.cs` — add `base.OnDestroy();` at top of `OnDestroy`
  
  **File E**: `src/Patches/Gameplay/Player/CosmeticsLayerPatch.cs` — `>` → `>=`

- [x] 2. **Build, deploy, test**

  ```bash
  dotnet build src/Surfer.csproj --configuration Release
  cp src/bin/Release/net6.0/Surfer.dll "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/"
  ```
  
  Test: Host starts game → non-host players should NOT be kicked.

---

## Success Criteria

```bash
grep -c "base.OnDestroy()" src/Mono/ExtendedPlayerControl.cs src/Mono/ExtendedPlayerInfo.cs src/Mono/BetterPingTracker.cs
# Expected: 3

grep "PlayerColors.Length" src/Patches/Gameplay/Player/CosmeticsLayerPatch.cs | grep ">="
# Expected: 1 match with >=

grep "IsIncomplete" src/Mono/PlayerInfoDisplay.cs
# Expected: ≥ 1 (new guard added)

dotnet build src/Surfer.csproj --configuration Release
# Expected: exit 0
```
