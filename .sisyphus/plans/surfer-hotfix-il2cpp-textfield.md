# Surfer Hotfix: Remove IL2CPP-Unsupported TextField Calls

## TL;DR

> **Quick Summary**: `GUILayout.TextField` is not available in Among Us's IL2CPP build — the method was stripped. Every call throws `Method unstripping failed` and breaks IMGUI rendering for the frame. Replace all 5 TextField calls with Labels or remove them.
>
> **Deliverables**:
> - `src/Mono/SurferMenu.cs` — all `GUILayout.TextField` replaced with `GUILayout.Label` (numeric display) or removed (keyword/ban input)
>
> **Estimated Effort**: Quick (2 tasks, parallel)

---

## Context

### Root Cause

BepInEx log shows:
```
[Error :Il2CppInterop] Exception in IL2CPP-to-Managed trampoline:
System.NotSupportedException: Method unstripping failed
   at UnityEngine.GUI.DoTextField(Rect position, Int32 id, GUIContent content, Boolean multiline, Int32 maxLength, GUIStyle style, String secureText, Char maskChar)
   at Surfer.SurferMenu.DrawHostTab()
   at Surfer.SurferMenu.DrawWindow(Int32 id)
```

`GUILayout.TextField` ultimately calls `UnityEngine.GUI.DoTextField`, which was stripped from the IL2CPP-compiled Among Us binary. This method is never used by the vanilla game, so the IL2CPP linker removed it.

There are 5 TextField calls in `SurferMenu.cs`, all added by the UI improvements changes:
1. Line 166: AutoKickThreshold numeric input → Host tab blank
2. Line 359: DrawOptionSlider numeric input → Anti-Cheat tab blank  
3-5. Lines 386, 444, 478: Keyword/BanName/BanWord add inputs → sub-windows crash if opened

---

## TODOs

