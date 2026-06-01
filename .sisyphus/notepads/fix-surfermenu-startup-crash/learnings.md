# fix-surfermenu-startup-crash - Learnings

## Problem
`ClassInjector.Finalize` crash with "Handle is not initialized" during `AddComponent<SurferMenu>()` in `OnChainloaderFinished`. The IL2CPP interop injection infrastructure isn't fully initialized at that point.

## Solution
- Created a nested `MenuCreationHelper : MonoBehaviour` inside `SurferPlugin`
- Uses `Start()` (fires on next frame) + coroutine (waits 2 more frames) = ~3 frame total delay
- `AddComponent<MenuCreationHelper>()` is called from `OnChainloaderFinished` via `BasePlugin.AddComponent<T>()`

## Key Technical Details

### IL2CPP Coroutine Pattern
In BepInEx 6 IL2CPP, `MonoBehaviour.StartCoroutine(IEnumerator)` expects `Il2CppSystem.Collections.IEnumerator`, not `System.Collections.IEnumerator`. To bridge this:
- Import `using BepInEx.Unity.IL2CPP.Utils.Collections;`
- Call `.WrapToIl2Cpp()` on the `System.Collections.IEnumerator` return value

### Why BasePlugin.AddComponent works here
`BasePlugin.AddComponent<T>()` creates the component through the same IL2CPP path as direct `GameObject.AddComponent<T>()`. The helper's `Start()` method is deferred to the next Unity frame, by which time IL2CPP interop has settled.

### Auto-Registration
`MenuCreationHelper` is auto-registered by `RegisterAllMonoBehavioursInAssembly()` because it's a non-abstract `MonoBehaviour` subclass in the assembly (found via `Assembly.GetTypes()`).

### NuGet Dependencies
- `BepInEx.Unity.IL2CPP.Utils` - base IL2CPP utilities
- `BepInEx.Unity.IL2CPP.Utils.Collections` - provides `WrapToIl2Cpp()` for IEnumerator conversion
