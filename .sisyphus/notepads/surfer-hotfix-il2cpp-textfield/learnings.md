# Learnings: Remove GUILayout.TextField from SurferMenu.cs

## Problem
- `GUILayout.TextField` calls `UnityEngine.GUI.DoTextField` internally
- `DoTextField` is stripped from Among Us IL2CPP build (not preserved at compile time)
- Any TextField call throws `Method unstripping failed` and breaks IMGUI rendering

## Changes Made
Replaced all 5 `GUILayout.TextField` calls in `src/Mono/SurferMenu.cs`:

1. **DrawHostTab — AutoKickThreshold** (slider value display): TextField → Label
2. **DrawOptionSlider** (slider value display): TextField → Label
3. **DrawKeywordsWindow** (add keyword input): Add row → static note label
4. **DrawBanNameWindow** (add name input): Add row → static note label
5. **DrawBanWordWindow** (add word input): Add row → static note label

Removed unused fields: `_newKeyword`, `_newBanName`, `_newBanWord`

## What Works
- Sliders still display their current value via `GUILayout.Label`
- All X (remove) buttons preserved in sub-windows
- All list displays preserved
- Build passes with 0 errors

## What Changed UX-wise
- AutoKickThreshold and OptionSlider values are now read-only display (no direct number entry)
- Keywords/BanName/BanWord sub-windows now show a note directing users to edit the txt file directly instead of an inline Add button
