# Surfer Visual Cleanup & Scroll Fix

## TL;DR

> **Quick Summary**: Strip ALL Better Among Us visual branding (green theme, logos, branded text) from ~17 files while keeping functional overlays. Fix scroll/zoom conflict where the ImGUI menu passes scroll events to the game camera. Migrate 17 anti-cheat/gameplay settings from the removed "Better Settings" gear tab into the ImGUI SurferMenu.
>
> **Deliverables**:
> - `src/Patches/Gameplay/ZoomPatch.cs` — checks menu visibility, no longer zooms when menu is open
> - `src/Mono/SurferMenu.cs` — ScrollView added, anti-cheat settings tab added with full controls
> - `src/Patches/Client/Managers/MainMenuManagerPatch.cs` — all visual code stripped, ButtonPrefab preserved
> - `src/Patches/Gameplay/UI/Settings/GameSettingsPatch.cs` — green tab removed, OptionItem data preserved
> - 14+ other files — green colors and branding removed, functional code intact
> - `bin/Release/net6.0/Surfer.dll` — clean build without stale BetterAmongUs artifacts
>
> **Estimated Effort**: Large (17 tasks, 4 waves)
> **Parallel Execution**: YES — Waves 1 & 2 fully parallel (6+6 tasks), Waves 3-4 mostly sequential
> **Critical Path**: T1 (IsVisible) → T2 (ZoomPatch) → Wave 3 (GameSettings) → T16 (build)

---

## Context

### Original Request
1. Scrolling in the ImGUI menu (Delete key toggle) causes the game to zoom out in a loop. The menu doesn't scroll.
2. Remove ALL Better Among Us visual modifications — green main menu, "Better Settings" tab in gear icon, logo replacements, everything. Keep the ImGUI SurferMenu and its functional toggles.
3. Migrate the anti-cheat settings from the removed "Better Settings" tab INTO the Surfer ImGUI menu.
4. Output .dll to `/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins`

### Interview Summary
**Key Discussions**:
- Scroll fix: Add ScrollView to menu AND prevent zoom when menu is visible
- Visual cleanup: Deep clean — remove ALL green branding and mod-specific visuals
- Anti-cheat migration: Move all settings (WhenCheating, KickLevelBelow, ban lists, etc.) into the Surfer ImGUI — not discard them
- Test approach: Iterative build verification (build, check, fix, repeat)

**Research Findings**:
- **Zoom conflict root cause**: `ZoomPatch.cs` reads `Input.mouseScrollDelta` in `HudManager.Update` Postfix. `BlockZoomPatch.cs` tries to Prefix-reset zoom to 3f, but runs BEFORE ZoomPatch in the same frame — creating a per-frame tug-of-war.
- **Green theme engine**: `ObjectHelper.SetUIColors()` / `AddColor()` defaults to `Color.green`, called from 5+ patches. Has a `Disable_Theme` kill switch but nothing sets it.
- **Anti-cheat data web**: `BetterGameSettings.*` static OptionItems are the single source of truth for anti-cheat config, read by anti-cheat handlers throughout the codebase. Must preserve these as data objects while removing their visual tab.
- **Stale artifacts**: `BetterAmongUs.dll`, `.pdb`, `.deps.json` in `bin/Release/net6.0/` from project rename.

### Metis Review
**Identified Gaps** (addressed):
- **Functional overlay ambiguity**: BetterPingTracker, PlayerInfoDisplay, BetterNotificationManager — resolved: keep functional overlays, strip green branding from them. All green hex values and "Surfer"/"BetterUser" labels become neutral.
- **GameSettings data strategy**: Resolved: keep `BetterGameSettings.*` OptionItem static fields as pure data objects (skip visual `OptionTab` creation), wrap them as ImGUI controls in SurferMenu.
- **Scroll fix architecture**: Resolved: modify `ZoomPatch.cs` to check `SurferMenu.Visible` before processing scroll + add `GUI.BeginScrollView` to SurferMenu + delete `BlockZoomPatch.cs`.
- **Build verification**: Each wave ends with `dotnet build --configuration Release` verification. No proceeding past a broken build.
- **Mod compatibility**: Preserve all `SurferModdedSupportFlags` flag checks — change default behavior, not the compatibility gates.

---

## Work Objectives

### Core Objective
Strip ALL Better Among Us visual branding (green theme, logos, branded text) while keeping all functional features intact; fix scroll/zoom conflict; migrate anti-cheat settings into the ImGUI menu.

### Concrete Deliverables
- `src/Patches/Gameplay/ZoomPatch.cs` — menu-aware; skips zoom when SurferMenu is open
- `src/Mono/SurferMenu.cs` — ScrollView + new "Anti-Cheat" tab with 17+ setting controls
- `src/Patches/Client/Managers/MainMenuManagerPatch.cs` — visual code removed
- `src/Patches/Gameplay/UI/Settings/GameSettingsPatch.cs` — OptionTab removed, OptionItem data preserved
- 14+ additional files — green/branding stripped, functionality preserved
- `bin/Release/net6.0/Surfer.dll` — clean build, no stale BetterAmongUs artifacts
- `build/mod_folder_path` — verified pointing to correct Among Us plugins directory

