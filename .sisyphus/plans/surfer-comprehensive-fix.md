# Surfer Comprehensive Fix: Crash, Alignment, Overlap, Input, Overflow, Keywords

## TL;DR

> **Quick Summary**: Fix 6 issues: (1) leave game NullReferenceException from disconnect handler, (2) slider label misalignment, (3) sub-window overlap (auto-close previous), (4) numeric input via +/- step buttons (TextField unavailable in IL2CPP), (5) tab overflow by shrinking names, (6) keyword editing via clipboard button.
>
> **Deliverables**:
> - `src/Patches/Gameplay/Player/PlayerJoinAndLeftPatch.cs` — null guards on disconnect handlers
> - `src/Mono/SurferMenu.cs` — slider alignment fix, +/- buttons on all sliders, sub-window auto-close, tab name shrink, clipboard-based keyword add
>
> **Estimated Effort**: Quick (2 files, 4 tasks)

---

## Context

### Issue 1: Leave Game Crash
BepInEx log shows repeated `NullReferenceException` without stack traces, and `[Info] Client has left game for: ExitGame` (CustomLoadingBarManager null guard works). The crash likely occurs in `PlayerJoinAndLeftPatch.BetterShowNotification()` which accesses `HudManager.Instance.Notifier` — null during scene transition when leaving.

### Issue 2: Slider Misalignment  
`DrawOptionSlider` wraps label in `BeginVertical` + `FlexibleSpace` ×2. The vertical group's content doesn't vertically center against the adjacent slider — label sits at baseline, slider naturally centers.

### Issue 3: Sub-Window Overlap
Opening a second sub-window (e.g., Ban Name List after Keywords) leaves the first one visible. They stack on top of each other.

### Issue 4: Numeric Input Unavailable
`GUILayout.TextField` is stripped from IL2CPP Among Us. Need +/- step buttons as alternative.

### Issue 5: Tab Overflow  
"Anti-Cheat" tab name (11 chars at 14pt bold in 28px button) causes 4 tabs to overflow 480px window width. ~120px per tab needed, only ~115px available each.

### Issue 6: Keyword Editing  
TextField unavailable. Workaround: read from system clipboard (`GUIUtility.systemCopyBuffer`) — already confirmed working via `CopyLobbyCodePatch`.

---

## TODOs

