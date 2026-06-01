# Learnings - Restore Full IL2CPP Registration

## Summary
Restored registering ALL MonoBehaviours for IL2CPP injection (not just SurferMenu) and added `SurferBehaviour` base class to prevent the finalization crash.

## What was done
1. **SurferPlugin.cs** - Replaced single-type registration with full assembly scan loop using reflection. All non-abstract MonoBehaviours are now registered with error isolation (try-catch per type).
2. **SurferBehaviour.cs** (new) - Abstract base class inheriting MonoBehaviour that calls `GC.SuppressFinalize(this)` in `OnDestroy` to prevent IL2CPP `ClassInjector.Finalize` crashes on destroyed native handles.
3. **SurferMenu.cs** - Changed inheritance from `MonoBehaviour` to `SurferBehaviour`.

## Key details
- ImplicitUsings is enabled in .csproj (net6.0), so no explicit `using System.Linq;` or `using System.Reflection;` needed.
- Only SurferMenu uses `SurferBehaviour` for now — it's DontDestroyOnLoad and most likely to crash during scene transitions.
- Other MonoBehaviours are scene-specific and get destroyed normally by Unity.
- 10 MonoBehaviours + 1 abstract base = 11 total registered types.

## Verification
- `dotnet build` (Debug + Release): 0 errors
- DLL auto-deployed to `/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/`
