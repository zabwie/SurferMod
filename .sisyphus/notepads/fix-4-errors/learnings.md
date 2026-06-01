# Learnings: Fix 4 Known Surfer Errors

## Changes Made

### 1. ChatPatch NRE (ChatPatch.cs:134)
- Added null guard for `bubble`, `playerInfo`, and `PlayerControl.LocalPlayer` before accessing them in `SetChatBubbleName_Postfix`
- Prevents NRE when these are null during rapid chat events

### 2. Il2CppInterop "Handle is not initialized" crash (SurferPlugin.cs)
- Replaced `RegisterAllMonoBehavioursInAssembly()` (which scanned assembly for all MonoBehaviours) with explicit registration of `SurferMenu` only
- Root cause: Registering 13 MonoBehaviours creates finalizers for each; when IL2CPP objects get GC'd with broken handles → crash in `ClassInjector.Finalize`

### 3. MenuCreationHelper removed (SurferPlugin.cs)
- Replaced `AddComponent<MenuCreationHelper>()` deferred approach with direct `SurferMenu` creation via `GameObject.AddComponent<SurferMenu>()`
- Removed `MenuCreationHelper` nested class entirely
- Cleaned up unused usings: `System.Reflection`, `System.Collections`, `BepInEx.Unity.IL2CPP.Utils.Collections`

### 4. DrawOptionSlider static (SurferMenu.cs:410)
- Changed from instance method to static to avoid Il2CppInterop proxying issues
- Required making 5 edit-state fields static (`_editingNumber`, `_editBuffer`, `_editMin`, `_editMax`, `_editCommit`) — safe because only one SurferMenu exists

## Key Insight
When making a method static in IL2CPP-injected classes, all accessed instance fields must also be static. This is safe for singleton GUIs.