- [x] 1. **Replace TextField with Label in sliders**

  **What to do**:
  - Edit `src/Mono/SurferMenu.cs`:
  
  **Fix 1a — DrawHostTab, line 166** (AutoKickThreshold):
  Replace:
  ```csharp
  string numStr = GUILayout.TextField(newVal.ToString(), 4, GUILayout.Width(35));
  if (int.TryParse(numStr, out int parsed) && parsed >= 0 && parsed <= 100)
      newVal = parsed;
  ```
  With:
  ```csharp
  GUILayout.Label(newVal.ToString(), GUILayout.Width(35));
  ```
  (Remove the TryParse block since there's no user-editable input anymore)

  **Fix 1b — DrawOptionSlider, lines 358-361** (Min Level to Detect, Min Level to Kick):
  Replace:
  ```csharp
  // Numeric text input for direct editing
  string numStr = GUILayout.TextField(newVal.ToString(), 6, GUILayout.Width(45));
  if (int.TryParse(numStr, out int parsed) && parsed >= 0 && parsed <= 10000)
      newVal = parsed;
  ```
  With:
  ```csharp
  GUILayout.Label(newVal.ToString(), GUILayout.Width(45));
  ```
  (Numeric input is not possible in IL2CPP — users must use the slider)

  **Must NOT do**:
  - Do NOT remove the slider itself
  - Do NOT change any layout widths or positions

  **Recommended Agent Profile**: `quick`
  **Parallel**: YES (with T2)

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -n "GUILayout.TextField" src/Mono/SurferMenu.cs | grep -c "DrawHostTab\|DrawOptionSlider"` returns 0 (no TextField in these methods)
  - [ ] Slider value labels still display correctly

  **QA Scenarios**:
  ```
  Scenario: No TextField in slider methods
    Tool: Bash (grep)
    Steps:
      1. grep -A 30 "private void DrawHostTab" src/Mono/SurferMenu.cs | grep "TextField"
      2. Assert: 0 matches
      3. grep -A 50 "private static void DrawOptionSlider" src/Mono/SurferMenu.cs | grep "TextField"
      4. Assert: 0 matches
    Evidence: .sisyphus/evidence/task-il2cpp-fix1.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Evidence: .sisyphus/evidence/task-il2cpp-fix1-build.txt
  ```

  **Commit**: NO (groups with T2)

---

- [x] 2. **Remove TextField from sub-windows (keywords, ban names, ban words)**

  **What to do**:
  - Edit `src/Mono/SurferMenu.cs`:
  
  Since `GUILayout.TextField` is not available in IL2CPP, remove the Add text inputs from the sub-windows. Users can manage entries via the text files directly or via chat commands (`/removeplayer` already exists).
  
  **Fix 2a — DrawKeywordsWindow (lines 385-394)**:
  Remove the "Add" row entirely:
  ```csharp
  // REMOVE these lines:
  GUILayout.BeginHorizontal();
  _newKeyword = GUILayout.TextField(_newKeyword, GUILayout.Width(180));
  GUI.backgroundColor = PurpleOn;
  if (GUILayout.Button("Add", GUILayout.Width(60)) && !string.IsNullOrWhiteSpace(_newKeyword))
  {
      BetterDataManager.AddKeyword(_newKeyword.Trim());
      _newKeyword = "";
  }
  GUI.backgroundColor = PurpleOn;
  GUILayout.EndHorizontal();
  ```
  Replace with a simple note:
  ```csharp
  GUILayout.Label("Keywords (edit AntiBotKeywords.txt to add):");
  ```
  
  **Fix 2b — DrawBanNameWindow (lines 443-452)**:
  Same pattern — remove Add row, add note:
  ```csharp
  GUILayout.Label("Names (edit BanNameList.txt to add):");
  ```
  
  **Fix 2c — DrawBanWordWindow (lines 477-486)**:
  Same pattern:
  ```csharp
  GUILayout.Label("Words (edit BanWordList.txt to add):");
  ```
  
  **Also clean up unused fields** (lines 19-20):
  Remove or comment out `_newKeyword`, `_newBanName`, `_newBanWord` since they're no longer used for TextField input. Or leave them — they won't cause issues if unused, just a warning.

  **Must NOT do**:
  - Do NOT remove the list display or X (remove) buttons
  - Do NOT remove the sub-windows themselves
  - Do NOT remove `_showKeywordsWindow`, `_showBanPlayerWindow`, etc.

  **Recommended Agent Profile**: `quick`
  **Parallel**: YES (with T1)

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -c "GUILayout.TextField" src/Mono/SurferMenu.cs` returns 0 (NO TextField calls anywhere)
  - [ ] `grep -n "DrawKeywordsWindow\|DrawBanNameWindow\|DrawBanWordWindow" src/Mono/SurferMenu.cs` returns 3 matches (windows still exist)
  - [ ] `grep -n "GUILayout.Button.*X.*GUILayout.Width(30)" src/Mono/SurferMenu.cs` returns ≥ 4 matches (remove buttons preserved)

  **QA Scenarios**:
  ```
  Scenario: Zero TextField calls in entire file
    Tool: Bash (grep)
    Steps:
      1. grep -c "GUILayout.TextField" src/Mono/SurferMenu.cs
      2. Assert: 0
    Evidence: .sisyphus/evidence/task-il2cpp-fix2-textfield.txt

  Scenario: Sub-windows still have remove buttons
    Tool: Bash (grep)
    Steps:
      1. grep -c 'Button("X"' src/Mono/SurferMenu.cs
      2. Assert: count ≥ 4 (keywords, ban players, ban names, ban words)
    Evidence: .sisyphus/evidence/task-il2cpp-fix2-xbuttons.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Evidence: .sisyphus/evidence/task-il2cpp-fix2-build.txt
  ```

  **Commit**: NO (groups with T1)

---

- [x] 3. **Build, deploy, final verify**

  **What to do**:
  - `dotnet build src/Surfer.csproj --configuration Release`
  - Copy to plugins
  - Verify zero TextField calls and all tabs render

  **Commit**: YES
  - Message: `fix: remove GUILayout.TextField (stripped from IL2CPP build), replace with Label`

---

## Success Criteria

```bash
# Zero TextField calls
grep -c "GUILayout.TextField" src/Mono/SurferMenu.cs
# Expected: 0

# Remove buttons preserved
grep -c 'Button("X"' src/Mono/SurferMenu.cs
# Expected: ≥ 4

# Build
dotnet build src/Surfer.csproj --configuration Release
# Expected: exit 0
```
