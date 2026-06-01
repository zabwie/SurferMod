# Surfer: Binary-Search Crash Culprit

## TL;DR

> Surfer causes the crash, but NOT in the ExitGame postfix (traces prove it completes). The crash is in Among Us's native ExitGame code, triggered by game state Surfer modified. Binary-search: disable suspected patches one at a time, test after each.

---

## Suspects (ordered by likelihood)

| # | Patch | Why Suspect |
|---|-------|------------|
| 1 | `HudManagerPatch` (HudManager.Start) | Deactivates original ChatNotifications, creates DontDestroyOnLoad notification object with possibly stale scene refs |
| 2 | `ZoomPatch` (HudManager.Update) | Modifies Camera.main every frame — camera in bad state during exit |
| 3 | `SurferMenu` GameObject | DontDestroyOnLoad MonoBehaviour with OnGUI running every frame |
| 4 | `PlayerJoinAndLeftPatch` (OnPlayerLeft/HandleDisconnect) | Disconnect handler might do something before crash |

---

## TODO

- [x] 1. **Test Suspect #1: Disable HudManagerPatch**

  Comment out the `[HarmonyPatch]` attribute on `HudManagerPatch` class in `/home/zabwie/Desktop/surferMod/src/Patches/Gameplay/Managers/HudManagerPatch.cs`:
  ```csharp
  // [HarmonyPatch]   ← COMMENT THIS OUT
  internal static class HudManagerPatch
  ```
  
  Build, deploy, test leave game.
  
  **If crash GONE**: HudManagerPatch is the cause. We'll fix it.
  **If crash STILL happens**: Restore the attribute, move to suspect #2.

- [x] 2. **Test Suspect #2: Disable ZoomPatch**

  Comment out `[HarmonyPatch]` on `ZoomPatch` class in `src/Patches/Gameplay/ZoomPatch.cs`.
  
  Build, deploy, test.

- [x] 3. **Test Suspect #3: Disable SurferMenu creation**

  In `src/SurferPlugin.cs` lines 155-160, comment out the menu creation:
  ```csharp
  // var menuObj = new GameObject("SurferMenu");
  // ...
  ```
  
  Build, deploy, test.

- [x] 4. **Report which suspect eliminated the crash**

  Tell me which patch caused it. I'll then create a surgical fix for just that patch.
