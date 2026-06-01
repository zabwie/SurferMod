# Learnings

## Conversion Pattern: MonoBehaviour → Plain C# Class
- Followed the same pattern as `ExtendedPlayerControl` (already a plain class)
- Key changes:
  1. Remove `SurferBehaviour` / `IMonoExtension` inheritance
  2. Remove `Awake()` / `OnDestroy()` lifecycle methods
  3. Convert `Update()` → static `UpdateAll()` with dictionary iteration
  4. Use static `Dictionary` for O(1) `BetterData()` lookup
  5. Call `UpdateAll()` from `ModManagerPatch.LateUpdate`

## HandshakeHandler Coroutine
- `WaitSendSecretToPlayer` previously used `extendedData.StartCoroutine(...)` where `extendedData` was the MonoBehaviour EPI
- Changed parameter to accept a `MonoBehaviour coroutineRunner` — passed `data` (NetworkedPlayerInfo, which is a MonoBehaviour)

## BetterData() Lookup Change
- Previously: `MonoExtensionManager.Get<ExtendedPlayerInfo>(data)` via IMonoExtension registration
- Now: `ExtendedPlayerInfo._dataMap.TryGetValue(data, out var epi)`
- All 68 callers work unchanged (same return type, same properties)

## BetterDataWait() Change
- Previously: used `MonoExtensionManager.RunWhenNotNull<ExtendedPlayerInfo>(...)` (requires IMonoExtension constraint)
- Now: uses `player.StartCoroutine(CoBetterDataWait(...))` — simple polling coroutine
- Requires `using BepInEx.Unity.IL2CPP.Utils;` for `StartCoroutine(IEnumerator)` extension

## Registration
- Removed `if (type.Name == "ExtendedPlayerInfo") continue;` skip from `SurferPlugin.RegisterAllMonoBehavioursInAssembly()`
- EPI no longer a MonoBehaviour, so it won't appear in the assembly scan anyway
- Full registration restored
