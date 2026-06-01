# Surfer Surgical Fix: Crash, Alignment, Overflow, Precision, Keywords

## TL;DR

> **Quick Summary**: Surgical fixes for 6 issues — root causes addressed, no band-aids. Tab overflow fixed by dynamic equal-width calculation (not renaming). Slider alignment fixed by vertically centering label text via `TextAnchor.MiddleLeft` GUIStyle (not magic padding). Numeric precision via wider sliders + minimal `[-1] [+1]` buttons (not TextField). Sub-window overlap via `CloseAllSubWindows`. Leave-game crash via null guard on disconnect handler. Keywords via clipboard button (only viable IL2CPP input).
>
> **Effort**: Quick (1 file gets most changes + 1 file gets 1 line)

---

## Issue-by-Issue Root Cause & Surgical Fix

### Issue 1: Leave Game Crash
- **Root cause**: `PlayerJoinAndLeftPatch.BetterShowNotification()` accesses `HudManager.Instance.Notifier` — `HudManager` gets destroyed during scene transition when player leaves, leaving `Instance` as null.
- **Fix**: Single null guard `if (HudManager.Instance == null) return;` at top of `BetterShowNotification`. No additional logic, no band-aids.

### Issue 2: Slider Label Misalignment
- **Root cause**: `GUILayout.Label` renders text at the **top** of its layout cell (baseline-aligned). `GUILayout.HorizontalSlider` renders centered in its cell. In a horizontal layout, different vertical positions = misalignment.
- **Fix**: Create a `GUIStyle` with `alignment = TextAnchor.MiddleLeft` for slider labels. This vertically centers the text within the label's cell, matching the slider's vertical center. **One GUIStyle change, not padding hacks.**

### Issue 3: Sub-Window Overlap
- **Root cause**: Each sub-window toggle only flips its own bool. Nothing closes the others.
- **Fix**: `CloseAllSubWindows()` helper that sets all 4 sub-window bools to false. Called before any sub-window toggle. **Clean, self-documenting.**

### Issue 4: Numeric Precision on Large Sliders
- **Root cause**: 100px slider ÷ 10,000 range = 100 levels per pixel. One pixel = jumping 100 levels. Even 200px only gives 50 levels/pixel. TextField doesn't work in IL2CPP.
- **Surgical fix**: (a) Widen slider to 160px for better coarse control (~62.5 levels/pixel). (b) Add minimal `[-1] [+1]` buttons next to the value for exact fine-tuning. Users coarse-adjust with slider, then press +/-1 to dial in the exact number. **Two buttons, not a row of 8.**

### Issue 5: Tab Overflow
- **Root cause**: Tab buttons use `GUILayout.Button(name, GUILayout.Height(28))` — no width constraint. Unity auto-sizes each to its content. "General" = ~60px, "Host" = ~45px, "Anti-Cheat" = ~85px, "About" = ~55px = ~245px total. But the problem is these are rounded UP and the bar itself takes space. In a 480px ScrollView, 4 auto-sized buttons with padding can overflow.
- **Surgical fix**: Instead of renaming or enlarging window, calculate equal tab widths from available space:
  ```csharp
  float tabWidth = 450f / _tabs.Count;  // 450px = window - padding
  GUILayout.Button(_tabs[i].name, GUILayout.Width(tabWidth), GUILayout.Height(28));
  ```
  Each tab gets exactly 112.5px. Text longer than that clips (Unity IMGUI auto-clips button text). **The underlying layout issue is solved, not patched over.**

### Issue 6: Keyword Editing Without TextField
- **Root cause**: `GUILayout.TextField` → `GUI.DoTextField` stripped from IL2CPP Among Us. No runtime text input available.
- **Surgical fix**: "Add from Clipboard" button that reads `GUIUtility.systemCopyBuffer` (confirmed working in IL2CPP via `CopyLobbyCodePatch`). User copies keyword(s) → clicks button → keywords added. **Leverages existing working API, not a workaround.**

---

## TODOs

