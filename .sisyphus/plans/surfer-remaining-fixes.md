# Surfer Remaining Fixes: Loading Screen, Version Warnings, Welcome Messages

## TL;DR

> **Quick Summary**: Fix 3 bugs left over from the visual cleanup: (1) extra splash screen delay/animation, (2) version warning popups on every sign-in, (3) invalid welcome message translation keys.
>
> **Deliverables**:
> - `src/Patches/Client/SplashIntroPatch.cs` — `BetterIntro` sequence removed, splash proceeds directly to menu
> - `src/Patches/Client/ClientPatch.cs` — version warning popups suppressed
> - `src/Resources/Lang/en_US.json` — translation keys renamed to match code; welcome text de-branded
>
> **Estimated Effort**: Quick (4 tasks, 1 wave)

---

## Context

### Original Request
1. Fix the double/extended loading screen caused by `BetterIntro` sequence replaying the logo animation
2. Stop the unsupported version warning popups
3. Fix `<INVALID:WelcomeMsg.WelcomeToSurfer>` and `<INVALID:WelcomeMsg.SurferDescription1>` translation errors

### Research Findings
- **Double loading**: `SplashIntroPatch.StartBetterIntro()` resets timer, replays logo animation, shows black overlay, enforces 3s minimum wait — even after logo replacement was removed
- **Version warnings**: Only one version in supported array (`2025.11.18`). No disable flag. Popups fire every sign-in.
- **Invalid messages**: Code uses keys `WelcomeMsg.WelcomeToSurfer` / `WelcomeMsg.SurferDescription1` but `en_US.json` has `WelcomeMsg.WelcomeToBAU` / `WelcomeMsg.BAUDescription1`

---

## Work Objectives

### Must Have
- Splash screen proceeds normally without extra delay or animation replay
- No version warning popups
- Welcome message displays correctly (not `<INVALID:...>`)
- Welcome text uses "Surfer" not "BAU" or "Better Among Us"

### Must NOT Have
- Do NOT remove the `IsReallyDoneLoading` flag (used by `ModManagerPatch`)
- Do NOT break the skip-on-click splash functionality
- Do NOT remove the Translator or modify how it resolves keys

---

## TODOs

