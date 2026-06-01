# Surfer: Fix Lobby Loading While Keeping Crash Fix

## TL;DR

> Agent fixed leave-game crash by only registering `SurferMenu` for IL2CPP injection. But 9 other MonoBehaviours are created via `AddComponent<T>()` at runtime and MUST be registered. Without registration, they can't be created → lobby never loads.
>
> **Fix**: Restore registering ALL MonoBehaviours. Prevent the finalization crash (original leave-game issue) by suppressing GC finalization in OnDestroy.

---

## Root Cause

The agent correctly identified the leave-game crash: IL2CPP's `ClassInjector.Finalize` crashes when garbage collecting registered MonoBehaviours whose native Unity handles are already destroyed (scene change). The fix was to only register `SurferMenu`.

But these 9 MonoBehaviours are created at runtime via `AddComponent<T>()` and MUST be registered:

| MonoBehaviour | Created by | What it does |
|---|---|---|
| `UpdateManager` | `UpdateManager.cs:37` | Update system |
| `BetterPingTracker` | `PingTrackerPatch.cs:16` | Ping/HUD overlay |
| `MeetingInfoDisplay` | `MeetingHudPatch.cs:21` | Meeting info |
| `AnimatedMapIcon` | `MiniMapBehaviourPatch.cs:118` | Minimap icons |
| `ExtendedPlayerControl` | `ExtendedPlayerControl.cs:94` | Player extension |
| `PlayerInfoDisplay` | `ExtendedPlayerControl.cs:29` | Player nameplate info |
| `ExtendedPlayerInfo` | `ExtendedPlayerControl.cs:55` | Player data extension |
| `GithubAPI` | `GithubAPI.cs:46` | Update checker |
| `NewsLoader` | `GithubAPI.cs:60` | News loader |
| `UpdateLoader` | `GithubAPI.cs:63` | Update loader |

Without registration, `AddComponent<T>()` silently fails → half the mod doesn't initialize → black screen.

---

## TODO

- [x] 1. **Restore registering all MonoBehaviours + fix finalization crash** (`src/SurferPlugin.cs`)

  Replace the current `RegisterAllMonoBehavioursInAssembly()` (lines 195-200) with:
  ```csharp
  private static void RegisterAllMonoBehavioursInAssembly()
  {
      var assembly = System.Reflection.Assembly.GetExecutingAssembly();
      var monoBehaviourTypes = assembly.GetTypes()
          .Where(type => type.IsSubclassOf(typeof(MonoBehaviour)) && !type.IsAbstract)
          .OrderBy(type => type.Name);
      
      foreach (var type in monoBehaviourTypes)
      {
          try
          {
              ClassInjector.RegisterTypeInIl2Cpp(type);
          }
          catch (Exception ex)
          {
              Logger_.Error($"Failed to register MonoBehaviour: {type.FullName}\n{ex}");
          }
      }
  }
  ```
  
  Restore `using System.Reflection;` at the top of the file.

  **Also add a base MonoBehaviour that suppresses finalization** for ALL Surfer components to prevent the `Handle is not initialized` crash on scene change. Add a new file:
  
  `src/Mono/SurferBehaviour.cs`:
  ```csharp
  using UnityEngine;
  
  namespace Surfer;
  
  /// <summary>
  /// Base class for all Surfer MonoBehaviours.
  /// Suppresses GC finalization to prevent IL2CPP ClassInjector.Finalize crashes
  /// when native Unity handles are destroyed during scene changes.
  /// </summary>
  public abstract class SurferBehaviour : MonoBehaviour
  {
      protected virtual void OnDestroy()
      {
          System.GC.SuppressFinalize(this);
      }
  }
  ```
  
  Then change `SurferMenu` to inherit from `SurferBehaviour` instead of `MonoBehaviour`:
  ```csharp
  // public class SurferMenu : MonoBehaviour  ← OLD
  public class SurferMenu : SurferBehaviour     // ← NEW
  ```
  
  Only SurferMenu needs this now since it's DontDestroyOnLoad and most likely to hit the finalization crash. Other MonoBehaviours are scene-specific and get destroyed by Unity normally.

  **Must NOT do**:
  - Do NOT register types that aren't MonoBehaviours
  - Do NOT change other MonoBehaviours to inherit SurferBehaviour (only SurferMenu for now)

  **QA**: `dotnet build` exits 0. `grep "RegisterTypeInIl2Cpp" src/SurferPlugin.cs` shows the loop registering all types.

- [x] 2. **Build, deploy, test**

  ```bash
  dotnet build src/Surfer.csproj --configuration Release
  cp src/bin/Release/net6.0/Surfer.dll "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/"
  ```
  
  Test:
  - Join a lobby → should load normally (not black screen)
  - Click Leave Game → should NOT crash

---

## Success Criteria

```bash
# All MonoBehaviours registered
grep -A 15 "RegisterAllMonoBehavioursInAssembly" src/SurferPlugin.cs | grep -c "RegisterTypeInIl2Cpp"
# Expected: ≥ 1 (the loop line)

# SurferMenu inherits SurferBehaviour
grep "class SurferMenu" src/Mono/SurferMenu.cs | grep "SurferBehaviour"
# Expected: 1

# GC.SuppressFinalize exists
grep -c "GC.SuppressFinalize" src/Mono/SurferBehaviour.cs
# Expected: 1

dotnet build src/Surfer.csproj --configuration Release
# Expected: exit 0
```