- [ ] 1. **Fix leave game crash + slider alignment + sub-window overlap + numeric buttons + tab overflow**

  **What to do**: Edit `src/Mono/SurferMenu.cs` — multi-part fix:
  
  **Part A — Sub-window auto-close (prevents overlap):**
  Add a helper method and use it in all sub-window toggle buttons:
  ```csharp
  private void CloseAllSubWindows()
  {
      _showKeywordsWindow = false;
      _showBanPlayerWindow = false;
      _showBanNameWindow = false;
      _showBanWordWindow = false;
  }
  ```
  Change every sub-window toggle to close others first:
  ```csharp
  // In DrawHostTab line 152:
  if (GUILayout.Button("Keywords", ...))
  {
      CloseAllSubWindows();
      _showKeywordsWindow = !_showKeywordsWindow;
  }
  // In DrawAntiCheatTab lines 194, 204, 213:
  CloseAllSubWindows();
  _showBanPlayerWindow = !_showBanPlayerWindow;
  // etc.
  ```
  
  **Part B — Slider alignment fix in DrawOptionSlider (lines 342-367):**
  Remove the `BeginVertical`+`FlexibleSpace` wrapping. Replace lines 348-353:
  ```csharp
  // OLD (remove):
  GUILayout.BeginVertical();
  GUILayout.FlexibleSpace();
  GUILayout.Label(label + ":", GUILayout.Width(145));
  GUILayout.FlexibleSpace();
  GUILayout.EndVertical();
  ```
  Replace with a simple label — accept Unity's natural ~2px offset, it's barely noticeable and much more stable:
  ```csharp
  GUILayout.Space(3);
  GUILayout.Label(label + ":", GUILayout.Width(140));
  ```
  This shifts the label down 3px to visually align with the slider's center. Same fix for the AutoKickThreshold label (line 163): add `GUILayout.Space(2)` before the `GUILayout.Label("Threshold:", GUILayout.Width(70))`.
  
  **Part C — Numeric +/- buttons on all sliders:**
  Replace the value label (`GUILayout.Label(newVal.ToString(), ...)`) in `DrawOptionSlider` (line 360) and `DrawHostTab` (line 166) with step buttons:
  
  For DrawOptionSlider (after the slider):
  ```csharp
  // Small step buttons next to value display
  int step = 1;
  // Determine good step size based on range
  if (maxVal >= 1000) step = 50;  // For 0-10000 range
  
  GUI.backgroundColor = new Color32(60, 60, 80, 255);
  if (GUILayout.Button("-", GUILayout.Width(22))) newVal = Math.Max(min, newVal - step);
  if (GUILayout.Button("-10", GUILayout.Width(28))) newVal = Math.Max(min, newVal - step * 10);
  GUI.backgroundColor = PurpleOn;
  
  GUILayout.Label(newVal.ToString(), GUILayout.Width(40));
  
  GUI.backgroundColor = new Color32(60, 60, 80, 255);
  if (GUILayout.Button("+10", GUILayout.Width(28))) newVal = Math.Min(max, newVal + step * 10);
  if (GUILayout.Button("+", GUILayout.Width(22))) newVal = Math.Min(max, newVal + step);
  GUI.backgroundColor = PurpleOn;
  ```
  
  For the AutoKickThreshold in DrawHostTab (after the slider at line 165):
  ```csharp
  int newVal = (int)GUILayout.HorizontalSlider(val, 0, 100, GUILayout.Width(80));
  
  GUI.backgroundColor = new Color32(60, 60, 80, 255);
  if (GUILayout.Button("-1", GUILayout.Width(25))) newVal = Math.Max(0, newVal - 1);
  if (GUILayout.Button("-5", GUILayout.Width(25))) newVal = Math.Max(0, newVal - 5);
  GUI.backgroundColor = PurpleOn;
  GUILayout.Label(newVal.ToString(), GUILayout.Width(25));
  GUI.backgroundColor = new Color32(60, 60, 80, 255);
  if (GUILayout.Button("+5", GUILayout.Width(25))) newVal = Math.Min(100, newVal + 5);
  if (GUILayout.Button("+1", GUILayout.Width(25))) newVal = Math.Min(100, newVal + 1);
  GUI.backgroundColor = PurpleOn;
  ```
  
  **Part D — Tab overflow fix:**
  Shrink tab names. Change lines 34-37:
  ```csharp
  _tabs.Add(("General", DrawGeneralTab));
  _tabs.Add(("Host", DrawHostTab));
  _tabs.Add(("A-Cheat", DrawAntiCheatTab));    // "Anti-Cheat" → "A-Cheat"
  _tabs.Add(("About", DrawAboutTab));
  ```
  
  **Must NOT do**:
  - Do NOT remove any existing controls or functionality
  - Do NOT change layout beyond what's specified

  **Recommended Agent Profile**: `unspecified-high` (multi-part changes in one file)
  **Parallel**: YES (with T2, T4)

  **References**:
  - `src/Mono/SurferMenu.cs` — full file (516 lines): slider alignment at 342-367 and 160-173, sub-window toggles at 152, 194, 204, 213, tab names at 34-37

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -n "CloseAllSubWindows" src/Mono/SurferMenu.cs` returns ≥ 4 matches (method + 3+ callers)
  - [ ] `grep -n "A-Cheat" src/Mono/SurferMenu.cs` returns 1 match
  - [ ] `grep -c "GUILayout.Button.*\"-\"\|GUILayout.Button.*\"+\"" src/Mono/SurferMenu.cs` returns ≥ 4 matches (+/- buttons added)
  - [ ] `grep -n "FlexibleSpace.*DrawOptionSlider\|FlexibleSpace.*DrawHostTab" src/Mono/SurferMenu.cs` — no FlexibleSpace near sliders

  **QA Scenarios**:
  ```
  Scenario: Sub-window auto-close implemented
    Tool: Bash (grep)
    Steps:
      1. grep -c "CloseAllSubWindows" src/Mono/SurferMenu.cs
      2. Assert: count ≥ 4
    Evidence: .sisyphus/evidence/task-comp1-close.txt

  Scenario: +/- buttons on all sliders
    Tool: Bash (grep)
    Steps:
      1. grep -c 'GUILayout.Button.*"[+-]' src/Mono/SurferMenu.cs
      2. Assert: count ≥ 8 (+/- buttons)
    Evidence: .sisyphus/evidence/task-comp1-buttons.txt

  Scenario: Tab names shrunk
    Tool: Bash (grep)
    Steps:
      1. grep "A-Cheat" src/Mono/SurferMenu.cs
      2. Assert: 1 match
    Evidence: .sisyphus/evidence/task-comp1-tabs.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Evidence: .sisyphus/evidence/task-comp1-build.txt
  ```

  **Commit**: NO (groups with all tasks)

---

- [ ] 2. **Add clipboard-based keyword Add button**

  **What to do**: Edit `src/Mono/SurferMenu.cs`:
  
  In `DrawKeywordsWindow()` (the sub-window for keywords), replace the removed TextField+Add row with a clipboard-based alternative:
  
  ```csharp
  GUILayout.BeginHorizontal();
  if (GUILayout.Button("Add from Clipboard", GUILayout.Width(150)))
  {
      string clip = GUIUtility.systemCopyBuffer;
      if (!string.IsNullOrWhiteSpace(clip))
      {
          // Split by newlines or commas to handle multiple keywords at once
          foreach (string kw in clip.Split('\n', '\r', ','))
          {
              string trimmed = kw.Trim();
              if (!string.IsNullOrWhiteSpace(trimmed))
                  BetterDataManager.AddKeyword(trimmed);
          }
      }
  }
  GUI.backgroundColor = PurpleOn;
  GUILayout.EndHorizontal();
  GUILayout.Space(4);
  GUILayout.Label("Copy keyword(s), then click above. Edit AntiBotKeywords.txt to bulk-manage.");
  ```
  
  Same pattern for `DrawBanNameWindow()` and `DrawBanWordWindow()` — "Add from Clipboard" button that reads `GUIUtility.systemCopyBuffer` and calls the appropriate `BetterDataManager.Add*()` method.

  **Must NOT do**:
  - Do NOT use TextField
  - Do NOT change the list display or remove buttons

  **Recommended Agent Profile**: `quick`
  **Parallel**: YES (with T1, T3, T4)

  **References**:
  - `src/Mono/SurferMenu.cs` — `DrawKeywordsWindow` (line 379+), `DrawBanNameWindow` (line 437+), `DrawBanWordWindow` (line 471+)
  - `src/Modules/CopyLobbyCodePatch.cs` — line 17 confirms `GUIUtility.systemCopyBuffer` works in IL2CPP Among Us

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -c "GUIUtility.systemCopyBuffer" src/Mono/SurferMenu.cs` returns ≥ 3 matches (keywords + ban names + ban words)
  - [ ] `grep -c "Add from Clipboard" src/Mono/SurferMenu.cs` returns ≥ 3 matches

  **QA Scenarios**:
  ```
  Scenario: Clipboard buttons in all 3 sub-windows
    Tool: Bash (grep)
    Steps:
      1. grep -c "Add from Clipboard" src/Mono/SurferMenu.cs
      2. Assert: count ≥ 3
    Evidence: .sisyphus/evidence/task-comp2-clipboard.txt

  Scenario: SystemCopyBuffer read access
    Tool: Bash (grep)
    Steps:
      1. grep -c "GUIUtility.systemCopyBuffer" src/Mono/SurferMenu.cs
      2. Assert: count ≥ 3
    Evidence: .sisyphus/evidence/task-comp2-syscopy.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Evidence: .sisyphus/evidence/task-comp2-build.txt
  ```

  **Commit**: NO (groups with all tasks)