- [x] 1. **Fix SplashIntroPatch: remove double loading screen**

  **What to do**:
  - Edit `src/Patches/Client/SplashIntroPatch.cs`:
  - **REMOVE** the `BetterIntro` field (line 13)
  - **REMOVE** the `CanStartBetterIntro` call + `StartBetterIntro` call block (lines 49-53 in `SplashManager_Update_Prefix`). Replace with:
    ```csharp
    // After normal splash completes, proceed to main menu
    if (__instance.doneLoadingRefdata && !__instance.startedSceneLoad &&
        Time.time - __instance.startTime > __instance.minimumSecondsBeforeSceneChange)
    {
        CheckIfDone(__instance);
        return false;
    }
    ```
  - **REMOVE** `StartBetterIntro()` method entirely (lines 84-99)
  - **REMOVE** `HandleAudioDestruction()` method (lines 59-67) — only runs when `BetterIntro` is true
  - **REMOVE** call to `HandleAudioDestruction(__instance)` on line 41
  - **SIMPLIFY** `CheckIfDone()` to not check `BetterIntro`:
    ```csharp
    private static bool CheckIfDone(SplashManager __instance, bool isSkip = false)
    {
        IsReallyDoneLoading = true;
        __instance.sceneChanger.AllowFinishLoadingScene();
        __instance.startedSceneLoad = true;
        __instance.loadingObject.SetActive(true);
        return true;
    }
    ```
  - **UPDATE** `TryHandleSkipClick` — it no longer needs `BetterIntro` to be true. Change the `CheckIfDone` call to not pass `isSkip` (or update `CheckIfDone` to ignore it since we no longer have a minimum duration).
  - After cleanup, the `SplashManager_Update_Prefix` should look like:
    ```csharp
    private static bool SplashManager_Update_Prefix(SplashManager __instance)
    {
        if (Skip)
        {
            CheckIfDone(__instance);
            return false;
        }

        if (TryHandleSkipClick(__instance))
        {
            Skip = true;
            return false;
        }

        // Proceed directly when splash loading is complete
        if (__instance.doneLoadingRefdata && !__instance.startedSceneLoad &&
            Time.time - __instance.startTime > __instance.minimumSecondsBeforeSceneChange)
        {
            CheckIfDone(__instance);
            return false;
        }

        return false;
    }
    ```
  - Remove unused `using` statements (the old `Surfer.Helpers` for `Utils` was already removed in the previous plan)

  **Must NOT do**:
  - Do NOT remove `IsReallyDoneLoading` flag — it's used by `ModManagerPatch.cs`
  - Do NOT remove the skip-on-click logic (`TryHandleSkipClick`, `Skip` field)
  - Do NOT remove the black overlay hide in `SplashManager_Start_Prefix` (line 26-28)

  **Recommended Agent Profile**: `quick`
  **Parallel**: YES (Wave 1, with T2, T3, T4)

  **References**:
  - `src/Patches/Client/SplashIntroPatch.cs` — current file (118 lines)
  - `src/Patches/Client/Managers/ModManagerPatch.cs` — uses `SplashIntroPatch.IsReallyDoneLoading`

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -n "BetterIntro\|StartBetterIntro\|HandleAudioDestruction\|CanStartBetterIntro" src/Patches/Client/SplashIntroPatch.cs` returns 0 matches
  - [ ] `grep -n "IsReallyDoneLoading" src/Patches/Client/SplashIntroPatch.cs` returns ≥ 1 match (flag preserved)

  **QA Scenarios**:
  ```
  Scenario: BetterIntro logic fully removed
    Tool: Bash (grep)
    Steps:
      1. grep -n "BetterIntro\|StartBetterIntro\|HandleAudioDestruction\|CanStartBetterIntro" src/Patches/Client/SplashIntroPatch.cs
      2. Assert: 0 matches
    Expected Result: All double-loading code gone
    Evidence: .sisyphus/evidence/task-f1-gone.txt

  Scenario: IsReallyDoneLoading still set
    Tool: Bash (grep)
    Steps:
      1. grep -n "IsReallyDoneLoading = true" src/Patches/Client/SplashIntroPatch.cs
      2. Assert: 1 match inside CheckIfDone
    Expected Result: Flag still set for ModManagerPatch compatibility
    Evidence: .sisyphus/evidence/task-f1-flag.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Evidence: .sisyphus/evidence/task-f1-build.txt
  ```

  **Commit**: NO (groups with all 4 tasks)

---

- [x] 2. **Fix en_US.json: rename welcome message translation keys**

  **What to do**:
  - Edit `src/Resources/Lang/en_US.json`:
  
  **Rename keys to match code** (the code in `HudManagerPatch.cs` uses these keys):
  - Line 48: `"WelcomeMsg.WelcomeToBAU"` → `"WelcomeMsg.WelcomeToSurfer"`
  - Line 50: `"WelcomeMsg.BAUDescription1"` → `"WelcomeMsg.SurferDescription1"`
  
  **Update text content to de-brand**:
  - Line 50 value: Replace the green `#0dff00` colors and BAU branding. Current value:
    ```
    "<color=#0dff00>{0}</color> Is a mod for improving the vanilla Among Us experience with a built-in {1} and other futures, <color=#0dff00>{0}</color> is a client-sided mod so it can be used with other vanilla Among Us players."
    ```
    Replace with (removed green colors, updated placeholder context):
    ```
    "{0} is a client-sided mod for improving the vanilla Among Us experience with a built-in {1} system and other features. It can be used alongside other vanilla Among Us players."
    ```
  
  **Update branding values** referenced by the welcome message:
  - Line 5: `"Surfer": "Better Among Us"` → `"Surfer": "Surfer"` (so the welcome says "Welcome To Surfer" instead of "Welcome To Better Among Us")
  - Line 3: `"bau": "BAU"` → `"bau": "Surfer"` (used as `{0}` in the description placeholder)

  **Must NOT do**:
  - Do NOT change any other translation keys
  - Do NOT remove any other lines from the JSON
  - Do NOT change the JSON structure or formatting

  **Recommended Agent Profile**: `quick`
  **Parallel**: YES (Wave 1, with T1, T3, T4)

  **References**:
  - `src/Resources/Lang/en_US.json` — full file (152 lines)
  - `src/Patches/Gameplay/Managers/HudManagerPatch.cs` — uses `WelcomeMsg.WelcomeToSurfer` (line 13) and `WelcomeMsg.SurferDescription1` (line 15)

  **Acceptance Criteria**:
  - [ ] `grep -n "WelcomeToBAU\|BAUDescription1" src/Resources/Lang/en_US.json` returns 0 matches (old keys gone)
  - [ ] `grep -n "WelcomeToSurfer\|SurferDescription1" src/Resources/Lang/en_US.json` returns 2 matches (new keys exist)
  - [ ] `grep -n "#0dff00" src/Resources/Lang/en_US.json` returns 0 matches (green removed from welcome text)
  - [ ] `grep "Surfer.*Better Among Us" src/Resources/Lang/en_US.json` returns 0 matches (value updated)

  **QA Scenarios**:
  ```
  Scenario: Keys renamed to match code
    Tool: Bash (grep)
    Steps:
      1. grep "WelcomeMsg.WelcomeToSurfer" src/Resources/Lang/en_US.json
      2. Assert: 1 match (key exists)
      3. grep "WelcomeMsg.SurferDescription1" src/Resources/Lang/en_US.json
      4. Assert: 1 match (key exists)
    Expected Result: Both keys present under new names
    Evidence: .sisyphus/evidence/task-f2-keys.txt

  Scenario: No green color in welcome text
    Tool: Bash (grep)
    Steps:
      1. grep "#0dff00" src/Resources/Lang/en_US.json
      2. Assert: 0 matches
    Expected Result: Green color removed
    Evidence: .sisyphus/evidence/task-f2-green.txt

  Scenario: Branding updated
    Tool: Bash (grep)
    Steps:
      1. grep '"Surfer":' src/Resources/Lang/en_US.json
      2. Assert: shows "Surfer" not "Better Among Us"
    Expected Result: Brand name updated
    Evidence: .sisyphus/evidence/task-f2-brand.txt
  ```

  **Commit**: NO (groups with all 4 tasks)