### Definition of Done
- [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0 after every wave
- [ ] `grep -rn "#0dff00\|SetUIColors\|AddColor" src/Patches/ src/Mono/ src/Commands/ src/Managers/ --include="*.cs" | grep -v "VentGroups\|Resources\|.csproj\|ObjectHelper\|OptionsConsole\|RoleTypes"` returns 0 matches
- [ ] `grep -c "BetterSettingsTab" src/Patches/Gameplay/UI/Settings/GameSettingsPatch.cs` returns 0
- [ ] `grep -c "BetterGameSettings\." src/ --include="*.cs" -r` returns ≥ 17 (all anti-cheat reads preserved)
- [ ] `ls bin/Release/net6.0/BetterAmongUs.dll` returns "No such file or directory"
- [ ] `bin/Release/net6.0/Surfer.dll` exists and is newer than 30 minutes old

### Must Have
- ImGUI menu scrolling works, game does NOT zoom when menu is open
- Game zooms normally when menu is closed
- Main menu has original Among Us colors (no green tint)
- Gear icon settings has NO "Better Settings" tab
- All 17+ anti-cheat settings are configurable in the Surfer ImGUI
- No green `#0dff00` or `Color.green` branding remains in any UI-facing code
- Build produces ONLY `Surfer.dll` (no `BetterAmongUs.dll` stale artifacts)

### Must NOT Have (Guardrails)
- **DO NOT** delete or rename `Better*` classes (BetterDataManager, BetterNotificationManager, BetterPingTracker, BetterAntiCheat, BetterGameSettings)
- **DO NOT** remove `SurferModdedSupportFlags` flag checks — change default behavior only
- **DO NOT** touch `src/Modules/VentGroups.cs` (uses `Color.green` for game-functional vent highlighting)
- **DO NOT** break any anti-cheat functionality — `BetterGameSettings.*` OptionItem reads must compile and work
- **DO NOT** change the BepInEx ConfigEntry system in `SurferPlugin.cs`
- **DO NOT** remove functional overlays (ping tracker, player info, notifications) — only strip green/branding from them
- **DO NOT** modify `Resources/Lang/` translation files
- **DO NOT** touch `.csproj` references, NuGet packages, or TargetFramework

---

## Verification Strategy

### Test Decision
- **Infrastructure exists**: NO (IL2CPP BepInEx mod — no test framework)
- **Automated tests**: None (not feasible without game runtime)
- **Framework**: None
- **Verification method**: Build-based iterative verification with exact grep checks after each wave

### QA Policy
Every task includes Agent-Executed QA Scenarios using build commands and grep-based assertions. Evidence saved to `.sisyphus/evidence/task-{N}-{slug}.txt`.

- **Build verification**: `dotnet build src/Surfer.csproj --configuration Release` — must exit 0
- **Grep verification**: Exact hex values and method names checked against expected counts
- **File existence checks**: Verify output DLLs and stale artifact removal

---

## Execution Strategy

### Parallel Execution Waves

```
Wave 1 (Start Immediately — foundation + scroll fix, MAX PARALLEL):
├── T1: SurferMenu foundation (IsVisible + ScrollView + scroll event consumption)
├── T2: ZoomPatch menu-aware guard clause
├── T3: Delete BlockZoomPatch.cs
├── T4: Strip MainMenuManagerPatch visuals
├── T5: Clean VersionShowerPatch text
└── T6: Clean ModManagerPatch stamp + SplashIntroPatch logo

Wave 2 (After Wave 1 — in-game UI cleanup, MAX PARALLEL):
├── T7: Clean ClientPatch (green buttons + warning text)
├── T8: Clean FindAGameManagerPatch (green hover)
├── T9: Clean LobbyPatch (green buttons)
├── T10: Clean BetterPingTracker (de-brand, keep functional)
├── T11: Clean BetterNotificationManager + PlayerInfoDisplay (de-brand)
└── T12: Clean HelpCommand + AnnouncementPanelPatch + ChatPatch branding

Wave 3 (After Wave 2 — GameSettings migration, sequential within):
├── T13: Restructure GameSettingsPatch (remove tab, preserve OptionItems)
├── T14: Add anti-cheat ImGUI controls to SurferMenu (17+ settings)

Wave 4 (After Wave 3 — final cleanup + build):
├── T15: Clean stale BetterAmongUs build artifacts
└── T16: Build, copy to game, final grep verification

Critical Path: T1 → T2 → Wave 2 → T13 → T14 → T16
Parallel Speedup: ~60% faster than sequential (12 independent tasks in Waves 1-2)
Max Concurrent: 6 (Waves 1 & 2)
```

### Dependency Matrix

| Task | Blocks | Blocked By |
|------|--------|------------|
| T1 | T2, T3, T4, T5, T6 | None |
| T2 | — | T1 |
| T3 | — | T1 (conceptual) |
| T4 | — | T1 (conceptual) |
| T5 | — | None |
| T6 | — | None |
| T7 | — | None |
| T8 | — | None |
| T9 | — | None |
| T10 | — | None |
| T11 | — | None |
| T12 | — | None |
| T13 | T14 | None (but runs after Wave 2) |
| T14 | — | T13 |
| T15 | — | T14 |
| T16 | — | T15 |

---

## TODOs

- [x] 1. **SurferMenu foundation: static IsVisible + ScrollView + scroll event consumption**

  **What to do**:
  - Add `public static bool IsVisible { get; private set; }` to `SurferMenu` class — set it to `_visible` in `Update()` (each frame) and also in the property setter
  - Wrap the window content (everything inside `DrawWindow` after the title bar) in `GUILayout.BeginScrollView` / `GUILayout.EndScrollView`:
    ```csharp
    Vector2 _scrollPos = Vector2.zero; // new field
    _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Width(460), GUILayout.Height(380));
    // ... existing content ...
    GUILayout.EndScrollView();
    ```
  - Add `Event.current.Use()` inside `OnGUI()` when a ScrollWheel event occurs AND the mouse is over the window rect:
    ```csharp
    if (Event.current.type == EventType.ScrollWheel && _windowRect.Contains(new Vector2(Event.current.mousePosition.x, Screen.height - Event.current.mousePosition.y)))
        Event.current.Use();
    ```
    Note: IMGUI mouse position is bottom-left origin; Unity Input.mousePosition is bottom-left too, so `Event.current.mousePosition` should work directly with `_windowRect.Contains()`.
  - Keep existing tabs, toggles, layout unchanged. Only add the scroll wrapper.

  **Must NOT do**:
  - Do NOT change the Delet e key toggle or window ID (9969)
  - Do NOT change tab layout, colors, or existing toggle logic
  - Do NOT remove the DragWindow rect

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Single file, well-scoped UI change following existing patterns
  - **Skills**: [none]

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with T2, T3, T4, T5, T6)
  - **Blocks**: T2, T3, T4, T5, T6
  - **Blocked By**: None

  **References**:
  - `src/Mono/SurferMenu.cs` — full file: existing `Visible` property (line 10), `_visible` field (line 9), `Update()` toggle (line 31), `OnGUI()` render (lines 35-46), `DrawWindow()` content (lines 48-68), `DrawGeneralTab()` (lines 72-96), `DrawHostTab()` (lines 100-120), `DrawAboutTab()` (lines 124-142), `DrawToggle()` helper (lines 146-165), `SurferStyles` (lines 168-182)
  - `src/Patches/Gameplay/ZoomPatch.cs` — will reference `SurferMenu.IsVisible` in T2

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] File contains `public static bool IsVisible` 
  - [ ] File contains `GUILayout.BeginScrollView`
  - [ ] File contains `EventType.ScrollWheel` and `Event.current.Use()`

  **QA Scenarios**:

  ```
  Scenario: ScrollView present and compiles
    Tool: Bash (dotnet build)
    Preconditions: Working directory /home/zabwie/Desktop/surferMod
    Steps:
      1. Run: dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0, "Build succeeded"
    Expected Result: Zero build errors
    Failure Indicators: Any CS error referencing SurferMenu.cs
    Evidence: .sisyphus/evidence/task-1-build.txt

  Scenario: Static IsVisible property exists
    Tool: Bash (grep)
    Steps:
      1. Run: grep -n "public static bool IsVisible" src/Mono/SurferMenu.cs
      2. Assert: at least 1 match
    Expected Result: Property declaration found
    Evidence: .sisyphus/evidence/task-1-isvisible.txt

  Scenario: ScrollView in DrawWindow
    Tool: Bash (grep)
    Steps:
      1. Run: grep -n "BeginScrollView\|EndScrollView" src/Mono/SurferMenu.cs
      2. Assert: both BeginScrollView and EndScrollView found
    Expected Result: Scroll wrapper present
    Evidence: .sisyphus/evidence/task-1-scrollview.txt

  Scenario: Scroll wheel event consumed
    Tool: Bash (grep)
    Steps:
      1. Run: grep -n "EventType.ScrollWheel" src/Mono/SurferMenu.cs
      2. Assert: at least 1 match
    Expected Result: Scroll wheel handling present
    Evidence: .sisyphus/evidence/task-1-scrollwheel.txt
  ```

  **Evidence to Capture**:
  - [ ] `task-1-build.txt` — build output
  - [ ] `task-1-isvisible.txt` — IsVisible grep result
  - [ ] `task-1-scrollview.txt` — ScrollView grep result
  - [ ] `task-1-scrollwheel.txt` — ScrollWheel grep result

  **Commit**: NO (groups with Wave 1)

