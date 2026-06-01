# Surfer: Fix "Play Again" Crash — Extend SurferBehaviour to All 10 MonoBehaviours

## TL;DR

> Only `SurferMenu` inherits `SurferBehaviour` (with `GC.SuppressFinalize`). The other 9 MonoBehaviours still inherit plain `MonoBehaviour` — when destroyed during scene changes (game → play again → lobby), their IL2CPP finalizer crashes with `Handle is not initialized`. Change all 10 to inherit `SurferBehaviour`.
>
> **Effort**: Quick (10 files, mechanical change)

---

## Root Cause

When a game ends and "Play Again" loads a new lobby:
1. Game scene unloads → Unity destroys GameObjects
2. Attached MonoBehaviours (registered with ClassInjector) get destroyed
3. Their native handles become invalid
4. GC runs `ClassInjector.Finalize` with invalid handle → crash

`SurferMenu` is protected (inherits `SurferBehaviour` → `OnDestroy` calls `GC.SuppressFinalize`). These 9 are NOT:

| MonoBehaviour | Destroyed When |
|---|---|
| `UpdateManager` | Application quit |
| `BetterPingTracker` | Game scene unload |
| `MeetingInfoDisplay` | Meeting ends / scene unload |
| `AnimatedMapIcon` | Minimap unload / scene unload |
| `ExtendedPlayerControl` | Player GameObject destroyed |
| `PlayerInfoDisplay` | Player GameObject destroyed |
| `ExtendedPlayerInfo` | Player data destroyed |
| `GithubAPI` | Application quit |
| `NewsLoader` | GithubAPI destroyed |
| `UpdateLoader` | GithubAPI destroyed |

---

## TODO

- [x] 1. **Change all 9 MonoBehaviours to inherit SurferBehaviour**

  In each file below, change `: MonoBehaviour` → `: SurferBehaviour`. Add `using` if needed (SurferBehaviour is in `namespace Surfer`).

  | File | Line | Change |
  |------|------|--------|
  | `src/Managers/UpdateManager.cs` | ~16 | `MonoBehaviour` → `SurferBehaviour` |
  | `src/Mono/BetterPingTracker.cs` | ~14 | `MonoBehaviour` → `SurferBehaviour` |
  | `src/Mono/MeetingInfoDisplay.cs` | ~14 | `MonoBehaviour` → `SurferBehaviour` |
  | `src/Mono/AnimatedMapIcon.cs` | ~8 | `MonoBehaviour` → `SurferBehaviour` |
  | `src/Mono/ExtendedPlayerControl.cs` | ~13 | `MonoBehaviour` → `SurferBehaviour` |
  | `src/Mono/PlayerInfoDisplay.cs` | ~20 | `MonoBehaviour` → `SurferBehaviour` |
  | `src/Mono/ExtendedPlayerInfo.cs` | ~14 | `MonoBehaviour` → `SurferBehaviour` |
  | `src/Network/GithubAPI.cs` | ~12 | `MonoBehaviour` → `SurferBehaviour` |
  | `src/Network/Loaders/NewsLoader.cs` | ~15 | `MonoBehaviour` → `SurferBehaviour` |
  | `src/Network/Loaders/UpdateLoader.cs` | ~14 | `MonoBehaviour` → `SurferBehaviour` |

  **Must NOT do**:
  - Do NOT change any method bodies or other code
  - Do NOT change namespace declarations
  - Do NOT change `SurferMenu.cs` (already inherits SurferBehaviour)

- [x] 2. **Build, deploy, test**

  ```bash
  dotnet build src/Surfer.csproj --configuration Release
  cp src/bin/Release/net6.0/Surfer.dll "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/"
  ```
  
  Test: Play a game → "Play Again" → should load lobby without crashing.

  **QA**: `grep -r "SurferBehaviour" src/Mono/ src/Managers/ src/Network/ --include="*.cs" | wc -l` → ≥ 10

---

## Success Criteria

```bash
# All 10 MonoBehaviours inherit SurferBehaviour (SurferMenu + 9 others)
grep -r ": SurferBehaviour" src/ --include="*.cs" | wc -l
# Expected: ≥ 10

dotnet build src/Surfer.csproj --configuration Release
# Expected: exit 0
```