---

- [x] 3. **Fix ClientPatch: suppress version warning popups**

  **What to do**:
  - Edit `src/Patches/Client/ClientPatch.cs`:
  - In `SignInStatusComponent_SetOnline_Prefix` (line 26), remove the two `Utils.ShowPopUp()` calls that show version warnings:
    - Remove lines 46-49: the "above supported versions" popup
    - Remove lines 61-64: the "below supported versions" popup
  - Keep the version comparison logic (lines 28-65 structure) but replace each `Utils.ShowPopUp(...)` call with a no-op or just remove the popup lines only.
  - Alternatively, strip the entire popup block and leave just:
    ```csharp
    // Version check — popups suppressed
    ```
    where the popups were.
  - Keep the `return true;` at line 67 so sign-in proceeds normally.

  **Must NOT do**:
  - Do NOT remove the entire prefix method — other mods might depend on it running
  - Do NOT remove the `return true;` (sign-in must proceed)
  - Do NOT change supported versions array in `SurferPlugin.cs`

  **Recommended Agent Profile**: `quick`
  **Parallel**: YES (Wave 1, with T1, T2, T4)

  **References**:
  - `src/Patches/Client/ClientPatch.cs` — `SignInStatusComponent_SetOnline_Prefix` at lines 26-68

  **Acceptance Criteria**:
  - [ ] `dotnet build src/Surfer.csproj --configuration Release` exits 0
  - [ ] `grep -n "ShowPopUp" src/Patches/Client/ClientPatch.cs` returns 0 matches (all popup calls removed from this file)
  - [ ] `grep -n "SignInStatusComponent_SetOnline_Prefix" src/Patches/Client/ClientPatch.cs` returns 1 match (method still exists)
  - [ ] `grep -n "return true" src/Patches/Client/ClientPatch.cs` returns ≥ 1 match (sign-in still proceeds)

  **QA Scenarios**:
  ```
  Scenario: No version warning popups
    Tool: Bash (grep)
    Steps:
      1. grep -n "ShowPopUp" src/Patches/Client/ClientPatch.cs
      2. Assert: 0 matches
    Expected Result: All popup calls removed
    Evidence: .sisyphus/evidence/task-f3-popups.txt

  Scenario: Method still functional
    Tool: Bash (grep)
    Steps:
      1. grep -n "return true" src/Patches/Client/ClientPatch.cs
      2. Assert: at least 1 match (sign-in proceeds)
    Expected Result: Sign-in flow intact
    Evidence: .sisyphus/evidence/task-f3-functional.txt

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. dotnet build src/Surfer.csproj --configuration Release
      2. Assert: exit code 0
    Evidence: .sisyphus/evidence/task-f3-build.txt
  ```

  **Commit**: NO (groups with all 4 tasks)

