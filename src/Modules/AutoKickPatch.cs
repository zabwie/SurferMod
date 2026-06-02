using HarmonyLib;
using Surfer.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace Surfer.Modules;

[HarmonyPatch]
internal static class AutoKickPatch
{
    private static readonly List<byte> _handled = [];
    private static int _lastPlayerCount = -1;
    private static float _lastScanTime;

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    private static void HudManager_Update_Postfix()
    {
        if (!SurferPlugin.AutoKick?.Value ?? true) return;
        if (!AmongUsClient.Instance.AmHost) return;
        if (!GameState.IsLobby) return;

        int threshold = SurferPlugin.AutoKickThreshold?.Value ?? 0;
        if (threshold <= 0) return;

        int currentCount = SurferPlugin.AllPlayerControls.Count;

        // First frame after arming → immediate scan
        if (_lastPlayerCount < 0)
        {
            _lastPlayerCount = currentCount;
            ScanPlayers(threshold);
            return;
        }

        // Count didn't increase → no join → skip
        if (currentCount <= _lastPlayerCount)
        {
            _lastPlayerCount = currentCount;
            return;
        }

        // Debounce rapid join events
        if (Time.time - _lastScanTime < 0.3f) return;

        _lastPlayerCount = currentCount;
        _lastScanTime = Time.time;
        ScanPlayers(threshold);
    }

    private static void ScanPlayers(int threshold)
    {
        foreach (var player in SurferPlugin.AllPlayerControls)
        {
            if (player == null || player.Data == null || player.Data.IsIncomplete) continue;
            if (player.IsLocalPlayer()) continue;
            if (_handled.Contains(player.PlayerId)) continue;

            uint rawLevel = player.Data.PlayerLevel;

            // Garbage data (0xFFFFFFFF = not yet synced)
            if (rawLevel > 100000) continue;

            // Level 0 = not synced yet for remote players — give them time
            if (rawLevel == 0 && !player.IsLocalPlayer()) continue;

            uint displayLevel = rawLevel + 1;
            if (displayLevel < (uint)threshold)
            {
                AmongUsClient.Instance.KickPlayer(player.Data.ClientId, false);
                _handled.Add(player.PlayerId);
            }
        }
    }

    public static void ResetState()
    {
        _handled.Clear();
        _lastPlayerCount = -1;
    }
}
