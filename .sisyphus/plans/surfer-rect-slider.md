# Surfer: Rect-Based Slider Alignment + Slider Contrast

## TL;DR

> `GUILayout.HorizontalSlider` + `GUILayout.Label` with matching `Height(20)` don't align reliably in Unity IMGUI — different widget types render at different vertical offsets within their cells. Switch to `GUI.HorizontalSlider` + `GUI.Label` + `GUI.Button` with explicit `Rect` positioning. Add visible track background for contrast.

---

## Root Cause

`GUILayout.Height(20)` forces the layout CELL to 20px, but doesn't control where INSIDE that cell each widget renders:
- `GUILayout.Label` with `TextAnchor.MiddleLeft`: text renders at cell center (y ≈ 3 from top for 14px font in 20px cell)
- `GUILayout.HorizontalSlider`: track renders near cell top (y ≈ 2-4 from top)
- `GUILayout.Button`: label renders near cell center

These 1-2px differences accumulate across a row, creating visible misalignment. Especially noticeable on dark backgrounds.

**Fix**: Use `GUI.*` (non-layout) methods with explicit `Rect` values. Every element shares the same `row.y` baseline. Slider track is vertically centered within the row by adding a `+4` Y offset.

---

## TODOs

- [x] 1. **Rewrite DrawOptionSlider with rect-based positioning** (`src/Mono/SurferMenu.cs`)

  Replace the entire `DrawOptionSlider` method (lines 404-430) with:
  ```csharp
  private void DrawOptionSlider(string label, OptionIntItem? item)
  {
      if (item == null) return;
      
      int val = item.GetValue();
      
      // Single row with fixed pixel height — no GUILayout alignment guessing
      Rect row = GUILayoutUtility.GetRect(350, 24);
      
      // Label at top-left of row
      GUI.Label(new Rect(row.x, row.y, 140, 24), label + ":", SurferStyles.AlignedLabel);
      
      // Slider with visible gray track background for contrast
      Rect trackRect = new Rect(row.x + 145, row.y + 7, 140, 10);
      GUI.Box(trackRect, "");  // visible track
      int newVal = (int)GUI.HorizontalSlider(trackRect, val, 0, 10000);
      
      // Editable value button at right end
      bool editingThis = _editingNumber && _editMin == 0 && _editMax == 10000;
      string dsp = editingThis ? (_editBuffer + "_") : newVal.ToString();
      GUI.backgroundColor = editingThis ? new Color32(80, 80, 20, 255) : new Color32(40, 40, 60, 255);
      if (GUI.Button(new Rect(row.x + 289, row.y, 45, 24), dsp))
      {
          _editingNumber = true;
          _editBuffer = newVal.ToString();
          _editMin = 0; _editMax = 10000;
          _editCommit = (v) => item.SetValue(v);
      }
      GUI.backgroundColor = PurpleOn;
      
      if (newVal != val)
          item.SetValue(newVal);
  }
  ```

  **Must NOT do**:
  - Do NOT use `GUILayout.Label` / `GUILayout.HorizontalSlider` / `GUILayout.Button` inside this method
  - Do NOT add `GUILayout.BeginHorizontal()` / `GUILayout.EndHorizontal()`
  - Do NOT change other methods

- [x] 2. **Rewrite DrawHostTab threshold slider with rect-based positioning** (`src/Mono/SurferMenu.cs`)

  Replace the threshold slider block (lines 211-230):
  ```csharp
  // OLD: GUILayout.BeginHorizontal() ... GUILayout.EndHorizontal() block
  // NEW:
  Rect row = GUILayoutUtility.GetRect(350, 24);
  // Indent 20px
  Rect labelR = new Rect(row.x + 20, row.y, 70, 24);
  GUI.Label(labelR, "Threshold:", SurferStyles.AlignedLabel);
  
  int val = SurferPlugin.AutoKickThreshold?.Value ?? 0;
  Rect trackR = new Rect(labelR.xMax + 4, row.y + 7, 120, 10);
  GUI.Box(trackR, "");
  int newVal = (int)GUI.HorizontalSlider(trackR, val, 0, 100);
  
  Rect btnR = new Rect(trackR.xMax + 4, row.y, 35, 24);
  bool editingThis = _editingNumber && _editMin == 0 && _editMax == 100;
  string dsp = editingThis ? (_editBuffer + "_") : newVal.ToString();
  GUI.backgroundColor = editingThis ? new Color32(80, 80, 20, 255) : new Color32(40, 40, 60, 255);
  if (GUI.Button(btnR, dsp))
  {
      _editingNumber = true;
      _editBuffer = newVal.ToString();
      _editMin = 0; _editMax = 100;
      _editCommit = (v) => { if (SurferPlugin.AutoKickThreshold != null) SurferPlugin.AutoKickThreshold.Value = v; };
  }
  GUI.backgroundColor = PurpleOn;
  
  if (newVal != val && SurferPlugin.AutoKickThreshold != null)
      SurferPlugin.AutoKickThreshold.Value = newVal;
  ```

  **Must NOT do**:
  - Do NOT keep any `GUILayout` calls in the threshold block
  - Do NOT remove the `DrawToggle("Auto-Kick Low Level", ...)` call above it
  - Do NOT change the `if (SurferPlugin.AutoKick?.Value == true)` guard

- [x] 3. **Build, deploy, verify alignment**

  - `dotnet build src/Surfer.csproj --configuration Release`
  - Copy DLL
  - Verify: sliders and labels are pixel-aligned, slider track has visible gray background

  **QA**:
  ```bash
  grep -c "GUI.HorizontalSlider\|GUI.Label.*AlignedLabel\|GUI.Button" src/Mono/SurferMenu.cs
  # Expected: ≥ 8 (rect-based positioning used in both methods)
  
  grep -c "GUILayoutUtility.GetRect" src/Mono/SurferMenu.cs
  # Expected: ≥ 2 (one per slider method)
  ```

---

## Success Criteria

```bash
# Rect-based positioning in place
grep -c "GUILayoutUtility.GetRect" src/Mono/SurferMenu.cs
# Expected: ≥ 2

# Slider track visible
grep -c "GUI.Box(trackRect" src/Mono/SurferMenu.cs
# Expected: ≥ 2

# No GUILayout in slider methods
grep -A 30 "private void DrawOptionSlider" src/Mono/SurferMenu.cs | grep -c "GUILayout\."
# Expected: 0

dotnet build src/Surfer.csproj --configuration Release
# Expected: exit 0
```