- [ ] 1. **Fix slider alignment + numeric buttons + tab overflow + sub-window overlap + clipboard keywords** (all in `src/Mono/SurferMenu.cs`)

  **Part A — Slider alignment (root cause fix)**:
  
  Add a cached GUIStyle field to the class (near line 25, with the other static fields):
  ```csharp
  private static GUIStyle? _alignedLabelStyle;
  private static GUIStyle AlignedLabelStyle => _alignedLabelStyle ??= new GUIStyle(GUI.skin.label)
  {
      alignment = TextAnchor.MiddleLeft,
      fontSize = 14
  };
  ```
  
  Then in `DrawOptionSlider`, replace the label line (currently `GUILayout.Label(label + ":", GUILayout.Width(145))`) with:
  ```csharp
  GUILayout.Label(label + ":", AlignedLabelStyle, GUILayout.Width(140), GUILayout.Height(20));
  ```
  Same for the AutoKickThreshold label in `DrawHostTab` line 163: replace
  ```csharp
  GUILayout.Label("Threshold:", GUILayout.Width(70));
  ```
  with:
  ```csharp
  GUILayout.Label("Threshold:", AlignedLabelStyle, GUILayout.Width(70), GUILayout.Height(20));
  ```
  
  **Part B — Numeric precision on sliders**:
  
  In `DrawOptionSlider`: widen the slider from `GUILayout.Width(100)` to `GUILayout.Width(160)`. Add `[-1] [+1]` buttons after the value label:
  ```csharp
  int newVal = (int)GUILayout.HorizontalSlider(val, 0, 10000, GUILayout.Width(160), GUILayout.Height(20));
  
  // Fine-tune buttons
  GUI.backgroundColor = new Color32(60, 60, 80, 255);
  if (GUILayout.Button("-1", GUILayout.Width(28))) newVal = Math.Max(0, newVal - 1);
  GUI.backgroundColor = PurpleOn;
  GUILayout.Label(newVal.ToString(), GUILayout.Width(40));
  GUI.backgroundColor = new Color32(60, 60, 80, 255);
  if (GUILayout.Button("+1", GUILayout.Width(28))) newVal = Math.Min(10000, newVal + 1);
  GUI.backgroundColor = PurpleOn;
  ```
  
  In `DrawHostTab` AutoKickThreshold: widen slider from `GUILayout.Width(100)` to `GUILayout.Width(140)`. Add `[-1] [+1]` buttons after value label (same pattern, max=100).
  
  **Part C — Tab overflow fix (equal-width tabs)**:
  
  Remove the auto-sized button line at line 97:
  ```csharp
  // OLD:
  if (GUILayout.Button(_tabs[i].name, GUILayout.Height(28)))
  // NEW:
  float tabWidth = 440f / _tabs.Count;
  if (GUILayout.Button(_tabs[i].name, GUILayout.Width(tabWidth), GUILayout.Height(28)))
  ```
  
  **Part D — Sub-window overlap fix**:
  
  Add helper method anywhere in the class:
  ```csharp
  private void CloseAllSubWindows()
  {
      _showKeywordsWindow = false;
      _showBanPlayerWindow = false;
      _showBanNameWindow = false;
      _showBanWordWindow = false;
  }
  ```
  
  Wrap every sub-window toggle button click with `CloseAllSubWindows()`:
  - Line 152 (Keywords): `CloseAllSubWindows(); _showKeywordsWindow = !_showKeywordsWindow;`
  - Line 194 (BanPlayer): same pattern
  - Line 204 (BanName): same pattern
  - Line 213 (BanWord): same pattern
  
  **Part E — Clipboard keyword add**:
  
  In `DrawKeywordsWindow`, replace the removed TextField row with:
  ```csharp
  GUILayout.BeginHorizontal();
  GUILayout.Label("Copy keyword, then:", GUILayout.Width(110));
  if (GUILayout.Button("Add from Clipboard", GUILayout.Width(140)))
  {
      string clip = GUIUtility.systemCopyBuffer;
      if (!string.IsNullOrWhiteSpace(clip))
      {
          foreach (string kw in clip.Split('\n', '\r', ','))
          {
              string t = kw.Trim();
              if (!string.IsNullOrWhiteSpace(t))
                  BetterDataManager.AddKeyword(t);
          }
      }
  }
  GUILayout.EndHorizontal();
  ```
  Same pattern for `DrawBanNameWindow` (call `BetterDataManager.AddBanName`) and `DrawBanWordWindow` (call `BetterDataManager.AddBanWord`).

  **Must NOT do**:
  - Do NOT rename any tabs or add magic pixel offsets
  - Do NOT use `FlexibleSpace` or `BeginVertical` to center labels
  - Do NOT remove existing controls
  - Do NOT use `GUILayout.TextField` anywhere

  **Recommended Agent Profile**: `unspecified-high` (multi-part single-file)
  **Parallel**: YES (with T2, T3)

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -c "TextAnchor.MiddleLeft" src/Mono/SurferMenu.cs` → 1 (GUIStyle fix, one definition)
  - [ ] `grep -c "AlignedLabelStyle" src/Mono/SurferMenu.cs` → ≥ 2 (cached + used in both slider methods)
  - [ ] `grep -c "CloseAllSubWindows" src/Mono/SurferMenu.cs` → ≥ 4
  - [ ] `grep -c "440f / _tabs.Count" src/Mono/SurferMenu.cs` → 1
  - [ ] `grep -c "Add from Clipboard" src/Mono/SurferMenu.cs` → ≥ 3
  - [ ] `grep -c "GUILayout.TextField" src/Mono/SurferMenu.cs` → 0

  **QA Scenarios**:
  ```
  Scenario: GUIStyle alignment defined
    Tool: Bash
    Steps:
      1. grep "TextAnchor.MiddleLeft" src/Mono/SurferMenu.cs
      2. Assert: 1 match
    Evidence: .sisyphus/evidence/task-surg1-align.txt

  Scenario: Equal tab widths
    Tool: Bash
    Steps:
      1. grep "440f / _tabs.Count" src/Mono/SurferMenu.cs
      2. Assert: 1 match
    Evidence: .sisyphus/evidence/task-surg1-tabs.txt

  Scenario: No TextField anywhere
    Tool: Bash
    Steps:
      1. grep -c "GUILayout.TextField" src/Mono/SurferMenu.cs
      2. Assert: 0
    Evidence: .sisyphus/evidence/task-surg1-notextfield.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Evidence: .sisyphus/evidence/task-surg1-build.txt
  ```

- [ ] 2. **Fix leave game crash** (in `src/Patches/Gameplay/Player/PlayerJoinAndLeftPatch.cs`)

  - In `BetterShowNotification` (around line 119), add at the very top of the method:
    ```csharp
    if (HudManager.Instance == null) return;
    ```
  - That's it. One line. No other changes.

  **Acceptance Criteria**:
  - [ ] `grep -n "HudManager.Instance == null" src/Patches/Gameplay/Player/PlayerJoinAndLeftPatch.cs` → 1 match
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0

  **QA**:
  ```
  Scenario: Null guard present
    Tool: Bash
    Steps:
      1. grep "HudManager.Instance == null" src/Patches/Gameplay/Player/PlayerJoinAndLeftPatch.cs
      2. Assert: 1 match
    Evidence: .sisyphus/evidence/task-surg2-guard.txt
  ```

- [ ] 3. **Build, deploy, verify**

  - `dotnet build src/Surfer.csproj --configuration Release`
  - `cp src/bin/Release/net6.0/Surfer.dll "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/"`
  
  **Commit**: YES — `fix: surgical fixes for alignment, tabs, precision, overlap, crash, keywords`

---

## Success Criteria

```bash
# Alignment fix (surgical - GUIStyle, not padding)
grep -c "TextAnchor.MiddleLeft" src/Mono/SurferMenu.cs && echo "✓" || echo "✗"
# Expected: 1

# Tab overflow (surgical - dynamic width, not rename)
grep -c "440f / _tabs.Count" src/Mono/SurferMenu.cs && echo "✓" || echo "✗"
# Expected: 1

# Overlap fix
grep -c "CloseAllSubWindows" src/Mono/SurferMenu.cs && echo "✓" || echo "✗"
# Expected: ≥ 4

# No TextField
grep -c "GUILayout.TextField" src/Mono/SurferMenu.cs && echo "✗ (should be 0)" || echo "✓"
# Expected: 0

# Crash fix
grep -c "HudManager.Instance == null" src/Patches/Gameplay/Player/PlayerJoinAndLeftPatch.cs && echo "✓" || echo "✗"
# Expected: 1

# Build
dotnet build src/Surfer.csproj --configuration Release && echo "✓" || echo "✗"
```
