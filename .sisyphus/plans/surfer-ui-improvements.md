# Surfer UI Improvements: Sliders, Keywords, Ban Lists, Layout

## TL;DR

> **Quick Summary**: Fix slider alignment + add numeric input for large ranges. Add sub-menu windows for managing anti-bot keywords and ban lists. Fix About tab overflow by resizing the window. Convert hardcoded anti-bot keywords to runtime-editable storage.
>
> **Deliverables**:
> - `src/Mono/SurferMenu.cs` — aligned sliders, numeric text fields, resized window, 4 new sub-windows
> - `src/Data/BetterDataManager.cs` — keyword load/save APIs + ban name/word add/remove APIs
> - `src/Modules/AntiBotPatch.cs` — keywords loaded from file instead of hardcoded array
>
> **Estimated Effort**: Medium (5 tasks, 1 wave + final)

---

## Context

### User Request
1. Slider labels and sliders are misaligned vertically (slider sits slightly above text)
2. Need numeric text input for large-range sliders (0-10,000 — slider jumps ~83 levels per pixel)
3. Anti-bot keyword kick needs a sub-menu to add/remove keywords
4. "Use Ban Player List", "Use Ban Name List", "Use Ban Word List" need sub-menus to manage entries
5. About tab content overflows — need larger container

### Research Findings
- **Slider misalignment**: `DrawOptionSlider` uses `Label(145px)` + `HorizontalSlider(120px)` in a horizontal layout, but `GUILayout.Label` and `GUILayout.HorizontalSlider` have different intrinsic vertical alignments in Unity IMGUI.
- **Numeric input**: No text field exists — only `GUILayout.Label(newVal.ToString(), ...)` for display
- **Keywords**: Currently hardcoded in `AntiBotPatch.cs` as `string[] ProhibitedKeywords = ["tnt", "hyde", "discord.gg", "discord", "predators"]` — must edit source and rebuild to change
- **Ban lists**: Three `.txt` files in `Better_Data/SaveInfo/`. Only `BanPlayerList.txt` has a programmatic API (`AddToBanList()`). Name and word lists are file-edit-only.
- **About tab**: Window is 460×420, ScrollView is 460×380. Content is ~14 lines at 14-18pt with spacers — tight but scrollable.

---

## Work Objectives

### Core Objective
Polish the ImGUI menu: fix slider alignment, add numeric input for precision, add editable sub-menus for keywords and ban lists, resize window for content fit.

### Must Have
- Sliders vertically centered with their labels
- Text field for direct numeric input on large-range sliders (Min Level to Detect: 0-10,000, Min Level to Kick: 0-10,000)
- Keywords sub-window: add/remove keywords that persist between game sessions
- Ban Name List sub-window: add/remove name patterns
- Ban Word List sub-window: add/remove word patterns
- Ban Player List sub-window: view and remove entries
- About tab content fits without overflow
- AntiBotPatch.cs reads keywords from file, not hardcoded array

### Must NOT Have
- Do NOT change config entry system for existing toggles
- Do NOT break `TextFileHandler` parsing or wildcard matching
- Do NOT change the data file format (comments with `//`, one entry per line)
- Do NOT add new NuGet packages or dependencies

---

## Execution Strategy

All 5 tasks can be worked on in parallel (they touch different areas of SurferMenu.cs or different files), then final build/deploy.

---

## TODOs