- [x] 2. **ZoomPatch: add menu visibility guard clause**

  **What to do**:
  - In `src/Patches/Gameplay/ZoomPatch.cs`, at the top of `HudManager_Update_Postfix()` (line 17, before the `bool canZoom` check), add:
    ```csharp
    if (SurferMenu.IsVisible) return;
    ```
  - This prevents ALL scroll-based zoom processing when the Surfer menu is open, including both zoom-in and zoom-out, and prevents the zoom reset logic from conflicting.
  - No other changes to the file.

  **Must NOT do**:
  - Do NOT change zoom ranges (3.0f / 18.0f)
  - Do NOT change `SetZoomSize` logic or camera manipulation
  - Do NOT change the `ResolutionManager.ResolutionChanged.Invoke` call

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Single line addition to one file
  - **Skills**: [none]

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with T1, T3, T4, T5, T6)
  - **Blocks**: None
  - **Blocked By**: T1 (conceptual — needs to know `SurferMenu.IsVisible` API)

  **References**:
  - `src/Patches/Gameplay/ZoomPatch.cs` — full file (79 lines): `HudManager_Update_Postfix` at lines 17-47
  - `src/Mono/SurferMenu.cs` — `public static bool IsVisible` property (added in T1)

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -n "SurferMenu.IsVisible" src/Patches/Gameplay/ZoomPatch.cs` returns exactly 1 match
  - [ ] The guard clause appears as the FIRST line inside `HudManager_Update_Postfix()`, before `bool canZoom`

  **QA Scenarios**:

  ```
  Scenario: Guard clause compiles
    Tool: Bash (dotnet build)
    Steps:
      1. Run: dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Expected Result: Build succeeds
    Evidence: .sisyphus/evidence/task-2-build.txt

  Scenario: Guard clause positioned correctly
    Tool: Bash (grep)
    Steps:
      1. Run: grep -A 2 "HudManager_Update_Postfix" src/Patches/Gameplay/ZoomPatch.cs
      2. Assert: next line contains "SurferMenu.IsVisible" followed by "return"
    Expected Result: Guard clause is first statement in method
    Evidence: .sisyphus/evidence/task-2-guard.txt
  ```

  **Evidence to Capture**:
  - [ ] `task-2-build.txt`
  - [ ] `task-2-guard.txt`

  **Commit**: NO (groups with Wave 1)

- [x] 3. **Delete BlockZoomPatch.cs** (obsolete)

  **What to do**:
  - Delete the file `src/Modules/BlockZoomPatch.cs` entirely.
  - BlockZoomPatch attempted to fix the same issue but was ineffective (runs as Prefix before ZoomPatch's Postfix). With T1+T2 fixes, it is no longer needed and would interfere.

  **Must NOT do**:
  - Do NOT delete any other files
  - Do NOT leave a partial/empty file

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Single file deletion
  - **Skills**: [none]

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with T1, T2, T4, T5, T6)
  - **Blocks**: None
  - **Blocked By**: T1 (conceptual)

  **References**:
  - `src/Modules/BlockZoomPatch.cs` — file to delete (17 lines)

  **Acceptance Criteria**:
  - [ ] File `src/Modules/BlockZoomPatch.cs` no longer exists
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0 (no compile errors from missing reference)
  - [ ] `grep -rn "BlockZoomPatch" src/` returns 0 matches

  **QA Scenarios**:

  ```
  Scenario: File deleted and build succeeds
    Tool: Bash
    Steps:
      1. Run: rm src/Modules/BlockZoomPatch.cs
      2. Run: dotnet build src/Surfer.csproj --configuration Release
      3. Assert: exit code 0
    Expected Result: Build succeeds without BlockZoomPatch
    Evidence: .sisyphus/evidence/task-3-build.txt

  Scenario: No remaining references
    Tool: Bash (grep)
    Steps:
      1. Run: grep -rn "BlockZoomPatch" src/ --include="*.cs"
      2. Assert: 0 matches (empty output)
    Expected Result: No dangling references
    Evidence: .sisyphus/evidence/task-3-grep.txt
  ```

  **Evidence to Capture**:
  - [ ] `task-3-build.txt`
  - [ ] `task-3-grep.txt`

  **Commit**: NO (groups with Wave 1)

- [x] 4. **Strip MainMenuManagerPatch visual modifications**

  **What to do**:
  - Edit `src/Patches/Client/Managers/MainMenuManagerPatch.cs`:
    - **REMOVE** the entire `MainMenuManager_LateUpdate_Postfix` method (lines 16-32) — this recolors all 13 main menu buttons green via `SetUIColors()`
    - **REMOVE** the logo replacement block (lines 38-54, the entire `if (!SurferModdedSupportFlags.HasFlag(SurferModdedSupportFlags.Disable_SurferLogo))` block and its contents including logo repositioning, scaling, and sprite replacement)
    - **REMOVE** the background texture recoloring (line 57: `__instance.transform.Find("MainUI/AspectScaler/BackgroundTexture")?.gameObject?.SetSpriteColors(...)`)
    - **KEEP** the ButtonPrefab creation (lines 60-66) — it's used by other features
    - **KEEP** `UpdateManager.Instance?.OnMainMenu()` (line 69)
    - **KEEP** the `PassiveButton? ButtonPrefab` field (line 12)
  - The `Start` postfix should end up looking like:
    ```csharp
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    [HarmonyPostfix]
    private static void MainMenuManager_Start_Postfix(MainMenuManager __instance)
    {
        if (ButtonPrefab == null)
        {
            ButtonPrefab = UnityEngine.Object.Instantiate(__instance.inventoryButton);
            ButtonPrefab.gameObject.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(ButtonPrefab);
        }
        UpdateManager.Instance?.OnMainMenu();
    }
    ```
  - After removing the `LateUpdate` postfix, remove the `List<PassiveButton>` import if no other code uses `System.Collections.Generic` in this file. Check remaining usings.
  - Remove the `Surfer.Helpers` using if no remaining code uses it.

  **Must NOT do**:
  - Do NOT remove ButtonPrefab creation or the field
  - Do NOT remove UpdateManager call
  - Do NOT remove the Harmony attributes or class structure

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Defined removals in one file
  - **Skills**: [none]

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with T1, T2, T3, T5, T6)
  - **Blocks**: None
  - **Blocked By**: T1 (conceptual)

  **References**:
  - `src/Patches/Client/Managers/MainMenuManagerPatch.cs` — full file (71 lines): LateUpdate postfix at 16-32, Start postfix at 34-70
  - `src/Helpers/ObjectHelper.cs` — `SetUIColors` at lines 88-124, `AddColor` at lines 131-137 (these are the methods being uncalled)

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -n "SetUIColors" src/Patches/Client/Managers/MainMenuManagerPatch.cs` returns 0 matches
  - [ ] `grep -n "SetSpriteColors" src/Patches/Client/Managers/MainMenuManagerPatch.cs` returns 0 matches
  - [ ] `grep -n "Utils.LoadSprite.*Surfer-Logo" src/Patches/Client/Managers/MainMenuManagerPatch.cs` returns 0 matches
  - [ ] `grep -n "ButtonPrefab" src/Patches/Client/Managers/MainMenuManagerPatch.cs` returns ≥ 2 matches (field + usage)
  - [ ] `grep -n "UpdateManager" src/Patches/Client/Managers/MainMenuManagerPatch.cs` returns ≥ 1 match (still present)
  - [ ] `grep -c "MainMenuManager_LateUpdate_Postfix" src/Patches/Client/Managers/MainMenuManagerPatch.cs` returns 0 (method removed)

  **QA Scenarios**:

  ```
  Scenario: Build succeeds after stripping visuals
    Tool: Bash (dotnet build)
    Steps:
      1. Run: dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0, "Build succeeded"
    Expected Result: No compile errors from removed references
    Evidence: .sisyphus/evidence/task-4-build.txt

  Scenario: No green theming calls remain
    Tool: Bash (grep)
    Steps:
      1. Run: grep -n "SetUIColors\|SetSpriteColors\|AddColor\|Surfer-Logo" src/Patches/Client/Managers/MainMenuManagerPatch.cs
      2. Assert: 0 matches (empty output)
    Expected Result: All theming removed
    Evidence: .sisyphus/evidence/task-4-theme.txt

  Scenario: Functional code preserved
    Tool: Bash (grep)
    Steps:
      1. Run: grep -n "ButtonPrefab\|UpdateManager" src/Patches/Client/Managers/MainMenuManagerPatch.cs
      2. Assert: ButtonPrefab (2+ matches), UpdateManager (1+ match)
    Expected Result: ButtonPrefab and UpdateManager still present
    Evidence: .sisyphus/evidence/task-4-functional.txt
  ```

  **Evidence to Capture**:
  - [ ] `task-4-build.txt`
  - [ ] `task-4-theme.txt`
  - [ ] `task-4-functional.txt`

  **Commit**: NO (groups with Wave 1)

