# Surfer: Leave-Game Crash Isolation Test

## TL;DR

> Move Surfer.dll out → test leave game → report result. This tells us whether Surfer or MalumMenu/LowKick is the cause.

---

## TODO

- [x] 1. **Isolate Surfer**

  ```bash
  # Move Surfer out temporarily
  mv "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/Surfer.dll" \
     "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/Surfer.dll.disabled"
  
  # Verify it's gone
  ls "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/Surfer.dll" 2>&1
  # Expected: "No such file or directory"
  ```

  **User action**: Launch Among Us, join a lobby, click "Leave Game". Report: does it crash?

  **If it STILL crashes**: Problem is MalumMenu, LowKick, or the game itself. Restore Surfer:
  ```bash
  mv "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/Surfer.dll.disabled" \
     "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/Surfer.dll"
  ```
  Then also test without MalumMenu:
  ```bash
  mv "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/MalumMenu.dll" \
     "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/MalumMenu.dll.disabled"
  mv "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/MalumLowKick.dll" \
     "/home/zabwie/.local/share/Steam/steamapps/common/Among Us/BepInEx/plugins/MalumLowKick.dll.disabled"
  ```

  **If it does NOT crash without Surfer**: Surfer is involved. Restore Surfer.dll. We'll then methodically disable individual Harmony patches to find the culprit.

- [ ] 2. **Report result**

  Tell me:
  - Did it crash without Surfer? (YES/NO)
  - If NO (Surfer is the cause): can you live with this crash for now, or do you want me to find which specific Surfer patch causes it?
  - If YES (not Surfer): test with MalumMenu disabled too — does it still crash?
