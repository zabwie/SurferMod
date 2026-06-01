# learnings.md

## T15 - Clean stale BetterAmongUs build artifacts

- Stale BetterAmongUs artifacts were found in both `bin/Release/net6.0/` and `obj/` directories
- Simple `rm -f` removal was sufficient to eliminate them
- After removal, `dotnet clean && dotnet build` succeeded with 0 errors (5 pre-existing warnings: NU1603 and CS0649)
- The build correctly outputs `Surfer.dll` (589824 bytes) and copies to the Among Us BepInEx plugins directory
- No stale BetterAmongUs.* files remain anywhere in bin/ or obj/
