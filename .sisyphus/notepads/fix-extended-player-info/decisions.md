# Decisions

## Dictionary Location
- Put `_dataMap` in `ExtendedPlayerInfo` class (not `ExtendedPlayerControl` as initially suggested)
- Rationale: BetterData() extension methods are in the same file (PlayerControlDataExtension), keeping lookup logic co-located with the data class

## Cleanup Strategy in UpdateAll()
- Uses deferred removal: collects stale keys during iteration, removes after loop
- Prevents `Collection was modified` exceptions during foreach
- Matches the original MonoExtensionManager.CleanAll() pattern

## Coroutine for BetterDataWait
- Replaced `MonoExtensionManager.RunWhenNotNull<T>` with simple `StartCoroutine` polling
- Avoids breaking the MonoExtensionManager (which other MonoBehaviours may still use)
- Simpler, no generic constraint issues
