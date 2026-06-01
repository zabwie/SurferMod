# Surfer: Fix Slider Alignment + Crash Tracing

## TL;DR

> **Quick Summary**: Fix slider misalignment (root cause: `BeginVertical`+`FlexibleSpace` wrapper overriding `TextAnchor.MiddleLeft` GUIStyle). Add detailed BepInEx logging to every method in the exit/disconnect chain to pinpoint the leave-game crash.
>
> **Effort**: Quick (1 file for alignment, 2 files for logging)

---

## Issue 1: Slider Misalignment (Root Cause)

`DrawOptionSlider` lines 411-415:
```csharp
GUILayout.BeginVertical();
GUILayout.FlexibleSpace();
GUILayout.Label(label + ":", SurferStyles.AlignedLabel, GUILayout.Width(145));
GUILayout.FlexibleSpace();
GUILayout.EndVertical();
```

The `AlignedLabel` GUIStyle has `TextAnchor.MiddleLeft` — text IS vertically centered. But it's wrapped in a `BeginVertical`+`FlexibleSpace`×2 group with NO height constraint. `FlexibleSpace` consumes the available space in the layout, and the label renders at whatever position the group allocates — **the GUIStyle alignment is overridden by the group's layout engine**.

**Fix**: Remove the vertical group wrapper. Give label and slider the same explicit `GUILayout.Height(20)`. With `TextAnchor.MiddleLeft`, the text naturally centers in its 20px cell alongside the 20px slider.

---

## Issue 2: Leave Game Crash

Anonymous `NullReferenceException` entries without stack traces. Immediate crash on click, happens everywhere (lobby, game, freeplay). Only one Harmony postfix on `ExitGame` — it has null guards.

**Approach**: Add `Logger_.Log()` at entry and exit of every method in the exit chain. The LAST log before the crash identifies the failing method.

---

## TODOs

- [x] 1. **Fix slider alignment** (`src/Mono/SurferMenu.cs`)

  **Fix A — DrawOptionSlider (lines 410-415)**:
  Replace the entire BeginVertical/EndVertical block:
  ```csharp
  // REMOVE:
  GUILayout.BeginVertical();
  GUILayout.FlexibleSpace();
  GUILayout.Label(label + ":", SurferStyles.AlignedLabel, GUILayout.Width(145));
  GUILayout.FlexibleSpace();
  GUILayout.EndVertical();
  ```
  Replace with single line:
  ```csharp
  GUILayout.Label(label + ":", SurferStyles.AlignedLabel, GUILayout.Width(145), GUILayout.Height(20));
  ```
  
  **Fix B — DrawOptionSlider slider (line 418)**:
  Add matching height to slider:
  ```csharp
  // FROM:
  int newVal = (int)GUILayout.HorizontalSlider(val, 0, 10000, GUILayout.Width(100));
  // TO:
  int newVal = (int)GUILayout.HorizontalSlider(val, 0, 10000, GUILayout.Width(160), GUILayout.Height(20));
  ```
  (Wider for better coarse control — 160px gives ~62.5 levels/pixel vs 100 levels/pixel at 100px)

  **Fix C — DrawHostTab threshold label (line 213)**:
  ```csharp
  // FROM:
  GUILayout.Label("Threshold:", SurferStyles.AlignedLabel, GUILayout.Width(70));
  // TO:
  GUILayout.Label("Threshold:", SurferStyles.AlignedLabel, GUILayout.Width(70), GUILayout.Height(20));
  ```
  
  **Fix D — DrawHostTab threshold slider (line 215)**:
  ```csharp
  // FROM:
  int newVal = (int)GUILayout.HorizontalSlider(val, 0, 100, GUILayout.Width(100));
  // TO:
  int newVal = (int)GUILayout.HorizontalSlider(val, 0, 100, GUILayout.Width(140), GUILayout.Height(20));
  ```

  **Must NOT do**:
  - Do NOT change any other layout
  - Do NOT add/remove controls

  **QA**: `dotnet build` exits 0. `grep -c "GUILayout.Height(20)" src/Mono/SurferMenu.cs` returns ≥ 4.

- [x] 2. **Add crash tracing logs** (`src/Patches/Client/ClientPatch.cs` + `src/Patches/Gameplay/Player/PlayerJoinAndLeftPatch.cs`)

  **ClientPatch.cs — `AmongUsClient_ExitGame_Postfix` (line 56)**:
  Add entry log at the very top:
  ```csharp
  Logger_.Log($"[CRASH-TRACE] ExitGame postfix START — reason={reason}");
  // ... existing code ...
  Logger_.Log($"[CRASH-TRACE] ExitGame postfix END");
  ```

  **PlayerJoinAndLeftPatch.cs — `AmongUsClient_OnPlayerLeft_Postfix` (line 75)**:
  Add at top and bottom:
  ```csharp
  Logger_.Log($"[CRASH-TRACE] OnPlayerLeft START — clientId={data?.Character?.PlayerId}");
  // ... existing code ...
  Logger_.Log($"[CRASH-TRACE] OnPlayerLeft END");
  ```

  **PlayerJoinAndLeftPatch.cs — `GameData_HandleDisconnect_Prefix` (line 99)**:
  ```csharp
  Logger_.Log($"[CRASH-TRACE] HandleDisconnect START — player={player?.name}");
  // ... existing code ...
  Logger_.Log($"[CRASH-TRACE] HandleDisconnect END");
  ```

  **PlayerJoinAndLeftPatch.cs — `BetterShowNotification` (line 119)**:
  Add at top (after the null guard):
  ```csharp
  Logger_.Log($"[CRASH-TRACE] BetterShowNotification START — reason={reason}");
  // ... existing code ...
  Logger_.Log($"[CRASH-TRACE] BetterShowNotification END");
  ```

  **Must NOT do**:
  - Do NOT change any existing logic or null guards
  - Do NOT add logging inside loops (performance)

  **QA**: `grep -c "CRASH-TRACE" src/Patches/Client/ClientPatch.cs src/Patches/Gameplay/Player/PlayerJoinAndLeftPatch.cs` returns ≥ 8.

- [x] 3. **Build, deploy, test**

  - Build + copy DLL
  - Reproduce the leave-game crash
  - Read `BepInEx/LogOutput.log` — the LAST `[CRASH-TRACE]` line before the crash tells you which method failed
  - Report back which trace was last

---

## Success Criteria
```bash
# Alignment: Height(20) on all labels and sliders
grep -c "GUILayout.Height(20)" src/Mono/SurferMenu.cs
# Expected: ≥ 4

# No more BeginVertical+FlexibleSpace wrapping
grep -A 3 "DrawOptionSlider" src/Mono/SurferMenu.cs | grep -c "FlexibleSpace"
# Expected: 0

# Crash tracing in place
grep -c "CRASH-TRACE" src/Patches/Client/ClientPatch.cs src/Patches/Gameplay/Player/PlayerJoinAndLeftPatch.cs
# Expected: ≥ 8

dotnet build src/Surfer.csproj --configuration Release
# Expected: exit 0
```
