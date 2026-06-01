# Surfer Hotfix: Leave Game Crash + Host Tab Blank

## TL;DR

> **Quick Summary**: Fix two regressions: (1) NullReferenceException when pressing "Leave Game" in lobby due to missing null guard in `CustomLoadingBarManager`, (2) Host tab rendering blank because `FlexibleSpace` in unconstrained vertical group consumes all visible space, pushing slider off-screen.
>
> **Deliverables**:
> - `src/Managers/CustomLoadingBarManager.cs` — null guards on `ToggleLoadingBar` and `SetLoadingPercent`
> - `src/Mono/SurferMenu.cs` — remove vertical group wrapping on AutoKickThreshold slider; use simpler aligned layout
>
> **Estimated Effort**: Quick (2 tasks, parallel)

---

## Context

### Root Causes

**Leave Game Crash**: `CustomLoadingBarManager.ToggleLoadingBar()` at line 19:
```csharp
LoadingBarManager.Instance.loadingBar.gameObject.SetActive(on);
```
No null check. `LoadingBarManager.Instance` only exists during gameplay/loading, not in the lobby. Triggered by `ClientPatch.AmongUsClient_ExitGame_Postfix` when user leaves a lobby.

**Host Tab Blank**: `DrawHostTab()` lines 162-166:
```csharp
GUILayout.BeginVertical();     // NO height constraint
GUILayout.FlexibleSpace();     // Grabs 50% of ALL remaining vertical space
GUILayout.Label("Threshold:", GUILayout.Width(70));
GUILayout.FlexibleSpace();     // Grabs remaining 50%
GUILayout.EndVertical();
```
`GUILayout.FlexibleSpace` without a `GUILayout.Height` constraint consumes all available space in the parent ScrollView (455px). The vertical group balloons to fill the ScrollView, and the slider + text field that follow are pushed hundreds of pixels below the visible area.

---

## TODOs