- [x] 5. **Clean VersionShowerPatch: remove green branding from version text**

  **What to do**:
  - Edit `src/Patches/Client/VersionShowerPatch.cs`:
  - Change line 16 from:
    ```csharp
    __instance.text.text = $"<color=#0dff00>{mark}{bau}{mark} {SurferPlugin.GetVersionText()}</color> <color=#ababab>~</color> {Utils.GetPlatformName(SurferPlugin.PlatformData.Platform)} v{SurferPlugin.AmongUsVersion} ({SurferPlugin.AppVersion})";
    ```
    To neutral text without green color or brand marks:
    ```csharp
    __instance.text.text = $"Surfer {SurferPlugin.GetVersionText()} | {Utils.GetPlatformName(SurferPlugin.PlatformData.Platform)} v{SurferPlugin.AmongUsVersion}";
    ```
  - Remove the `Translator.GetString` calls for `mark` and `bau` since they're no longer needed.
  - Remove unused `using Surfer.Modules;` if no other code uses it.

  **Must NOT do**:
  - Do NOT remove the version display entirely
  - Do NOT change functionality — just color and branding

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Single line change in one file
  - **Skills**: [none]

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with T1, T2, T3, T4, T6)
  - **Blocks**: None
  - **Blocked By**: None

  **References**:
  - `src/Patches/Client/VersionShowerPatch.cs` — full file (18 lines): line 16 is the version text

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -n "#0dff00" src/Patches/Client/VersionShowerPatch.cs` returns 0 matches
  - [ ] `grep -n "mark}{bau}{mark}" src/Patches/Client/VersionShowerPatch.cs` returns 0 matches
  - [ ] `grep -n "SurferPlugin.GetVersionText" src/Patches/Client/VersionShowerPatch.cs` returns 1 match (still shows version)

  **QA Scenarios**:

  ```
  Scenario: No green color in version text
    Tool: Bash (grep)
    Steps:
      1. Run: grep -n "#0dff00\|color=" src/Patches/Client/VersionShowerPatch.cs
      2. Assert: 0 matches
    Expected Result: No color formatting
    Evidence: .sisyphus/evidence/task-5-color.txt

  Scenario: Version still displayed
    Tool: Bash (grep)
    Steps:
      1. Run: grep -n "SurferPlugin.GetVersionText" src/Patches/Client/VersionShowerPatch.cs
      2. Assert: at least 1 match
    Expected Result: Version display preserved
    Evidence: .sisyphus/evidence/task-5-version.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. Run: dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Expected Result: Compiles cleanly
    Evidence: .sisyphus/evidence/task-5-build.txt
  ```

  **Evidence to Capture**:
  - [ ] `task-5-color.txt`
  - [ ] `task-5-version.txt`
  - [ ] `task-5-build.txt`

  **Commit**: NO (groups with Wave 1)

- [x] 6. **Clean ModManagerPatch + SplashIntroPatch: remove logo/stamp branding**

  **What to do**:
  
  **Part A — ModManagerPatch.cs** (`src/Patches/Client/Managers/ModManagerPatch.cs`):
  - REMOVE the mod stamp replacement block (lines 26-41 in the `LateUpdate_Postfix`):
    - Remove the `if (__instance.ModStamp.gameObject.active == true)` block and everything inside it
    - Remove the `modStamp` field (line 13)
    - Remove the `Surfer.Helpers` using if not needed elsewhere
  - KEEP: `SplashIntroPatch.IsReallyDoneLoading` check (line 20-23), `__instance.ShowModStamp()` call, `BetterAntiCheat.Update()`, `LateTask.UpdateAll()`, `BetterNotificationManager.Update()`
  
  **Part B — SplashIntroPatch.cs** (`src/Patches/Client/SplashIntroPatch.cs`):
  - REMOVE the logo replacement: the entire `ReplaceLogo()` method (lines 107-118)
  - REMOVE the call to `ReplaceLogo(__instance)` inside `StartBetterIntro()` (line 97)
  - REMOVE the `_betterLogo` field (line 17)
  - KEEP: skip-on-click, fast-load logic, `IsReallyDoneLoading` flag, audio destruction, black overlay management — these are all functional improvements

  **Must NOT do**:
  - Do NOT remove the skip/fast-load logic
  - Do NOT change `IsReallyDoneLoading` or how it interacts with ModManagerPatch
  - Do NOT remove `ShowModStamp()` call

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Two files, well-defined removals
  - **Skills**: [none]

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with T1, T2, T3, T4, T5)
  - **Blocks**: None
  - **Blocked By**: None

  **References**:
  - `src/Patches/Client/Managers/ModManagerPatch.cs` — full file (49 lines): stamp replacement at 26-41
  - `src/Patches/Client/SplashIntroPatch.cs` — full file (137 lines): ReplaceLogo at 107-118, call at 97, field at 17

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -n "modStamp" src/Patches/Client/Managers/ModManagerPatch.cs` returns 0 matches
  - [ ] `grep -n "Surfer-Mod" src/Patches/Client/Managers/ModManagerPatch.cs` returns 0 matches
  - [ ] `grep -n "ReplaceLogo\|_betterLogo\|Surfer-Logo" src/Patches/Client/SplashIntroPatch.cs` returns 0 matches
  - [ ] `grep -n "IsReallyDoneLoading" src/Patches/Client/SplashIntroPatch.cs` returns ≥ 1 match (flag preserved)
  - [ ] `grep -n "BetterAntiCheat.Update\|LateTask.UpdateAll\|BetterNotificationManager" src/Patches/Client/Managers/ModManagerPatch.cs` returns 3 matches (functional calls preserved)

  **QA Scenarios**:

  ```
  Scenario: No stamp or logo branding remains
    Tool: Bash (grep)
    Steps:
      1. Run: grep -rn "Surfer-Mod\|Surfer-Logo\|ReplaceLogo\|modStamp" src/Patches/Client/Managers/ModManagerPatch.cs src/Patches/Client/SplashIntroPatch.cs
      2. Assert: 0 matches
    Expected Result: All logo/stamp branding removed
    Evidence: .sisyphus/evidence/task-6-branding.txt

  Scenario: Functional code preserved
    Tool: Bash (grep)
    Steps:
      1. Run: grep -n "IsReallyDoneLoading\|ShowModStamp\|BetterAntiCheat.Update\|LateTask.UpdateAll\|BetterNotificationManager.Update" src/Patches/Client/Managers/ModManagerPatch.cs src/Patches/Client/SplashIntroPatch.cs
      2. Assert: ≥ 5 total matches (all functional pieces present)
    Expected Result: All functional code intact
    Evidence: .sisyphus/evidence/task-6-functional.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. Run: dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Expected Result: Compiles cleanly
    Evidence: .sisyphus/evidence/task-6-build.txt
  ```

  **Evidence to Capture**:
  - [ ] `task-6-branding.txt`
  - [ ] `task-6-functional.txt`
  - [ ] `task-6-build.txt`

  **Commit**: NO (groups with Wave 1)

- [x] 7. **Clean ClientPatch: remove green buttons + green warning popup text**

  **What to do**:
  - Edit `src/Patches/Client/ClientPatch.cs`:
  - **REMOVE** the `AccountTab_Awake_Postfix` method entirely (lines 17-23) — this applies `SetUIColors()` to the friends button with green tint
  - In `SignInStatusComponent_SetOnline_Prefix` (lines 27-68):
    - Change the popup text on line 47: remove `<color=#0dff00>` wrapping and "Better Among Us" text, replace with neutral version:
      ```csharp
      Utils.ShowPopUp($"<size=200%>-= <b>Warning</b> =-</size>\n\n" +
          $"<size=125%>Surfer {SurferPlugin.GetVersionText()}\nsupports Among Us {verText},\n" +
          $"Among Us <b>{SurferPlugin.AppVersion}</b> is above the supported versions!\n" +
          $"You may encounter minor to game breaking bugs.</size>");
      ```
    - Do the same for the second popup (lines 61-64): remove color tags and "Better Among Us"
  - Remove `using Surfer.Helpers;` if no other code uses `ObjectHelper`
  - KEEP: all version checking logic, all exit game logic, all co-loading logic, all chat-related code
  
  **Must NOT do**:
  - Do NOT change version checking logic or popup conditions
  - Do NOT touch `AmongUsClient_ExitGame_Postfix`, `AmongUsClient_OnGameEnd_Prefix`, `AmongUsClient_CoStartGame_Postfix`, or any loading methods

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Defined text removals in one file
  - **Skills**: [none]

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2 (with T8, T9, T10, T11, T12)
  - **Blocks**: None
  - **Blocked By**: None

  **References**:
  - `src/Patches/Client/ClientPatch.cs` — full file (260 lines): AccountTab postfix at 17-23, warning popups at 46-49 and 61-64

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -n "#0dff00" src/Patches/Client/ClientPatch.cs` returns 0 matches
  - [ ] `grep -n "Better Among Us" src/Patches/Client/ClientPatch.cs` returns 0 matches
  - [ ] `grep -n "SetUIColors" src/Patches/Client/ClientPatch.cs` returns 0 matches
  - [ ] `grep -n "AccountTab_Awake_Postfix" src/Patches/Client/ClientPatch.cs` returns 0 (method removed)

  **QA Scenarios**:

  ```
  Scenario: No green branding in ClientPatch
    Tool: Bash (grep)
    Steps:
      1. Run: grep -n "#0dff00\|Better Among Us\|SetUIColors" src/Patches/Client/ClientPatch.cs
      2. Assert: 0 matches
    Expected Result: All green/branding removed
    Evidence: .sisyphus/evidence/task-7-branding.txt

  Scenario: Warning popups still functional
    Tool: Bash (grep)
    Steps:
      1. Run: grep -n "ShowPopUp\|SurferPlugin.GetVersionText" src/Patches/Client/ClientPatch.cs
      2. Assert: 2+ ShowPopUp matches, 2+ GetVersionText matches
    Expected Result: Popups still present, version references updated
    Evidence: .sisyphus/evidence/task-7-functional.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. Run: dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Expected Result: Compiles cleanly
    Evidence: .sisyphus/evidence/task-7-build.txt
  ```

  **Evidence to Capture**:
  - [ ] `task-7-branding.txt`
  - [ ] `task-7-functional.txt`
  - [ ] `task-7-build.txt`

  **Commit**: NO (groups with Wave 2)

- [x] 8. **Clean FindAGameManagerPatch: remove green hover and button tints**

  **What to do**:
  - Edit `src/Patches/Gameplay/Managers/FindAGameManagerClass/FindAGameManagerPatch.cs`:
  - Remove all `SetUIColors()` calls from server/dropdown buttons (look for `SetUIColors(sprite => ...)` and `SetUIColors("Icon", ...)` patterns)
  - Remove the green hover tint: line that does `roll.OverColor = (roll.OverColor * 0.6f) + (Color.green * 0.5f)`
  - Remove any `using Surfer.Helpers;` if it was only needed for `SetUIColors` / `AddColor`
  - KEEP: all scroll/pagination logic, server filtering, game list modifications

  **Must NOT do**:
  - Do NOT break game list functionality
  - Do NOT remove the server list modifications

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Focused removals in one patch file
  - **Skills**: [none]

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2 (with T7, T9, T10, T11, T12)
  - **Blocks**: None
  - **Blocked By**: None

  **References**:
  - `src/Patches/Gameplay/Managers/FindAGameManagerClass/FindAGameManagerPatch.cs` — contains `SetUIColors` and `OverColor = ... Color.green` calls

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -n "SetUIColors\|Color\.green" src/Patches/Gameplay/Managers/FindAGameManagerClass/FindAGameManagerPatch.cs` returns 0 matches

  **QA Scenarios**:

  ```
  Scenario: No green/theme calls in FindAGameManagerPatch
    Tool: Bash (grep)
    Steps:
      1. Run: grep -n "SetUIColors\|Color\.green\|AddColor" src/Patches/Gameplay/Managers/FindAGameManagerClass/FindAGameManagerPatch.cs
      2. Assert: 0 matches
    Expected Result: All theme calls removed
    Evidence: .sisyphus/evidence/task-8-theme.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. Run: dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Expected Result: Compiles cleanly
    Evidence: .sisyphus/evidence/task-8-build.txt
  ```

  **Evidence to Capture**:
  - [ ] `task-8-theme.txt`
  - [ ] `task-8-build.txt`

  **Commit**: NO (groups with Wave 2)