- [x] 1. **Fix slider alignment + add numeric text input**

  **What to do**:
  - Edit `src/Mono/SurferMenu.cs`, replace the `DrawOptionSlider` method (lines 276-291):
  
  **Fix alignment**: Wrap the label in a small vertical group with `FlexibleSpace` to center it vertically against the slider:
  ```csharp
  private static void DrawOptionSlider(string label, OptionIntItem? item)
  {
      if (item == null) return;

      GUILayout.BeginHorizontal();
      
      // Vertically center the label against the slider
      GUILayout.BeginVertical();
      GUILayout.FlexibleSpace();
      GUILayout.Label(label + ":", GUILayout.Width(140));
      GUILayout.FlexibleSpace();
      GUILayout.EndVertical();

      int val = item.GetValue();
      // Get the min/max from the item — OptionIntItem stores range
      // Fall back to 0-10000 if unable to determine
      int min = 0, max = 10000;
      int newVal = (int)GUILayout.HorizontalSlider(val, min, max, GUILayout.Width(100));
      
      // Numeric text input for direct editing (6 chars max: "10000")
      string numStr = GUILayout.TextField(newVal.ToString(), 6, GUILayout.Width(45));
      int parsed;
      if (int.TryParse(numStr, out parsed) && parsed >= min && parsed <= max)
          newVal = parsed;

      GUILayout.EndHorizontal();

      if (newVal != val)
          item.SetValue(newVal);
  }
  ```
  
  **Read min/max from OptionIntItem**: Check `OptionIntItem` for min/max properties. Read `src/Modules/OptionItems/OptionIntItem.cs` to find the range bounds. If the class exposes `Min`/`Max` or stores them in a validator, use those. Fallback: hardcode appropriate ranges per slider instance (DetectedLevelAbove: 100-10000, KickLevelBelow: 0-10000, HideAndSeekImpNum: 1-5, AutoKickThreshold: 0-100).
  
  **Also fix the AutoKickThreshold slider** in `DrawHostTab()` (lines 122-133): convert the inline slider code to use the same vertical centering pattern. Replace:
  ```csharp
  GUILayout.BeginHorizontal();
  GUILayout.Space(20);
  GUILayout.Label("Threshold:", GUILayout.Width(70));
  int val = SurferPlugin.AutoKickThreshold?.Value ?? 0;
  int newVal = (int)GUILayout.HorizontalSlider(val, 0, 100, GUILayout.Width(120));
  GUILayout.Label(newVal.ToString(), GUILayout.Width(30));
  GUILayout.EndHorizontal();
  ```
  With vertically centered version:
  ```csharp
  GUILayout.BeginHorizontal();
  GUILayout.Space(20);
  GUILayout.BeginVertical();
  GUILayout.FlexibleSpace();
  GUILayout.Label("Threshold:", GUILayout.Width(70));
  GUILayout.FlexibleSpace();
  GUILayout.EndVertical();
  int val = SurferPlugin.AutoKickThreshold?.Value ?? 0;
  int newVal = (int)GUILayout.HorizontalSlider(val, 0, 100, GUILayout.Width(100));
  GUILayout.BeginVertical();
  GUILayout.FlexibleSpace();
  // Text field for direct input
  string numStr = GUILayout.TextField(newVal.ToString(), 4, GUILayout.Width(35));
  if (int.TryParse(numStr, out int parsed) && parsed >= 0 && parsed <= 100)
      newVal = parsed;
  GUILayout.FlexibleSpace();
  GUILayout.EndVertical();
  GUILayout.EndHorizontal();
  ```

  **Must NOT do**:
  - Do NOT change the toggle alignment (it's fine)
  - Do NOT remove the existing slider — add the text field alongside it

  **Recommended Agent Profile**: `quick`
  **Parallel**: YES (with T2, T3, T4)

  **References**:
  - `src/Mono/SurferMenu.cs` — `DrawOptionSlider` at 276-291, AutoKickThreshold at 120-133, `DrawToggle` at 208-227 (reference for pattern), `DrawOptionDropdown` at 252-274
  - `src/Modules/OptionItems/OptionIntItem.cs` — check for Min/Max/Step properties
  - `src/Patches/Gameplay/UI/Settings/GameSettingsPatch.cs` — `DetectedLevelAbove` created with `(100, 10000, 5)`, `KickLevelBelow` with `(0, 10000, 1)`, `HideAndSeekImpNum` with `(1, 5, 1)`

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -n "GUILayout.TextField" src/Mono/SurferMenu.cs` returns ≥ 3 matches (threshold + 2 detect/kick sliders)
  - [ ] `grep -n "FlexibleSpace" src/Mono/SurferMenu.cs` returns ≥ 4 matches (vertical centering groups added)
  - [ ] All sliders have a numeric text field alongside

  **QA Scenarios**:
  ```
  Scenario: Numeric text fields added to all sliders
    Tool: Bash (grep)
    Steps:
      1. grep -c "GUILayout.TextField" src/Mono/SurferMenu.cs
      2. Assert: count ≥ 3
    Evidence: .sisyphus/evidence/task-ui1-textfields.txt

  Scenario: Vertical centering implemented
    Tool: Bash (grep)
    Steps:
      1. grep -c "FlexibleSpace" src/Mono/SurferMenu.cs
      2. Assert: count ≥ 4
    Evidence: .sisyphus/evidence/task-ui1-flexspace.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Evidence: .sisyphus/evidence/task-ui1-build.txt
  ```

  **Commit**: NO (groups with all tasks)

---

- [x] 2. **Resize window for About tab overflow**

  **What to do**:
  - Edit `src/Mono/SurferMenu.cs`:
  - **Increase window size**: Line 32: `_windowRect = new Rect(100, 80, 460, 420);` → change to `new Rect(100, 60, 480, 500);`
    - Width: 460 → 480 (slightly wider to accommodate "Anti-Cheat" tab button comfortably)
    - Height: 420 → 500 (gives ~80px more vertical room)
    - Y: 80 → 60 (shift up slightly so it's still centered)
  - **Increase ScrollView height**: Line 62: `GUILayout.Height(380)` → `GUILayout.Height(455)`
    - 455 = 500 window - 20 title bar - 25 tab buttons/spacing = fills remaining space
  - **Update ScrollView width**: Line 62: `GUILayout.Width(460)` → `GUILayout.Width(480)` (match window width)
  - Also update the DragWindow rect on line 81: `new Rect(0, 0, 10000, 20)` → no change needed (it already spans full width)
  
  **Must NOT do**:
  - Do NOT change tab content or About tab text
  - Do NOT remove the ScrollView

  **Recommended Agent Profile**: `quick`
  **Parallel**: YES (with T1, T3, T4)

  **References**:
  - `src/Mono/SurferMenu.cs` — line 32 (window rect), line 62 (ScrollView size)

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep "new Rect(100, 60, 480, 500)" src/Mono/SurferMenu.cs` returns 1 match
  - [ ] `grep "GUILayout.Height(455)" src/Mono/SurferMenu.cs` returns 1 match
  - [ ] `grep "GUILayout.Width(480)" src/Mono/SurferMenu.cs` returns ≥ 1 match (ScrollView width)

  **QA Scenarios**:
  ```
  Scenario: Window resized to 480x500
    Tool: Bash (grep)
    Steps:
      1. grep "new Rect(100, 60, 480, 500)" src/Mono/SurferMenu.cs
      2. Assert: 1 match
    Evidence: .sisyphus/evidence/task-ui2-window.txt

  Scenario: ScrollView expanded to 480x455
    Tool: Bash (grep)
    Steps:
      1. grep "GUILayout.Height(455)" src/Mono/SurferMenu.cs
      2. Assert: 1 match
    Evidence: .sisyphus/evidence/task-ui2-scroll.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Evidence: .sisyphus/evidence/task-ui2-build.txt
  ```

  **Commit**: NO (groups with all tasks)

---

- [x] 3. **Add anti-bot keyword management system**

  **What to do**:
  
  **Part A — Add keyword storage to BetterDataManager** (`src/Data/BetterDataManager.cs`):
  
  - Add a new file path field:
    ```csharp
    internal static string antiBotKeywordsFile = Path.Combine(filePathFolder, "SaveInfo", "AntiBotKeywords.txt");
    ```
  - Add methods:
    ```csharp
    internal static List<string> LoadKeywords()
    {
        if (!File.Exists(antiBotKeywordsFile))
            return [];
        return TextFileHandler.ReadContents(antiBotKeywordsFile);
    }
    
    internal static void AddKeyword(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return;
        keyword = keyword.Trim().ToLower();
        // Check for duplicate
        var existing = LoadKeywords();
        if (existing.Contains(keyword, StringComparer.OrdinalIgnoreCase)) return;
        File.AppendAllText(antiBotKeywordsFile, keyword + Environment.NewLine);
    }
    
    internal static void RemoveKeyword(string keyword)
    {
        if (!File.Exists(antiBotKeywordsFile)) return;
        var lines = File.ReadAllLines(antiBotKeywordsFile)
            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.Trim().StartsWith("//") && !l.Trim().StartsWith("#"))
            .Where(l => !l.Trim().Equals(keyword.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();
        File.WriteAllLines(antiBotKeywordsFile, lines);
    }
    ```
  - In `Initialize()`, add file creation with header comment (like the other ban files):
    ```csharp
    if (!File.Exists(antiBotKeywordsFile))
        File.WriteAllText(antiBotKeywordsFile, "// Anti-Bot Keywords - one per line, case-insensitive" + Environment.NewLine +
            "// Players with names or messages containing these will be kicked" + Environment.NewLine);
    ```

  **Part B — Modify AntiBotPatch** (`src/Modules/AntiBotPatch.cs`):
  - Remove the hardcoded array (line 7-10):
    ```csharp
    // REMOVE THIS:
    private static readonly string[] ProhibitedKeywords = ["tnt", "hyde", "discord.gg", "discord", "predators"];
    ```
  - Replace with runtime-loaded list. Inside the prefix method, load keywords each check (or cache and refresh periodically):
    ```csharp
    var keywords = BetterDataManager.LoadKeywords();
    if (keywords.Count == 0) return true; // no keywords configured
    
    string playerNameLower = ...;
    string msgLower = ...;
    foreach (string keyword in keywords)
    {
        if (playerNameLower.Contains(keyword) || msgLower.Contains(keyword))
        {
            // kick logic (same as existing)
        }
    }
    ```
  - Add `using Surfer.Data;` for `BetterDataManager`

  **Part C — Add ImGUI sub-window** (`src/Mono/SurferMenu.cs`):
  
  Add fields to `SurferMenu` class:
  ```csharp
  private bool _showKeywordsWindow;
  private string _newKeyword = "";
  private Vector2 _keywordsScrollPos;
  ```
  
  In `OnGUI()`, after the main window, add the sub-window:
  ```csharp
  if (_showKeywordsWindow)
  {
      Rect kwRect = new Rect(_windowRect.x + _windowRect.width + 10, _windowRect.y, 300, 350);
      kwRect = GUI.Window(9970, kwRect, DrawKeywordsWindow, "Keywords");
  }
  ```
  
  New method:
  ```csharp
  private void DrawKeywordsWindow(int id)
  {
      GUILayout.Label("Anti-Bot Keywords", SurferStyles.SectionLabel);
      GUILayout.Label("Names/messages containing these trigger a kick.");
      GUILayout.Space(5);
      
      // Add new keyword
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
      
      GUILayout.Space(8);
      
      // List existing keywords
      var keywords = BetterDataManager.LoadKeywords();
      _keywordsScrollPos = GUILayout.BeginScrollView(_keywordsScrollPos, GUILayout.Height(220));
      foreach (string kw in keywords)
      {
          GUILayout.BeginHorizontal();
          GUILayout.Label("• " + kw);
          GUI.backgroundColor = new Color32(180, 40, 40, 255);
          if (GUILayout.Button("X", GUILayout.Width(30)))
              BetterDataManager.RemoveKeyword(kw);
          GUI.backgroundColor = PurpleOn;
          GUILayout.EndHorizontal();
      }
      GUILayout.EndScrollView();
      
      GUI.DragWindow(new Rect(0, 0, 300, 20));
  }
  ```
  
  In `DrawHostTab()`, add a button next to the AntiBot toggle (line 118):
  ```csharp
  GUILayout.BeginHorizontal();
  DrawToggle("Anti-Bot (Keyword Kick)", SurferPlugin.AntiBot);
  if (SurferPlugin.AntiBot?.Value == true)
  {
      if (GUILayout.Button("Keywords", GUILayout.Width(80), GUILayout.Height(20)))
          _showKeywordsWindow = !_showKeywordsWindow;
  }
  GUILayout.EndHorizontal();
  ```
  
  **Must NOT do**:
  - Do NOT change the `TextFileHandler` API
  - Do NOT change how keywords are matched (Contains, case-insensitive)
  - Do NOT remove the `SurferPlugin.AntiBot` config toggle

  **Recommended Agent Profile**: `unspecified-high`
  - Reason: Multi-file change (BetterDataManager, AntiBotPatch, SurferMenu) with new sub-window UI
  **Parallel**: YES (with T1, T2, T4)

  **References**:
  - `src/Data/BetterDataManager.cs` — existing ban file initialization pattern (lines 99-166), `AddToBanList()` (lines 236-263) as reference for file append
  - `src/Modules/AntiBotPatch.cs` — hardcoded array at lines 7-10, check logic at lines 13-38
  - `src/Modules/TextFileHandler.cs` — `ReadContents()` at lines 64-77 (handles comment stripping)
  - `src/Mono/SurferMenu.cs` — main window pattern, `DrawHostTab()` around line 114-134

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -n "LoadKeywords\|AddKeyword\|RemoveKeyword" src/Data/BetterDataManager.cs` returns ≥ 3 matches
  - [ ] `grep -n "ProhibitedKeywords" src/Modules/AntiBotPatch.cs` returns 0 matches (hardcoded array gone)
  - [ ] `grep -n "LoadKeywords" src/Modules/AntiBotPatch.cs` returns ≥ 1 match (reads from file)
  - [ ] `grep -n "_showKeywordsWindow\|DrawKeywordsWindow\|Keywords" src/Mono/SurferMenu.cs` returns ≥ 5 matches (ImGUI sub-window added)

  **QA Scenarios**:
  ```
  Scenario: Keywords loaded from file instead of hardcoded
    Tool: Bash (grep)
    Steps:
      1. grep -n "ProhibitedKeywords" src/Modules/AntiBotPatch.cs
      2. Assert: 0 matches (hardcoded array removed)
      3. grep -n "BetterDataManager.LoadKeywords\|BetterDataManager" src/Modules/AntiBotPatch.cs
      4. Assert: ≥ 1 match (now reads from BetterDataManager)
    Evidence: .sisyphus/evidence/task-ui3-dynamic.txt

  Scenario: Add/Remove APIs exist
    Tool: Bash (grep)
    Steps:
      1. grep -n "internal static.*AddKeyword\|internal static.*RemoveKeyword\|internal static.*LoadKeywords" src/Data/BetterDataManager.cs
      2. Assert: 3 matches
    Evidence: .sisyphus/evidence/task-ui3-apis.txt

  Scenario: ImGUI keywords sub-window added
    Tool: Bash (grep)
    Steps:
      1. grep -n "DrawKeywordsWindow\|_showKeywordsWindow" src/Mono/SurferMenu.cs
      2. Assert: ≥ 2 matches
    Evidence: .sisyphus/evidence/task-ui3-imgui.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Evidence: .sisyphus/evidence/task-ui3-build.txt
  ```

  **Commit**: NO (groups with all tasks)

---

- [x] 4. **Add ban list sub-menus**

  **What to do**:
  
  **Part A — Add APIs to BetterDataManager** (`src/Data/BetterDataManager.cs`):
  Add methods for BanNameList and BanWordList manipulation:
  
  ```csharp
  internal static List<string> LoadBanNames()
  {
      if (!File.Exists(banNameListFile)) return [];
      return TextFileHandler.ReadContents(banNameListFile);
  }
  
  internal static void AddBanName(string pattern)
  {
      if (string.IsNullOrWhiteSpace(pattern)) return;
      pattern = pattern.Trim();
      var existing = LoadBanNames();
      if (existing.Contains(pattern, StringComparer.OrdinalIgnoreCase)) return;
      File.AppendAllText(banNameListFile, pattern + Environment.NewLine);
  }
  
  internal static void RemoveBanName(string pattern)
  {
      if (!File.Exists(banNameListFile)) return;
      var lines = File.ReadAllLines(banNameListFile)
          .Where(l => !string.IsNullOrWhiteSpace(l) && !l.Trim().StartsWith("//") && !l.Trim().StartsWith("#"))
          .Where(l => !l.Trim().Equals(pattern.Trim(), StringComparison.OrdinalIgnoreCase))
          .ToList();
      File.WriteAllLines(banNameListFile, lines);
  }
  
  // Same pattern for BanWordList: LoadBanWords(), AddBanWord(), RemoveBanWord()
  // And for BanPlayerList: LoadBanPlayers(), RemoveBanPlayer() — AddToBanList already exists
  ```
  Also add `LoadBanPlayers()` and `RemoveBanPlayer()`:
  ```csharp
  internal static List<string> LoadBanPlayers()
  {
      if (!File.Exists(banPlayerListFile)) return [];
      return TextFileHandler.ReadContents(banPlayerListFile);
  }
  
  internal static void RemoveBanPlayer(string entry)
  {
      if (!File.Exists(banPlayerListFile)) return;
      var lines = File.ReadAllLines(banPlayerListFile)
          .Where(l => !string.IsNullOrWhiteSpace(l) && !l.Trim().StartsWith("//") && !l.Trim().StartsWith("#"))
          .Where(l => !l.Trim().Equals(entry.Trim(), StringComparison.OrdinalIgnoreCase))
          .ToList();
      File.WriteAllLines(banPlayerListFile, lines);
  }
  ```

  **Part B — Add ImGUI sub-windows** (`src/Mono/SurferMenu.cs`):
  
  Add fields:
  ```csharp
  private bool _showBanPlayerWindow, _showBanNameWindow, _showBanWordWindow;
  private string _newBanName = "", _newBanWord = "";
  private Vector2 _banPlayerScrollPos, _banNameScrollPos, _banWordScrollPos;
  ```
  
  Add buttons in `DrawAntiCheatTab()` next to the ban list toggles (lines 147-149):
  ```csharp
  // Around line 147-149, change from:
  DrawOptionToggle("Use Ban Player List", BetterGameSettings.UseBanPlayerList);
  DrawOptionToggle("Use Ban Name List", BetterGameSettings.UseBanNameList);
  DrawOptionToggle("Use Ban Word List", BetterGameSettings.UseBanWordList);
  // To:
  GUILayout.BeginHorizontal();
  DrawOptionToggle("Use Ban Player List", BetterGameSettings.UseBanPlayerList);
  if (BetterGameSettings.UseBanPlayerList?.GetValue() == true)
  {
      if (GUILayout.Button("List", GUILayout.Width(50), GUILayout.Height(20)))
          _showBanPlayerWindow = !_showBanPlayerWindow;
  }
  GUILayout.EndHorizontal();

  GUILayout.BeginHorizontal();
  DrawOptionToggle("Use Ban Name List", BetterGameSettings.UseBanNameList);
  if (BetterGameSettings.UseBanNameList?.GetValue() == true)
  {
      if (GUILayout.Button("List", GUILayout.Width(50), GUILayout.Height(20)))
          _showBanNameWindow = !_showBanNameWindow;
  }
  GUILayout.EndHorizontal();

  GUILayout.BeginHorizontal();
  DrawOptionToggle("Use Ban Word List", BetterGameSettings.UseBanWordList);
  if (BetterGameSettings.UseBanWordList?.GetValue() == true)
  {
      if (GUILayout.Button("List", GUILayout.Width(50), GUILayout.Height(20)))
          _showBanWordWindow = !_showBanWordWindow;
  }
  GUILayout.EndHorizontal();
  ```
  
  Add sub-windows in `OnGUI()` (after the main window block, alongside the keywords window):
  ```csharp
  if (_showBanPlayerWindow)
  {
      Rect r = new Rect(_windowRect.x + _windowRect.width + 10, _windowRect.y, 320, 350);
      r = GUI.Window(9971, r, DrawBanPlayerWindow, "Ban Player List");
  }
  if (_showBanNameWindow)
  {
      Rect r = new Rect(_windowRect.x + _windowRect.width + 10, _windowRect.y, 320, 350);
      r = GUI.Window(9972, r, DrawBanNameWindow, "Ban Name List");
  }
  if (_showBanWordWindow)
  {
      Rect r = new Rect(_windowRect.x + _windowRect.width + 10, _windowRect.y, 320, 350);
      r = GUI.Window(9973, r, DrawBanWordWindow, "Ban Word List");
  }
  ```
  
  Each window method follows the same pattern as `DrawKeywordsWindow`:
  - Scrollable list with X buttons
  - Text input + Add button
  - DragWindow title bar
  
  Example `DrawBanNameWindow`:
  ```csharp
  private void DrawBanNameWindow(int id)
  {
      GUILayout.Label("Ban Name List", SurferStyles.SectionLabel);
      GUILayout.Label("Use ** for wildcards (e.g., **hacker**)", SurferStyles.SmallLabel);
      GUILayout.Space(5);
      
      GUILayout.BeginHorizontal();
      _newBanName = GUILayout.TextField(_newBanName, GUILayout.Width(180));
      if (GUILayout.Button("Add", GUILayout.Width(60)) && !string.IsNullOrWhiteSpace(_newBanName))
      {
          BetterDataManager.AddBanName(_newBanName.Trim());
          _newBanName = "";
      }
      GUILayout.EndHorizontal();
      
      GUILayout.Space(8);
      var names = BetterDataManager.LoadBanNames();
      _banNameScrollPos = GUILayout.BeginScrollView(_banNameScrollPos, GUILayout.Height(220));
      foreach (string n in names)
      {
          GUILayout.BeginHorizontal();
          GUILayout.Label("• " + n);
          GUI.backgroundColor = new Color32(180, 40, 40, 255);
          if (GUILayout.Button("X", GUILayout.Width(30)))
              BetterDataManager.RemoveBanName(n);
          GUI.backgroundColor = PurpleOn;
          GUILayout.EndHorizontal();
      }
      GUILayout.EndScrollView();
      GUI.DragWindow(new Rect(0, 0, 320, 20));
  }
  ```
  Similar methods for `DrawBanPlayerWindow` and `DrawBanWordWindow` (same pattern, different data source).

  **Must NOT do**:
  - Do NOT change file format (comment handling, one entry per line)
  - Do NOT break existing ban check logic in `PlayerJoinAndLeftPatch.cs` or `SendChatHandler.cs`
  - Do NOT modify `TextFileHandler`
  - Do NOT add a sub-window for "Use Ban Word List Only In Lobby" — it's a simple on/off toggle

  **Recommended Agent Profile**: `unspecified-high`
  - Reason: Multi-file (BetterDataManager + SurferMenu), 9 new methods, 3 new sub-windows
  **Parallel**: YES (with T1, T2, T3)

  **References**:
  - `src/Data/BetterDataManager.cs` — existing ban file paths (banPlayerListFile, banNameListFile, banWordListFile), `AddToBanList()` as API pattern
  - `src/Modules/TextFileHandler.cs` — `ReadContents()` line 64-77
  - `src/Mono/SurferMenu.cs` — `DrawAntiCheatTab()` lines 138-182, existing sub-window pattern from T3
  - `src/Patches/Gameplay/Player/PlayerJoinAndLeftPatch.cs` — uses ban lists at lines 44-66

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -n "LoadBanNames\|AddBanName\|RemoveBanName\|LoadBanWords\|AddBanWord\|RemoveBanWord\|LoadBanPlayers\|RemoveBanPlayer" src/Data/BetterDataManager.cs | wc -l` returns ≥ 8
  - [ ] `grep -n "_showBanPlayerWindow\|_showBanNameWindow\|_showBanWordWindow" src/Mono/SurferMenu.cs` returns ≥ 3 matches
  - [ ] `grep -n "DrawBanPlayerWindow\|DrawBanNameWindow\|DrawBanWordWindow" src/Mono/SurferMenu.cs` returns ≥ 3 matches
  - [ ] `grep -n "GUI.Window(9971\|GUI.Window(9972\|GUI.Window(9973" src/Mono/SurferMenu.cs` returns ≥ 3 matches (sub-windows registered)

  **QA Scenarios**:
  ```
  Scenario: Ban list APIs added
    Tool: Bash (grep)
    Steps:
      1. grep -c "internal static.*LoadBan\|internal static.*AddBan\|internal static.*RemoveBan" src/Data/BetterDataManager.cs
      2. Assert: count ≥ 6
    Evidence: .sisyphus/evidence/task-ui4-apis.txt

  Scenario: Three ban sub-windows in ImGUI
    Tool: Bash (grep)
    Steps:
      1. grep -c "DrawBan.*Window" src/Mono/SurferMenu.cs
      2. Assert: count ≥ 3
    Evidence: .sisyphus/evidence/task-ui4-windows.txt

  Scenario: Sub-window IDs are unique
    Tool: Bash (grep)
    Steps:
      1. grep "GUI.Window(997" src/Mono/SurferMenu.cs
      2. Assert: IDs 9970, 9971, 9972, 9973 all present (keywords + 3 ban lists)
    Evidence: .sisyphus/evidence/task-ui4-ids.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Evidence: .sisyphus/evidence/task-ui4-build.txt
  ```

  **Commit**: NO (groups with all tasks)

---

- [x] 5. **Build, copy to game, final verify**

  **What to do**:
  - Build: `dotnet build src/Surfer.csproj --configuration Release`
  - Copy: `cp src/bin/Release/net6.0/Surfer.dll "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/"`
  - Verify all changes are present with grep audits
  - Check that the keyword file is created on first run (via Initialize)

  **Recommended Agent Profile**: `quick`
  **Parallel**: NO (final, after T1-T4)

  **Acceptance Criteria**:
  - [ ] Build exits 0
  - [ ] DLL copied and exists

  **QA Scenarios**:
  ```
  Scenario: Full build + deploy
    Tool: Bash
    Steps:
      1. dotnet build src/Surfer.csproj --configuration Release
      2. cp src/bin/Release/net6.0/Surfer.dll "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/"
      3. ls -la "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/Surfer.dll"
      4. Assert: all exit 0, file exists
    Evidence: .sisyphus/evidence/task-ui5-deploy.txt

  Scenario: Composite feature audit
    Tool: Bash
    Steps:
      1. grep -c "GUILayout.TextField" src/Mono/SurferMenu.cs → ≥3
      2. grep -c "FlexibleSpace" src/Mono/SurferMenu.cs → ≥4
      3. grep -c "DrawKeywordsWindow\|DrawBan.*Window" src/Mono/SurferMenu.cs → ≥4
      4. grep -c "LoadKeywords\|LoadBanNames\|LoadBanWords\|LoadBanPlayers" src/Data/BetterDataManager.cs → ≥4
      5. grep "ProhibitedKeywords" src/Modules/AntiBotPatch.cs → 0
      6. Assert: all checks pass
    Evidence: .sisyphus/evidence/task-ui5-audit.txt
  ```

  **Commit**: YES (final)
  - Message: `feat: slider alignment, numeric input, keyword/ban sub-menus, window resize`

---

## Success Criteria

```bash
# All features present
echo "=== Numeric inputs ===" && grep -c "GUILayout.TextField" src/Mono/SurferMenu.cs
echo "=== Sub-windows ===" && grep -c "DrawKeywordsWindow\|DrawBan.*Window" src/Mono/SurferMenu.cs
echo "=== APIs ===" && grep -c "LoadKeywords\|LoadBanNames\|LoadBanWords\|LoadBanPlayers" src/Data/BetterDataManager.cs
echo "=== No hardcoded keywords ===" && grep -c "ProhibitedKeywords" src/Modules/AntiBotPatch.cs
echo "=== Window resized ===" && grep -c "480, 500" src/Mono/SurferMenu.cs
# Expected: numeric ≥3, sub-windows ≥4, APIs ≥4, hardcoded=0, resize=1
```