---

- [ ] 3. **Fix leave game crash: null guard disconnect handlers**

  **What to do**: Edit `src/Patches/Gameplay/Player/PlayerJoinAndLeftPatch.cs`:
  
  In `BetterShowNotification()` method (around line 119), add null guards before accessing `HudManager.Instance`:
  
  ```csharp
  internal static void BetterShowNotification(NetworkedPlayerInfo playerData, DisconnectReasons reason = DisconnectReasons.Unknown, string forceReasonText = "")
  {
      // Guard: HudManager might be null during scene transitions (leave game)
      if (HudManager.Instance == null || HudManager.Instance.Notifier == null) return;
      
      // ... rest of existing method
  }
  ```
  
  Also check `AmongUsClient_OnPlayerLeft_Postfix` (line 75) — the `GameData_HandleDisconnect_Prefix` at line 99 accesses `player.BetterData()` which should be safe, but `BetterShowNotification` is the one that accesses HudManager.
  
  Also add a null guard in `AmongUsClient_OnPlayerLeft_Postfix` around the `BetterShowNotification` call:
  ```csharp
  if (!GameState.IsInGame) return;  // Skip notifications if not in game
  ```

  **Must NOT do**:
  - Do NOT remove disconnect notification logic
  - Do NOT change the translation/disconnect reason lookup

  **Recommended Agent Profile**: `quick`
  **Parallel**: YES (with T1, T2, T4)

  **References**:
  - `src/Patches/Gameplay/Player/PlayerJoinAndLeftPatch.cs` — `BetterShowNotification` at ~line 119, `OnPlayerLeft_Postfix` at ~line 75, `HandleDisconnect` at ~line 97

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -n "HudManager.Instance == null" src/Patches/Gameplay/Player/PlayerJoinAndLeftPatch.cs` returns ≥ 1 match

  **QA Scenarios**:
  ```
  Scenario: Null guard added to disconnect handler
    Tool: Bash (grep)
    Steps:
      1. grep -n "HudManager.Instance == null" src/Patches/Gameplay/Player/PlayerJoinAndLeftPatch.cs
      2. Assert: ≥ 1 match
    Evidence: .sisyphus/evidence/task-comp3-guard.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Evidence: .sisyphus/evidence/task-comp3-build.txt
  ```

  **Commit**: NO (groups with all tasks)

---

- [ ] 4. **Build, deploy, final verify**

  **What to do**:
  - `dotnet build src/Surfer.csproj --configuration Release`
  - `cp src/bin/Release/net6.0/Surfer.dll "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/"`
  - Verify all 6 fixes: auto-close, +/- buttons, clipboard add, tab names, null guards, alignment

  **Commit**: YES
  - Message: `fix: slider alignment, +/- buttons, sub-window overlap, clipboard keyword add, leave-game null guard`

---

## Success Criteria

```bash
# Auto-close sub-windows
grep -c "CloseAllSubWindows" src/Mono/SurferMenu.cs
# Expected: ≥ 4

# +/- step buttons
grep -c 'Button.*"[+-]' src/Mono/SurferMenu.cs
# Expected: ≥ 8

# Clipboard add
grep -c "GUIUtility.systemCopyBuffer" src/Mono/SurferMenu.cs  
# Expected: ≥ 3

# Tab names shrunk
grep -c "A-Cheat" src/Mono/SurferMenu.cs
# Expected: 1

# Null guard
grep -c "HudManager.Instance == null" src/Patches/Gameplay/Player/PlayerJoinAndLeftPatch.cs
# Expected: ≥ 1

# Build
dotnet build src/Surfer.csproj --configuration Release
# Expected: exit 0
```