- [x] 9. **Clean LobbyPatch: remove green button tints**

  **What to do**:
  - Edit `src/Patches/Gameplay/LobbyPatch.cs`:
  - Find and remove all `SetUIColors()` calls on lobby buttons (start button, edit button, view button, settings tabs)
  - Pattern to remove: lines that call `button.gameObject.SetUIColors("Icon")` or similar
  - Remove `using Surfer.Helpers;` if only needed for `SetUIColors`
  - KEEP: all lobby functionality (cancel-start, min-players override, timer, settings-tab behavior)

  **Must NOT do**:
  - Do NOT remove lobby gameplay modifications (start button behavior, player count overrides, timer)
  - Do NOT touch settings tab positioning/behavior (non-color changes)

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Single file, focused removals
  - **Skills**: [none]

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2 (with T7, T8, T10, T11, T12)
  - **Blocks**: None
  - **Blocked By**: None

  **References**:
  - `src/Patches/Gameplay/LobbyPatch.cs` — contains `SetUIColors("Icon")` calls on start/edit/view buttons (~lines 54-57)

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -n "SetUIColors" src/Patches/Gameplay/LobbyPatch.cs` returns 0 matches

  **QA Scenarios**:

  ```
  Scenario: No SetUIColors in LobbyPatch
    Tool: Bash (grep)
    Steps:
      1. Run: grep -n "SetUIColors" src/Patches/Gameplay/LobbyPatch.cs
      2. Assert: 0 matches
    Expected Result: All theme calls removed
    Evidence: .sisyphus/evidence/task-9-theme.txt

  Scenario: Functional code intact
    Tool: Bash (grep)
    Steps:
      1. Run: grep -n "StartButton\|cancelStart\|minPlayers\|timer\|settingsTab" src/Patches/Gameplay/LobbyPatch.cs
      2. Assert: multiple matches for functional features
    Expected Result: Gameplay code preserved
    Evidence: .sisyphus/evidence/task-9-functional.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. Run: dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Expected Result: Compiles cleanly
    Evidence: .sisyphus/evidence/task-9-build.txt
  ```

  **Evidence to Capture**:
  - [ ] `task-9-theme.txt`
  - [ ] `task-9-functional.txt`
  - [ ] `task-9-build.txt`

  **Commit**: NO (groups with Wave 2)

- [x] 10. **Clean BetterPingTracker: de-brand green text, keep functional overlay**

  **What to do**:
  - Edit `src/Mono/BetterPingTracker.cs`:
  - **REMOVE** or neutralize the "Surfer v1.3.2" branding text (line 71/72): change the version display text from branded "Surfer" to just a neutral version or remove the string entirely
  - **REMOVE** the GitHub URL display (line 72 area)
  - **STRIP** the green `#0dff00` coloring from FPS text: change FPS display color to a neutral color (white or grey)
  - **KEEP**: all ping functionality, lobby timer, host name display, ping color gradient (this is functional, not brand)
  - **KEEP**: the functional overlay structure — only strip text content and colors that reference Surfer branding

  **Must NOT do**:
  - Do NOT remove the ping tracker overlay itself
  - Do NOT remove FPS counter (it's toggleable in ImGUI via ShowFPS)
  - Do NOT change the ping gradient colors (these are functional status indicators)

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Single file, text/color changes
  - **Skills**: [none]

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2 (with T7, T8, T9, T11, T12)
  - **Blocks**: None
  - **Blocked By**: None

  **References**:
  - `src/Mono/BetterPingTracker.cs` — contains line 71 (Surfer version text), line 72 (GitHub URL), FPS color (green)

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -n "#0dff00.*Surfer\|Surfer.*v1\.\|github\.com/D1GQ" src/Mono/BetterPingTracker.cs` returns 0 matches
  - [ ] `grep -n "ShowFPS\|FPS\|ping\|lobbyTimer\|hostName" src/Mono/BetterPingTracker.cs` returns ≥ 4 matches (functional features preserved)

  **QA Scenarios**:

  ```
  Scenario: No Surfer branding in ping tracker
    Tool: Bash (grep)
    Steps:
      1. Run: grep -n "Surfer v\|github.com/D1GQ\|#0dff00" src/Mono/BetterPingTracker.cs
      2. Assert: 0 matches for branding
    Expected Result: All branding removed
    Evidence: .sisyphus/evidence/task-10-branding.txt

  Scenario: Functional features preserved
    Tool: Bash (grep)
    Steps:
      1. Run: grep -n "ShowFPS\|FPS\|Ping\|lobbyTimer\|hostName\|version" src/Mono/BetterPingTracker.cs
      2. Assert: ≥ 4 matches for functional features
    Expected Result: All functional code intact
    Evidence: .sisyphus/evidence/task-10-functional.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. Run: dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Expected Result: Compiles cleanly
    Evidence: .sisyphus/evidence/task-10-build.txt
  ```

  **Evidence to Capture**:
  - [ ] `task-10-branding.txt`
  - [ ] `task-10-functional.txt`
  - [ ] `task-10-build.txt`

  **Commit**: NO (groups with Wave 2)

- [x] 11. **Clean BetterNotificationManager + PlayerInfoDisplay: de-brand green labels**

  **What to do**:
  
  **Part A — BetterNotificationManager.cs** (`src/Managers/BetterNotificationManager.cs`):
  - Change the `#00ff44` green color on the system notification label (line ~46) to a neutral color (white or grey)
  - KEEP: all notification logic, anti-cheat alert functionality
  
  **Part B — PlayerInfoDisplay.cs** (`src/Mono/PlayerInfoDisplay.cs`):
  - Change the `#0dff00` green "BetterUser" tag (line ~380) to a neutral color (white) and rename text from "BetterUser" to "Mod"
  - Change any other green-tagged labels (cheater tags, etc.) to neutral colors if they use Surfer-branded green
  - KEEP: all player info display logic, role info, task counts, anti-cheat tag detection

  **Must NOT do**:
  - Do NOT remove the notification system or player info display
  - Do NOT change functional tag detection logic — only colors and "BetterUser" text

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Two files, focused color/text changes
  - **Skills**: [none]

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2 (with T7, T8, T9, T10, T12)
  - **Blocks**: None
  - **Blocked By**: None

  **References**:
  - `src/Managers/BetterNotificationManager.cs` — `#00ff44` green label at ~line 46
  - `src/Mono/PlayerInfoDisplay.cs` — `#0dff00` "BetterUser" at ~line 380

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -n "#00ff44" src/Managers/BetterNotificationManager.cs` returns 0 matches
  - [ ] `grep -n "#0dff00.*BetterUser\|BetterUser.*#0dff00" src/Mono/PlayerInfoDisplay.cs` returns 0 matches
  - [ ] `grep -n "BetterNotificationManager\|ShowNotification\|SystemNotification" src/Managers/BetterNotificationManager.cs` returns ≥ 3 matches (functional preserved)
  - [ ] `grep -n "PlayerInfo\|DisplayInfo\|ShowTags\|tag\|cheater" src/Mono/PlayerInfoDisplay.cs` returns ≥ 4 matches (functional preserved)

  **QA Scenarios**:

  ```
  Scenario: No green branding in notification/player info
    Tool: Bash (grep)
    Steps:
      1. Run: grep -n "#00ff44" src/Managers/BetterNotificationManager.cs
      2. Run: grep -n "#0dff00.*BetterUser\|BetterUser.*#0dff00" src/Mono/PlayerInfoDisplay.cs
      3. Assert: both return 0 matches
    Expected Result: Green branding removed from both files
    Evidence: .sisyphus/evidence/task-11-branding.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. Run: dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Expected Result: Compiles cleanly
    Evidence: .sisyphus/evidence/task-11-build.txt
  ```

  **Evidence to Capture**:
  - [ ] `task-11-branding.txt`
  - [ ] `task-11-build.txt`

  **Commit**: NO (groups with Wave 2)

- [x] 12. **Clean HelpCommand + AnnouncementPanelPatch + ChatPatch: de-brand remaining green text**

  **What to do**:
  
  **Part A — HelpCommand.cs** (`src/Commands/HelpCommand.cs`):
  - Change the green `#0dff00` "Surfer" welcome text (line 12) to neutral text without color:
    ```csharp
    return "Welcome to Surfer! Type /commands for available commands.";
    ```
    (no HTML color tags)
  
  **Part B — AnnouncementPanelPatch.cs** (`src/Patches/Client/AnnouncementPanelPatch.cs`):
  - Remove the Surfer-Icon display on mod news: remove the entire `AnnouncementPanel_SetUpPanel_Postfix` method (lines 53-85) — this adds a `Surfer-Icon.png` to mod announcements
  - KEEP: the `PlayerAnnouncementData_SetModAnnouncements_Prefix` method (lines 16-51) — mod news functionality still works, just without the icon
  
  **Part C — ChatPatch.cs** (`src/Patches/Gameplay/UI/Chat/ChatPatch.cs`):
  - Find `#0dff00` or green references related to "Surfer user" tags in chat — these are at ~line 156 and ~line 320 (text length gradient)
  - Change the green chat tag color from `#0dff00` to a neutral grey/white
  - Keep the chat role display logic — only change colors
  
  **Must NOT do**:
  - Do NOT remove the `/help` command entirely
  - Do NOT remove mod news loading functionality
  - Do NOT remove chat role display logic

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Three files, small focused changes each
  - **Skills**: [none]

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2 (with T7, T8, T9, T10, T11)
  - **Blocks**: None
  - **Blocked By**: None

  **References**:
  - `src/Commands/HelpCommand.cs` — green `#0dff00` Surfer text at line 12
  - `src/Patches/Client/AnnouncementPanelPatch.cs` — icon display at lines 53-85
  - `src/Patches/Gameplay/UI/Chat/ChatPatch.cs` — green chat tags at ~line 156 and ~line 320

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -n "#0dff00" src/Commands/HelpCommand.cs` returns 0 matches
  - [ ] `grep -n "Surfer-Icon" src/Patches/Client/AnnouncementPanelPatch.cs` returns 0 matches
  - [ ] `grep -n "#0dff00.*Surfer\|Surfer.*#0dff00" src/Patches/Gameplay/UI/Chat/ChatPatch.cs` returns 0 matches
  - [ ] `grep -n "ProcessModNewsFiles\|ToAnnouncement\|SetAnnouncements" src/Patches/Client/AnnouncementPanelPatch.cs` returns ≥ 3 matches (news functionality preserved)

  **QA Scenarios**:

  ```
  Scenario: No green branding in help/announcement/chat
    Tool: Bash (grep)
    Steps:
      1. Run: grep -rn "#0dff00\|Surfer-Icon" src/Commands/HelpCommand.cs src/Patches/Client/AnnouncementPanelPatch.cs src/Patches/Gameplay/UI/Chat/ChatPatch.cs
      2. Assert: 0 matches for brand-related green/icon references
    Expected Result: All green branding and icon references removed
    Evidence: .sisyphus/evidence/task-12-branding.txt

  Scenario: Functional code preserved
    Tool: Bash (grep)
    Steps:
      1. Run: grep -n "ProcessModNewsFiles\|ToAnnouncement\|SetAnnouncements\|/help\|/commands\|chat.*role\|chat.*tag" src/Commands/HelpCommand.cs src/Patches/Client/AnnouncementPanelPatch.cs src/Patches/Gameplay/UI/Chat/ChatPatch.cs
      2. Assert: Multiple functional matches across all three files
    Expected Result: All functional code intact
    Evidence: .sisyphus/evidence/task-12-functional.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. Run: dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Expected Result: Compiles cleanly
    Evidence: .sisyphus/evidence/task-12-build.txt
  ```

  **Evidence to Capture**:
  - [ ] `task-12-branding.txt`
  - [ ] `task-12-functional.txt`
  - [ ] `task-12-build.txt`

  **Commit**: NO (groups with Wave 2)

- [x] 13. **Restructure GameSettingsPatch: remove green OptionTab, preserve OptionItem data**

  **What to do**:
  - Edit `src/Patches/Gameplay/UI/Settings/GameSettingsPatch.cs`:
  
  **REMOVE the green tab from gear menu:**
  - Remove the `OptionTab.Create` call (line 50):
    ```csharp
    BetterSettingsTab = OptionTab.Create(3, "BetterSetting", "BetterSetting.Description", Color.green);
    ```
    Replace with a null assignment or just remove the line (since we still need `BetterSettingsTab` to exist as a parent for the items, or we can make all items parent-less if the OptionItem API supports it).
  - **IMPORTANT**: Check how `OptionCheckboxItem.Create`, `OptionStringItem.Create`, `OptionIntItem.Create` use the `BetterSettingsTab` parameter. These are Among Us game-level option items. If they require a parent `OptionTab` to function, keep a minimal `OptionTab` creation WITHOUT visual registration:
    - Keep `BetterSettingsTab` as a field but don't add it to the game's settings UI
    - Remove the `Color.green` parameter — use `Color.white` or no color
  - **REMOVE** the `GameSettingMenu_Start_Postfix` method (lines 124-167) — this injects the tab button into the gear menu and adjusts layout
  - **REMOVE** the `GameSettingMenu_ChangeTab_Prefix` method (lines 169-191) — this handles tab switching to index 3 (the Better Settings tab)
  
  **KEEP the option items:**
  - **KEEP** all of `SetupSettings()` except the `OptionTab.Create` line — all `OptionHeaderItem`, `OptionTitleItem`, `OptionCheckboxItem`, `OptionStringItem`, `OptionIntItem`, `OptionPlayerItem` creations must stay
  - **KEEP** the `BetterGameSettings` class with all its static fields
  - **KEEP** the `BetterGameSettingsTemp` class with HideAndSeek player items
  - **KEEP** `GameOptionsMenu_CreateSettings_Prefix` — prevents game from trying to render our orphaned items
  - **KEEP** `OptionsConsole_CanUse_Prefix` — allows all option items to be usable

  **Must NOT do**:
  - Do NOT delete any `BetterGameSettings.*` static fields
  - Do NOT delete any `OptionItem.Create` calls (except the `OptionTab.Create`)
  - Do NOT change the order or parameters of OptionItem creation
  - Do NOT break the dependency chain: `SetupSettings()` → `BetterSettingsTab` (parent) → individual items

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
    - Reason: Complex restructuring of game-level option system — need to carefully preserve data while removing UI. Higher effort than quick tasks.
  - **Skills**: [none]

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 3 (sequential, before T14)
  - **Blocks**: T14
  - **Blocked By**: None (runs after Wave 2)

  **References**:
  - `src/Patches/Gameplay/UI/Settings/GameSettingsPatch.cs` — full file (215 lines)
  - `src/Modules/OptionItems/OptionTab.cs` — `OptionTab.Create` method for understanding the API
  - `src/Modules/OptionItems/OptionCheckboxItem.cs` — uses `Color.green` for On state (line 108) — may need to be aware of this but don't change it
  - `src/Patches/Gameplay/Anticheat/VoteBanSystemPatch.cs` — reads `BetterGameSettings.WhenCheating`
  - `src/Patches/Gameplay/Player/PlayerControlPatch.cs` — may read BetterGameSettings items
  - `src/Mono/PlayerInfoDisplay.cs` — reads `BetterGameSettings.InvalidFriendCode.GetBool()` at ~line 246
  - `src/Managers/BetterNotificationManager.cs` — reads `BetterGameSettings.CensorDetectionReason.GetBool()` at ~line 78

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -c "BetterSettingsTab" src/Patches/Gameplay/UI/Settings/GameSettingsPatch.cs` returns 0 (green tab removed)
  - [ ] `grep -c "GameSettingMenu_Start_Postfix\|GameSettingMenu_ChangeTab_Prefix" src/Patches/Gameplay/UI/Settings/GameSettingsPatch.cs` returns 0 (UI injection methods removed)
  - [ ] `grep -c "BetterGameSettings\." src/Patches/Gameplay/UI/Settings/GameSettingsPatch.cs` returns ≥ 17 (all option items still exist)
  - [ ] `grep -c "OptionCheckboxItem.Create\|OptionStringItem.Create\|OptionIntItem.Create\|OptionPlayerItem.Create" src/Patches/Gameplay/UI/Settings/GameSettingsPatch.cs` returns ≥ 14 (all item creation preserved)
  - [ ] `grep -rn "BetterGameSettings\." src/ --include="*.cs" | wc -l` returns ≥ 17 (all anti-cheat reads across codebase preserved)

  **QA Scenarios**:

  ```
  Scenario: Tab removed but items preserved
    Tool: Bash (grep)
    Steps:
      1. Run: grep -n "BetterSettingsTab" src/Patches/Gameplay/UI/Settings/GameSettingsPatch.cs
      2. Assert: 0 matches
      3. Run: grep -n "BetterGameSettings\." src/Patches/Gameplay/UI/Settings/GameSettingsPatch.cs | wc -l
      4. Assert: count is ≥ 17
    Expected Result: Tab references gone, all option items still declared
    Evidence: .sisyphus/evidence/task-13-tab-items.txt

  Scenario: Cross-codebase anti-cheat reads preserved
    Tool: Bash (grep)
    Steps:
      1. Run: grep -rn "BetterGameSettings\." src/ --include="*.cs" | wc -l
      2. Assert: count is ≥ 17
    Expected Result: All anti-cheat reads across codebase intact
    Evidence: .sisyphus/evidence/task-13-crossrefs.txt

  Scenario: Build succeeds with restructured settings
    Tool: Bash
    Steps:
      1. Run: dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Expected Result: Compiles cleanly after tab removal
    Evidence: .sisyphus/evidence/task-13-build.txt
  ```

  **Evidence to Capture**:
  - [ ] `task-13-tab-items.txt`
  - [ ] `task-13-crossrefs.txt`
  - [ ] `task-13-build.txt`

  **Commit**: NO (groups with Wave 3)

- [x] 14. **Add anti-cheat settings to SurferMenu ImGUI**

  **What to do**:
  - Edit `src/Mono/SurferMenu.cs`:
  
  **Add a new "Anti-Cheat" tab** (as tab index 1 or 2, shifting existing tabs):
  - Add to `Start()`: `_tabs.Add(("Anti-Cheat", DrawAntiCheatTab));` after the Host tab
  - New tab order: General, Host, Anti-Cheat, About
  
  **Implement DrawAntiCheatTab()** with all 17 anti-cheat/gameplay settings from `BetterGameSettings`:
  
  See how T13 preserves `BetterGameSettings.*` static OptionItem fields. This task creates ImGUI controls that read/write to them.
  
  **Section: Anti-Cheat Detection** — Use `OptionStringItem` dropdown cycle buttons:
  ```csharp
  DrawDropdown("When Cheating", BetterGameSettings.WhenCheating);
  ```
  where `DrawDropdown` cycles through the OptionStringItem's options on click.
  
  **Section: Detection Settings** — Use checkbox toggles:
  - `InvalidFriendCode` (bool)
  - `CancelInvalidSabotage` (bool)
  - `UseBanPlayerList` (bool)
  - `UseBanNameList` (bool)
  - `UseBanWordList` (bool)
  - `UseBanWordListOnlyLobby` (bool, only enabled when `UseBanWordList` is true)
  - `CensorDetectionReason` (bool)
  - `DetectCheatClients` (bool)
  - `DetectInvalidRPCs` (bool)
  
  **Section: Thresholds** — Use slider controls:
  - `DetectedLevelAbove` (int, range 100-10000, step 5) — show as "Min Level to Detect: {value}"
  - `KickLevelBelow` (int, range 0-10000, step 1) — show as "Min Level to Kick: {value}"
  
  **Section: Role Algorithm** — Use dropdown + checkbox:
  - `RoleRandomizer` (OptionStringItem: "System.Random" / "UnityEngine.Random")
  - `DesyncRoles` (bool)
  
  **Section: Gameplay** — Use checkboxes:
  - `DisableSabotages` (bool)
  - `RemovePetOnDeath` (bool)
  
  **Section: Hide & Seek** (only if `GameState.IsHideNSeek`):
  - `HideAndSeekImpNum` (int slider, 1-5)
  - `HideAndSeekImp2-5` (player selection dropdowns, shown conditionally based on ImpNum and previous selections)
  
  **Helper methods to add** to SurferMenu:
  - `DrawOptionToggle(string label, OptionCheckboxItem item)` — toggle that reads/writes `item.GetValue()`/`item.SetValue()`
  - `DrawOptionDropdown(string label, OptionStringItem item)` — cycle button showing current selection, advances on click
  - `DrawOptionSlider(string label, OptionIntItem item)` — horizontal slider with min/max from item, reads/writes value
  - Each helper should use the existing `PurpleOn`/`PurpleOff` color scheme for consistency
  
  **IMPORTANT**: The OptionItem API uses:
  - `OptionCheckboxItem.GetValue()` → `bool`
  - `OptionCheckboxItem.SetValue(bool)`
  - `OptionStringItem.GetValue()` → `int` (selected index), `GetString()` → translated display string
  - `OptionStringItem.SetValue(int)`
  - `OptionIntItem.GetValue()` → `int`, `SetValue(int)`
  - For OptionStringItem, you need to get the Options array to cycle through and translate each key via `Translator.GetString(key)`.
  
  Add `using Surfer.Patches.Gameplay.UI.Settings;` to access `BetterGameSettings`.
  Add `using Surfer.Modules;` to access `Translator`.
  
  **Must NOT do**:
  - Do NOT create new BepInEx ConfigEntry values — use existing `BetterGameSettings.*` OptionItems
  - Do NOT change the existing General/Host/About tabs beyond adding the new tab between them
  - Do NOT remove existing toggle controls
  - Do NOT change the purple color scheme

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
    - Reason: Large task — 17+ controls with helper methods, dropdown implementation, conditional visibility for HideAndSeek items. Significant ImGUI coding.
  - **Skills**: [none]

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 3 (sequential, after T13)
  - **Blocks**: T15
  - **Blocked By**: T13 (needs `BetterGameSettings.*` OptionItems to be accessible)

  **References**:
  - `src/Mono/SurferMenu.cs` — existing structure: `Start()` lines 20-27, `DrawWindow()` lines 48-68, `DrawGeneralTab()` lines 72-96, `DrawHostTab()` lines 100-120, `DrawAboutTab()` lines 124-142, `DrawToggle()` lines 146-165, `SurferStyles` lines 168-182
  - `src/Patches/Gameplay/UI/Settings/GameSettingsPatch.cs` — `BetterGameSettings` class with all 17+ static OptionItem fields (lines 12-39), `BetterGameSettingsTemp` (lines 33-39)
  - `src/Modules/OptionItems/OptionCheckboxItem.cs` — API: GetValue()/SetValue(bool)
  - `src/Modules/OptionItems/OptionStringItem.cs` — API: GetValue()/SetValue(int), GetString(), Values array
  - `src/Modules/OptionItems/OptionIntItem.cs` — API: GetValue()/SetValue(int), min/max/step from constructor
  - `src/Modules/Translator.cs` — `Translator.GetString(key)` for translating option display strings
  - `src/Modules/GameState.cs` — `GameState.IsHideNSeek` check for conditional HideAndSeek section

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -c "DrawAntiCheatTab" src/Mono/SurferMenu.cs` returns 1 match
  - [ ] `grep -c "DrawOptionToggle\|DrawOptionDropdown\|DrawOptionSlider" src/Mono/SurferMenu.cs` returns ≥ 3 matches (helper methods added)
  - [ ] `grep -c "BetterGameSettings\." src/Mono/SurferMenu.cs` returns ≥ 17 matches (all settings referenced in ImGUI)
  - [ ] `grep -c "_tabs.Add.*Anti-Cheat" src/Mono/SurferMenu.cs` returns 1 match (new tab registered)

  **QA Scenarios**:

  ```
  Scenario: All anti-cheat settings accessible from ImGUI
    Tool: Bash (grep)
    Steps:
      1. Run: grep -c "BetterGameSettings\." src/Mono/SurferMenu.cs
      2. Assert: count is ≥ 17
    Expected Result: Every anti-cheat setting has an ImGUI control
    Evidence: .sisyphus/evidence/task-14-settings.txt

  Scenario: Helper methods exist
    Tool: Bash (grep)
    Steps:
      1. Run: grep -n "DrawOptionToggle\|DrawOptionDropdown\|DrawOptionSlider" src/Mono/SurferMenu.cs
      2. Assert: at least 3 helper methods defined
    Expected Result: All three control types have helper methods
    Evidence: .sisyphus/evidence/task-14-helpers.txt

  Scenario: Build succeeds with full ImGUI migration
    Tool: Bash
    Steps:
      1. Run: dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Expected Result: Compiles with all new ImGUI controls and BetterGameSettings references
    Evidence: .sisyphus/evidence/task-14-build.txt
  ```

  **Evidence to Capture**:
  - [ ] `task-14-settings.txt`
  - [ ] `task-14-helpers.txt`
  - [ ] `task-14-build.txt`

  **Commit**: NO (groups with Wave 3)

- [x] 15. **Clean stale BetterAmongUs build artifacts**

  **What to do**:
  - Delete all stale `BetterAmongUs.*` files from the build output directory:
    ```bash
    rm -f src/bin/Release/net6.0/BetterAmongUs.dll
    rm -f src/bin/Release/net6.0/BetterAmongUs.pdb
    rm -f src/bin/Release/net6.0/BetterAmongUs.deps.json
    ```
  - Also delete stale obj files:
    ```bash
    rm -f src/obj/BetterAmongUs.csproj.nuget.dgspec.json
    rm -f src/obj/BetterAmongUs.csproj.nuget.g.props
    rm -f src/obj/BetterAmongUs.csproj.nuget.g.targets
    rm -f src/obj/Release/net6.0/BetterAmongUs.*
    ```
  - Then run `dotnet clean src/Surfer.csproj` to ensure a fresh build state
  - **IMPORTANT**: Run a fresh `dotnet build` AFTER cleaning to verify nothing is referencing the stale artifacts

  **Must NOT do**:
  - Do NOT delete `Surfer.dll`, `Surfer.pdb`, or `Surfer.deps.json`
  - Do NOT delete any source files
  - Do NOT modify `.gitignore`

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: File cleanup + build verification
  - **Skills**: [none]

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 4 (sequential, before T16)
  - **Blocks**: T16
  - **Blocked By**: T14

  **References**:
  - `src/bin/Release/net6.0/` — directory containing both Surfer.dll and stale BetterAmongUs.dll artifacts
  - `src/obj/` — build intermediates with BetterAmongUs csproj artifacts

  **Acceptance Criteria**:
  - [ ] `ls src/bin/Release/net6.0/BetterAmongUs.dll 2>&1` returns "No such file or directory"
  - [ ] `ls src/bin/Release/net6.0/BetterAmongUs.pdb 2>&1` returns "No such file or directory"
  - [ ] `ls src/bin/Release/net6.0/BetterAmongUs.deps.json 2>&1` returns "No such file or directory"
  - [ ] `ls src/bin/Release/net6.0/Surfer.dll` returns the file path (exists)
  - [ ] `dotnet clean src/Surfer.csproj && dotnet build src/Surfer.csproj --configuration Release` exits 0

  **QA Scenarios**:

  ```
  Scenario: Stale BetterAmongUs artifacts removed
    Tool: Bash
    Steps:
      1. Run: ls src/bin/Release/net6.0/BetterAmongUs.* 2>&1
      2. Assert: "No such file or directory" for all three
    Expected Result: All stale artifacts gone
    Evidence: .sisyphus/evidence/task-15-stale.txt

  Scenario: Surfer.dll still exists
    Tool: Bash
    Steps:
      1. Run: ls -la src/bin/Release/net6.0/Surfer.dll
      2. Assert: File exists with non-zero size
    Expected Result: Correct output DLL present
    Evidence: .sisyphus/evidence/task-15-surfer.txt

  Scenario: Fresh build succeeds after cleanup
    Tool: Bash
    Steps:
      1. Run: dotnet clean src/Surfer.csproj --configuration Release
      2. Run: dotnet build src/Surfer.csproj --configuration Release
      3. Assert: exit code 0 for both
    Expected Result: Clean build produces Surfer.dll without stale artifacts
    Evidence: .sisyphus/evidence/task-15-build.txt
  ```

  **Evidence to Capture**:
  - [ ] `task-15-stale.txt`
  - [ ] `task-15-surfer.txt`
  - [ ] `task-15-build.txt`

  **Commit**: NO (groups with Wave 4)

- [x] 16. **Build, copy to game plugin directory, final verify**

  **What to do**:
  - Run the full release build:
    ```bash
    dotnet build src/Surfer.csproj --configuration Release
    ```
  - Verify the build output:
    ```bash
    ls -la src/bin/Release/net6.0/Surfer.dll
    ```
  - Copy the built DLL to the Among Us BepInEx plugins directory:
    ```bash
    cp src/bin/Release/net6.0/Surfer.dll "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/"
    ```
  - Run ALL Final Verification checks (F1-F7 from the Final Verification Wave section)
  - If any check fails, fix the corresponding task and re-verify
  
  **Build path note**: The `.csproj` has a `CopyModToGame` MSBuild target that reads from `build/mod_folder_path`. If that file contains the correct path, you can also use:
    ```bash
    dotnet build src/Surfer.csproj --configuration Release -p:CopyToGame=true
    ```
  Verify `build/mod_folder_path` content before using this approach.

  **Must NOT do**:
  - Do NOT skip any Final Verification checks
  - Do NOT proceed if any grep check fails

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Build + copy + verification commands
  - **Skills**: [none]

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 4 (last task)
  - **Blocks**: None (final task)
  - **Blocked By**: T15

  **References**:
  - `src/Surfer.csproj` — MSBuild targets for CopyModToGame, mod_folder_path
  - `src/build/mod_folder_path` — deploy path file

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `ls -la src/bin/Release/net6.0/Surfer.dll` — file exists and is less than 5 minutes old
  - [ ] DLL copied to `/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/Surfer.dll`
  - [ ] All 7 Final Verification checks pass (F1-F7)

  **QA Scenarios**:

  ```
  Scenario: Release build produces clean Surfer.dll
    Tool: Bash
    Steps:
      1. Run: dotnet build src/Surfer.csproj --configuration Release 2>&1
      2. Assert: exit code 0, output contains "Build succeeded" with 0 errors
      3. Run: ls -la src/bin/Release/net6.0/Surfer.dll
      4. Assert: File exists, size > 0
    Expected Result: Clean release build
    Evidence: .sisyphus/evidence/task-16-build.txt

  Scenario: DLL copied to Among Us plugins
    Tool: Bash
    Steps:
      1. Run: cp src/bin/Release/net6.0/Surfer.dll "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/"
      2. Run: ls -la "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/Surfer.dll"
      3. Assert: File exists
    Expected Result: DLL deployed to game directory
    Evidence: .sisyphus/evidence/task-16-deploy.txt

  Scenario: All Final Verification checks pass
    Tool: Bash
    Steps:
      1. Run F1: dotnet build src/Surfer.csproj --configuration Release → exit 0
      2. Run F2: grep -rn "#0dff00\|Color\.green" src/Patches/ src/Mono/ src/Commands/ src/Managers/ --include="*.cs" | grep -v "VentGroups\|OptionsConsole\|OptionCheckboxItem"
      3. Run F3: grep -rn "SetUIColors\|\.AddColor(" src/Patches/ src/Mono/ src/Commands/ --include="*.cs"
      4. Run F4: grep -c "BetterSettingsTab" src/Patches/Gameplay/UI/Settings/GameSettingsPatch.cs
      5. Run F5: grep -rn "BetterGameSettings\." src/ --include="*.cs" | wc -l
      6. Run F6: ls src/bin/Release/net6.0/BetterAmongUs.* 2>&1
      7. Assert: F1=0, F2=0 matches, F3=0 matches, F4=0, F5≥17, F6="No such file"
    Expected Result: All checks pass
    Evidence: .sisyphus/evidence/task-16-all-checks.txt
  ```

  **Evidence to Capture**:
  - [ ] `task-16-build.txt`
  - [ ] `task-16-deploy.txt`
  - [ ] `task-16-all-checks.txt`

  **Commit**: YES (final)
  - Message: `feat: strip BAU visual branding, fix scroll/zoom, migrate anti-cheat to ImGUI`
  - Files: all modified files from T1-T16
  - Pre-commit: `dotnet build src/Surfer.csproj --configuration Release`

---

## Final Verification Wave

- [x] F1. **Build Verification** — `dotnet build src/Surfer.csproj --configuration Release` exits 0
  Output: `Build [PASS/FAIL]`

- [x] F2. **Grep Audit: Green Removal** — Run: `grep -rn "#0dff00\|Color\.green" src/Patches/ src/Mono/ src/Commands/ src/Managers/ --include="*.cs" | grep -v "VentGroups\|OptionsConsole\|OptionCheckboxItem"`
  Verify: Only expected functional uses remain.
  Output: `Green refs [N remaining]`

- [x] F3. **Grep Audit: SetUIColors/AddColor** — Run: `grep -rn "SetUIColors\|\.AddColor(" src/Patches/ src/Mono/ src/Commands/ --include="*.cs"`
  Verify: 0 matches (all visual callers stripped).
  Output: `Theme calls [N remaining]`

- [x] F4. **Grep Audit: BetterSettingsTab** — Run: `grep -c "BetterSettingsTab\b" src/Patches/Gameplay/UI/Settings/GameSettingsPatch.cs`
  Verify: 0 matches (tab removed).
  Output: `Tab refs [0 expected]`

- [x] F5. **Grep Audit: AntiCheat Preserved** — Run: `grep -rn "BetterGameSettings\." src/ --include="*.cs" | wc -l`
  Verify: ≥ 17 matches (all anti-cheat reads preserved).
  Output: `AntiCheat reads [≥17 expected]`

- [x] F6. **Stale Artifact Check** — Run: `ls bin/Release/net6.0/BetterAmongUs.* 2>&1`
  Verify: "No such file or directory".
  Output: `Stale artifacts [CLEAN/EXIST]`

- [x] F7. **Output DLL Check** — Run: `ls -la bin/Release/net6.0/Surfer.dll`
  Verify: File exists and is recent.
  Output: `Output DLL [EXISTS/MISSING]`

---

## Commit Strategy

All tasks commit together as a single wave-based commit:
```
git add -A && git commit -m "feat: strip BAU visual branding, fix scroll/zoom, migrate anti-cheat to ImGUI"
```

---

## Success Criteria

### Verification Commands
```bash
# Build
dotnet build src/Surfer.csproj --configuration Release
# Expected: Build succeeded with 0 errors

# Copy to game
cp bin/Release/net6.0/Surfer.dll "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/"
# Expected: File copied

# Green removal audit
grep -rn "#0dff00\|Color\.green" src/Patches/ src/Mono/ src/Commands/ src/Managers/ --include="*.cs" | grep -v "VentGroups\|OptionsConsole\|OptionCheckboxItem"
# Expected: Empty output

# Tab removal
grep -c "BetterSettingsTab" src/Patches/Gameplay/UI/Settings/GameSettingsPatch.cs
# Expected: 0

# Stale artifacts gone
ls bin/Release/net6.0/BetterAmongUs.dll 2>&1
# Expected: No such file or directory
```

### Final Checklist
- [ ] All "Must Have" present
- [ ] All "Must NOT Have" absent
- [ ] `dotnet build` passes
- [ ] All 7 Final Verification checks pass
