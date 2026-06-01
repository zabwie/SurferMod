# Learnings - Surfer UI Improvements

## Slider + TextField pattern (2026-06-01)

- **Vertical centering in Unity IMGUI**: Wrap label in `BeginVertical()` + `FlexibleSpace()` + Label + `FlexibleSpace()` + `EndVertical()` to vertically center label text against a slider control.
- **Dual-input sliders**: Added `GUILayout.TextField` next to `HorizontalSlider` for direct numeric entry. Uses `int.TryParse` for safe conversion with range clamping.
- **Applied to**: `DrawOptionSlider` (range 0–10000) and AutoKickThreshold in `DrawHostTab` (range 0–100).
- **TextField widths**: 45px for 5-digit values (0–10000, max 6 chars), 35px for 3-digit values (0–100, max 4 chars).
- **Build**: `dotnet build src/Surfer.csproj --configuration Release` succeeds with 0 errors.
- **GUI.Window cast**: Unity IMGUI requires explicit `(GUI.WindowFunction)` cast on the callback parameter.
- **TextFileHandler.ReadContents is private**: Can't access from other namespaces. Inline file reading with comment filtering instead.
- **Keywords sub-window pattern**: Window ID 9970, positioned right of main window (`_windowRect.x + _windowRect.width + 10`), 300×350 size.
- **Keywords button**: Only shown when AntiBot toggle is enabled, uses `_showKeywordsWindow` toggle flag.