- [x] 1. **Fix leave game crash: add null guards to CustomLoadingBarManager**

  **What to do**:
  - Edit `src/Managers/CustomLoadingBarManager.cs`:
  - Replace `ToggleLoadingBar` (lines 17-20) with null-safe version:
    ```csharp
    internal static void ToggleLoadingBar(bool on)
    {
        if (LoadingBarManager.Instance == null || LoadingBarManager.Instance.loadingBar == null) return;
        LoadingBarManager.Instance.loadingBar.gameObject.SetActive(on);
    }
    ```
  - Replace `SetLoadingPercent` (lines 27-32) with null-safe version:
    ```csharp
    internal static void SetLoadingPercent(float percent, string loadText)
    {
        if (LoadingBarManager.Instance == null || LoadingBarManager.Instance.loadingBar == null) return;
        var loadingBar = LoadingBarManager.Instance.loadingBar;
        loadingBar.SetLoadingPercent(percent, StringNames.None);
        loadingBar.loadingText.SetText(loadText);
    }
    ```

  **Must NOT do**:
  - Do NOT change method signatures
  - Do NOT remove the `ClientPatch` postfix call

  **Recommended Agent Profile**: `quick`
  **Parallel**: YES (with T2)

  **References**:
  - `src/Managers/CustomLoadingBarManager.cs` — current file (33 lines)
  - `src/Patches/Client/ClientPatch.cs` — line 59 calls `ToggleLoadingBar(false)`

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -n "LoadingBarManager.Instance == null" src/Managers/CustomLoadingBarManager.cs` returns ≥ 2 matches

  **QA Scenarios**:
  ```
  Scenario: Null guards present
    Tool: Bash (grep)
    Steps:
      1. grep -c "LoadingBarManager.Instance == null" src/Managers/CustomLoadingBarManager.cs
      2. Assert: count ≥ 2 (both ToggleLoadingBar and SetLoadingPercent)
    Evidence: .sisyphus/evidence/task-hotfix1-guards.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Evidence: .sisyphus/evidence/task-hotfix1-build.txt
  ```

  **Commit**: NO (groups with T2)

---

- [x] 2. **Fix Host tab blank: replace FlexibleSpace layout with simple alignment**

  **What to do**:
  - Edit `src/Mono/SurferMenu.cs`, `DrawHostTab()` method (lines 143-177):
  
  **Problem**: Lines 162-166 use `BeginVertical()` + two `FlexibleSpace()` calls without height constraint. The vertical group consumes ALL available vertical space, pushing the slider off-screen.
  
  **Fix**: Remove the `BeginVertical`/`EndVertical` wrapping. Use a standard horizontal layout with the label, slider, and text field inline — NO vertical centering:
  
  Replace lines 157-176 (the entire AutoKick slider block) with:
  ```csharp
  DrawToggle("Auto-Kick Low Level", SurferPlugin.AutoKick);
  if (SurferPlugin.AutoKick?.Value == true)
  {
      GUILayout.BeginHorizontal();
      GUILayout.Space(20);
      GUILayout.Label("Threshold:", GUILayout.Width(70));
      int val = SurferPlugin.AutoKickThreshold?.Value ?? 0;
      int newVal = (int)GUILayout.HorizontalSlider(val, 0, 100, GUILayout.Width(100));
      string numStr = GUILayout.TextField(newVal.ToString(), 4, GUILayout.Width(35));
      if (int.TryParse(numStr, out int parsed) && parsed >= 0 && parsed <= 100)
          newVal = parsed;
      GUILayout.EndHorizontal();
      if (newVal != val && SurferPlugin.AutoKickThreshold != null)
          SurferPlugin.AutoKickThreshold.Value = newVal;
  }
  ```
  
  **Also fix the AntiBot toggle wrapping** (lines 147-154): the `DrawToggle` call is wrapped inside an outer `BeginHorizontal`/`EndHorizontal`, which is unconventional and adds unnecessary nesting. While this isn't the blank-tab cause, simplify it to match the original pattern:
  
  Replace lines 147-154 with:
  ```csharp
  DrawToggle("Anti-Bot (Keyword Kick)", SurferPlugin.AntiBot);
  if (SurferPlugin.AntiBot?.Value == true)
  {
      GUILayout.BeginHorizontal();
      GUILayout.Space(24);
      if (GUILayout.Button("Keywords", GUILayout.Width(80), GUILayout.Height(20)))
          _showKeywordsWindow = !_showKeywordsWindow;
      GUILayout.EndHorizontal();
  }
  ```
  
  This places the Keywords button on its own row below the toggle, indented to align with the toggle label.

  **Must NOT do**:
  - Do NOT remove the numeric text field (TextInput) — keep it
  - Do NOT change the AntiBot/AutoKick toggles themselves
  - Do NOT touch `DrawOptionSlider` (the Anti-Cheat tab sliders use a different pattern that works)

  **Recommended Agent Profile**: `quick`
  **Parallel**: YES (with T1)

  **References**:
  - `src/Mono/SurferMenu.cs` — `DrawHostTab()` lines 143-177
  - `src/Mono/SurferMenu.cs` — `DrawToggle()` lines 274-293 (how toggles are structured)

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -n "FlexibleSpace" src/Mono/SurferMenu.cs` returns ≥ 2 matches (still present in DrawOptionSlider, removed from DrawHostTab)
  - [ ] `grep -n "GUILayout.Label.*Threshold" src/Mono/SurferMenu.cs` returns 1 match, and the next line does NOT contain `FlexibleSpace`

  **QA Scenarios**:
  ```
  Scenario: No FlexibleSpace in DrawHostTab
    Tool: Bash
    Steps:
      1. grep -A 10 "private void DrawHostTab" src/Mono/SurferMenu.cs | grep "FlexibleSpace"
      2. Assert: 0 matches (FlexibleSpace removed from Host tab)
    Evidence: .sisyphus/evidence/task-hotfix2-flexspace.txt

  Scenario: Slider and text field still present
    Tool: Bash (grep)
    Steps:
      1. grep -A 20 "Auto-Kick Low Level" src/Mono/SurferMenu.cs | grep "HorizontalSlider"
      2. Assert: 1 match
      3. grep -A 20 "Auto-Kick Low Level" src/Mono/SurferMenu.cs | grep "TextField"
      4. Assert: 1 match
    Expected Result: Slider and numeric input both preserved
    Evidence: .sisyphus/evidence/task-hotfix2-controls.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Evidence: .sisyphus/evidence/task-hotfix2-build.txt
  ```

  **Commit**: NO (groups with T1)

---

- [x] 3. **Build, copy, verify**

  **What to do**:
  - `dotnet build src/Surfer.csproj --configuration Release`
  - `cp src/bin/Release/net6.0/Surfer.dll "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/"`
  - Verify both fixes are in place

  **Recommended Agent Profile**: `quick`
  **Parallel**: NO (after T1, T2)

  **Acceptance Criteria**:
  - [ ] Build exits 0
  - [ ] `grep -c "LoadingBarManager.Instance == null" src/Managers/CustomLoadingBarManager.cs` → 2
  - [ ] `grep "GUILayout.Label.*Threshold" src/Mono/SurferMenu.cs` → no FlexibleSpace on next line

  **Commit**: YES
  - Message: `fix: null guard leave-game crash, fix host tab blank slider layout`

---

## Success Criteria

```bash
# Crash fix
grep -c "LoadingBarManager.Instance == null" src/Managers/CustomLoadingBarManager.cs
# Expected: 2

# Host tab fix  
grep -A 10 "private void DrawHostTab" src/Mono/SurferMenu.cs | grep -c "FlexibleSpace"
# Expected: 0

# Build
dotnet build src/Surfer.csproj --configuration Release
# Expected: exit 0
```
