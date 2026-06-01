# Surfer: Custom Numeric Input via Event.current (IL2CPP-safe)

## TL;DR

> **Core Insight**: `GUILayout.TextField` calls `GUI.DoTextField` which is stripped from IL2CPP Among Us. But `Event.current.keyCode`, `Event.keyCode`, and `EventType.KeyDown` ARE available — the codebase already uses `Event.current.type == EventType.ScrollWheel` successfully at line 54 of `SurferMenu.cs`. We can build a custom numeric-only text input using keyboard events.
>
> **How it works**: Click a number → enters "edit mode" → keystrokes captured via `Event.current` → digits append to buffer → Enter commits → Escape cancels. Rendered as a styled `GUILayout.Label` with cursor underscore. Zero dependency on stripped Unity methods.

---

## Architecture

### Custom Input State
Add fields to `SurferMenu` class:
```csharp
private bool _editingNumber;           // true when user is typing a number
private int _editMin, _editMax;        // value range for validation
private string _editBuffer = "";       // what the user has typed so far
private System.Action<int> _editCommit; // callback when user presses Enter
```

### Keyboard Capture (in OnGUI, before visibility check)
```csharp
private void OnGUI()
{
    // === Handle keyboard input for number editing (must run even when menu hidden) ===
    if (_editingNumber)
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                // Commit
                if (int.TryParse(_editBuffer, out int val))
                {
                    val = Math.Clamp(val, _editMin, _editMax);
                    _editCommit?.Invoke(val);
                }
                _editingNumber = false;
                _editBuffer = "";
                e.Use();
            }
            else if (e.keyCode == KeyCode.Escape)
            {
                _editingNumber = false;
                _editBuffer = "";
                e.Use();
            }
            else if (e.keyCode == KeyCode.Backspace && _editBuffer.Length > 0)
            {
                _editBuffer = _editBuffer.Substring(0, _editBuffer.Length - 1);
                e.Use();
            }
            else if (e.keyCode >= KeyCode.Alpha0 && e.keyCode <= KeyCode.Alpha9)
            {
                _editBuffer += (char)('0' + (e.keyCode - KeyCode.Alpha0));
                e.Use();
            }
            else if (e.keyCode >= KeyCode.Keypad0 && e.keyCode <= KeyCode.Keypad9)
            {
                _editBuffer += (char)('0' + (e.keyCode - KeyCode.Keypad0));
                e.Use();
            }
        }
    }
    
    if (!_visible) return;
    // ... rest of existing OnGUI
}
```

### Clickable Value Display (in slider methods)
Replace the static `GUILayout.Label(newVal.ToString(), ...)` with a clickable display:

```csharp
// Instead of: GUILayout.Label(newVal.ToString(), GUILayout.Width(40));
// Use:
GUI.backgroundColor = _editingNumber ? new Color32(80, 80, 0, 255) : new Color32(40, 40, 60, 255);
string display = _editingNumber ? (_editBuffer.Length > 0 ? _editBuffer + "_" : "_") : val.ToString();
if (GUILayout.Button(display, GUILayout.Width(45), GUILayout.Height(20)))
{
    _editingNumber = true;
    _editBuffer = val.ToString();  // pre-fill with current value
    _editMin = min;
    _editMax = max;
    _editCommit = (v) => { /* commit to OptionItem or ConfigEntry */ };
}
GUI.backgroundColor = PurpleOn;
```

---

## TODOs

