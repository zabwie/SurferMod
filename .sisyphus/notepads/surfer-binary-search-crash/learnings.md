
## 2026-06-01: Binary Search Round 1 — Excluded PlayerInfoDisplay (IL2CPP Class Registration)

- Modified `RegisterAllMonoBehavioursInAssembly()` in `SurferPlugin.cs` (line ~202) to skip `PlayerInfoDisplay`: `if (type.Name == "PlayerInfoDisplay") continue;`
- Build clean with `dotnet build -c Release` — 0 errors
- DLL auto-deployed to: `/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/Surfer.dll`
- **Next step for user**: Launch game, test leaving a game — if crash persists, `PlayerInfoDisplay` is NOT the culprit → move to next suspect (`ExtendedPlayerControl`). If crash gone, `PlayerInfoDisplay` was the cause.