---

- [x] 4. **Build, copy to game, final verify**

  **What to do**:
  - Build: `dotnet build src/Surfer.csproj --configuration Release`
  - Verify: `ls -la src/bin/Release/net6.0/Surfer.dll`
  - Copy: `cp src/bin/Release/net6.0/Surfer.dll "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/"`
  - Run final grep audits to confirm all 3 fixes:
    1. `grep -n "BetterIntro\|StartBetterIntro\|HandleAudioDestruction" src/Patches/Client/SplashIntroPatch.cs` → 0 matches
    2. `grep -n "WelcomeToBAU\|BAUDescription1" src/Resources/Lang/en_US.json` → 0 matches
    3. `grep -n "ShowPopUp" src/Patches/Client/ClientPatch.cs` → 0 matches

  **Recommended Agent Profile**: `quick`
  **Parallel**: NO (final task — sequential after T1-T3)

  **Acceptance Criteria**:
  - [ ] Build exits 0
  - [ ] DLL copied to plugins directory
  - [ ] All 3 grep audits pass

  **QA Scenarios**:
  ```
  Scenario: Clean build + deploy
    Tool: Bash
    Steps:
      1. dotnet build src/Surfer.csproj --configuration Release
      2. cp src/bin/Release/net6.0/Surfer.dll "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/"
      3. ls -la "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/Surfer.dll"
      4. Assert: all exit 0, file exists
    Evidence: .sisyphus/evidence/task-f4-deploy.txt

  Scenario: All 3 fixes verified
    Tool: Bash
    Steps:
      1. grep -c "BetterIntro\|StartBetterIntro" src/Patches/Client/SplashIntroPatch.cs → 0
      2. grep -c "WelcomeToBAU\|BAUDescription1" src/Resources/Lang/en_US.json → 0
      3. grep -c "ShowPopUp" src/Patches/Client/ClientPatch.cs → 0
      4. Assert: all return 0
    Evidence: .sisyphus/evidence/task-f4-verify.txt
  ```

  **Commit**: YES (final)
  - Message: `fix: remove double splash, suppress version warnings, fix welcome message keys`

---

## Success Criteria

```bash
# All 3 fixes verified
grep -c "BetterIntro\|StartBetterIntro\|HandleAudioDestruction" src/Patches/Client/SplashIntroPatch.cs && echo "Fix 1: OK" || echo "Fix 1: FAIL"
grep -c "WelcomeToBAU\|BAUDescription1" src/Resources/Lang/en_US.json && echo "Fix 2: OK" || echo "Fix 2: FAIL"
grep -c "ShowPopUp" src/Patches/Client/ClientPatch.cs && echo "Fix 3: OK" || echo "Fix 3: FAIL"
# Expected: Fix 1: 0, Fix 2: 0, Fix 3: 0 (all OK)
```