- [x] 1. **Add custom numeric input system to SurferMenu** (`src/Mono/SurferMenu.cs`)

  **What to do**:
  
  **Step 1**: Add fields to `SurferMenu` class (near line 23):
  ```csharp
  private bool _editingNumber;
  private string _editBuffer = "";
  private int _editMin, _editMax;
  private System.Action<int>? _editCommit;
  ```

  **Step 2**: Add keyboard capture at the TOP of `OnGUI()` (before `if (!_visible) return;` at line 51):
  ```csharp
  private void OnGUI()
  {
      // Custom numeric input via keyboard events (IL2CPP-safe — TextField is stripped)
      if (_editingNumber)
      {
          Event e = Event.current;
          if (e != null && e.type == EventType.KeyDown)
          {
              if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
              {
                  if (int.TryParse(_editBuffer, out int v))
                  {
                      v = Math.Clamp(v, _editMin, _editMax);
                      _editCommit?.Invoke(v);
                  }
                  _editingNumber = false;
                  _editBuffer = "";
                  e.Use();
              }
              else if (e.keyCode == KeyCode.Escape)
              {
                  _editingNumber = false;
                  _editBuffer = "";
                  e.Use();
              }
              else if (e.keyCode == KeyCode.Backspace && _editBuffer.Length > 0)
              {
                  _editBuffer = _editBuffer.Remove(_editBuffer.Length - 1);
                  e.Use();
              }
              else if (e.keyCode >= KeyCode.Alpha0 && e.keyCode <= KeyCode.Alpha9)
              {
                  if (_editBuffer == "0" && _editBuffer.Length == 1)
                      _editBuffer = "";
                  _editBuffer += (char)('0' + (e.keyCode - KeyCode.Alpha0));
                  e.Use();
              }
              else if (e.keyCode >= KeyCode.Keypad0 && e.keyCode <= KeyCode.Keypad9)
              {
                  if (_editBuffer == "0" && _editBuffer.Length == 1)
                      _editBuffer = "";
                  _editBuffer += (char)('0' + (e.keyCode - KeyCode.Keypad0));
                  e.Use();
              }
          }
      }
      
      if (!_visible) return;
      // ... rest unchanged
  }
  ```

  **Step 3**: Replace value labels in ALL 4 sliders with clickable edit buttons:
  
  **a) DrawOptionSlider** (Anti-Cheat tab — Min Level to Detect, Min Level to Kick, Impostor Count):
  Replace `GUILayout.Label(newVal.ToString(), GUILayout.Width(45))` with:
  ```csharp
  bool editingThis = _editingNumber && (_editMin == 0 && _editMax == 10000); // or use actual range
  GUI.backgroundColor = editingThis ? new Color32(80, 80, 20, 255) : new Color32(40, 40, 60, 255);
  string display = editingThis ? (_editBuffer + "_") : newVal.ToString();
  if (GUILayout.Button(display, GUILayout.Width(45), GUILayout.Height(20)))
  {
      _editingNumber = true;
      _editBuffer = newVal.ToString();
      _editMin = 0;
      _editMax = 10000;
      int capturedVal = newVal;
      _editCommit = (v) => item.SetValue(v);
  }
  GUI.backgroundColor = PurpleOn;
  ```
  
  **b) AutoKickThreshold** (Host tab):
  Same pattern — replace `GUILayout.Label(newVal.ToString(), GUILayout.Width(35))` / the old TextField with clickable button. Range: 0-100.
  
  **c) DetectedLevelAbove** (Anti-Cheat tab):
  Range: 100-10000. Instead of hardcoding, read min/max from OptionIntItem or set explicitly.
  
  **d) KickLevelBelow** (Anti-Cheat tab):
  Range: 0-10000.
  
  **e) HideAndSeekImpNum** (Anti-Cheat tab):
  Range: 1-5.
  
  **Step 4 — Slider alignment fix (surgical)**:
  Add cached `TextAnchor.MiddleLeft` GUIStyle (from the surgical plan) to keep labels vertically aligned with sliders.

  **Step 5 — Tab overflow fix (surgical)**:
  Use `GUILayout.Width(440f / _tabs.Count)` for tab buttons.

  **Step 6 — Sub-window overlap fix**:
  Add `CloseAllSubWindows()` helper, call before any sub-window toggle.

  **Step 7 — Clipboard keyword add**:
  "Add from Clipboard" button in `DrawKeywordsWindow`, `DrawBanNameWindow`, `DrawBanWordWindow`.

  **Must NOT do**:
  - Do NOT use `GUILayout.TextField` anywhere
  - Do NOT rename tabs
  - Do NOT use magic padding/space offsets

  **Recommended Agent Profile**: `unspecified-high` (significant single-file changes)
  **Parallel**: YES (with T2)

  **References**:
  - `src/Mono/SurferMenu.cs` — full file: keyboard-safe `Event.current` usage at line 54 proves Event API works in IL2CPP
  - `KeyCode` enum — all values (Alpha0-9, Keypad0-9, Backspace, Return, Escape) are standard Unity enums, not stripped

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -c "GUILayout.TextField" src/Mono/SurferMenu.cs` → 0
  - [ ] `grep -c "_editingNumber\|_editBuffer\|_editCommit" src/Mono/SurferMenu.cs` → ≥ 5 matches
  - [ ] `grep -c "EventType.KeyDown" src/Mono/SurferMenu.cs` → ≥ 1
  - [ ] `grep -c "KeyCode.Alpha0\|KeyCode.Keypad0" src/Mono/SurferMenu.cs` → ≥ 2
  - [ ] `grep -c "TextAnchor.MiddleLeft" src/Mono/SurferMenu.cs` → 1
  - [ ] `grep -c "440f / _tabs.Count" src/Mono/SurferMenu.cs` → 1
  - [ ] `grep -c "CloseAllSubWindows" src/Mono/SurferMenu.cs` → ≥ 4
  - [ ] `grep -c "Add from Clipboard" src/Mono/SurferMenu.cs` → ≥ 3

  **QA Scenarios**:
  ```
  Scenario: Custom input system present
    Tool: Bash (grep)
    Steps:
      1. grep -c "_editingNumber" src/Mono/SurferMenu.cs → ≥ 3
      2. grep -c "EventType.KeyDown" src/Mono/SurferMenu.cs → ≥ 1
      3. grep -c "KeyCode.Alpha0" src/Mono/SurferMenu.cs → ≥ 1
      4. Assert: all ≥ 1
    Evidence: .sisyphus/evidence/task-inp1-system.txt

  Scenario: No TextField usage
    Tool: Bash
    Steps:
      1. grep -c "GUILayout.TextField" src/Mono/SurferMenu.cs
      2. Assert: 0
    Evidence: .sisyphus/evidence/task-inp1-notextfield.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Evidence: .sisyphus/evidence/task-inp1-build.txt
  ```

- [x] 2. **Fix leave game crash** (`src/Patches/Gameplay/Player/PlayerJoinAndLeftPatch.cs`)

  - Add `if (HudManager.Instance == null) return;` at top of `BetterShowNotification`
  
  **QA**: `grep "HudManager.Instance == null" src/Patches/Gameplay/Player/PlayerJoinAndLeftPatch.cs` → 1 match

- [x] 3. **Build, deploy, verify**

  - Build, copy DLL, verify all grep checks pass

---

## Success Criteria
```bash
# No TextField
grep -c "GUILayout.TextField" src/Mono/SurferMenu.cs && echo "✗" || echo "✓"

# Custom input system
grep -c "EventType.KeyDown.*_editingNumber\|_editingNumber.*EventType" src/Mono/SurferMenu.cs || \
grep -c "_editingNumber" src/Mono/SurferMenu.cs
# Expected: ≥ 3

# All 6 fixes present
echo "--- Tabs ---" && grep -c "440f / _tabs.Count" src/Mono/SurferMenu.cs
echo "--- Align ---" && grep -c "TextAnchor.MiddleLeft" src/Mono/SurferMenu.cs
echo "--- Overlap ---" && grep -c "CloseAllSubWindows" src/Mono/SurferMenu.cs
echo "--- Clipboard ---" && grep -c "Add from Clipboard" src/Mono/SurferMenu.cs
echo "--- Crash ---" && grep -c "HudManager.Instance == null" src/Patches/Gameplay/Player/PlayerJoinAndLeftPatch.cs

dotnet build src/Surfer.csproj --configuration Release && echo "✓ BUILD" || echo "✗ BUILD"
```
