using HarmonyLib;
using Surfer.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace Surfer.Modules;

[HarmonyPatch]
internal static class AutoKickPatch
{
    private static int _frameCounter;
    private static int _retryCount;
    private static bool _hasUnsynced;

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    private static void HudManager_Update_Postfix()
    {
        if (!SurferPlugin.AutoKick?.Value ?? true) return;
        if (SurferPlugin.VanillaMode?.Value == true) return;
        if (!AmongUsClient.Instance.AmHost) return;
        if (!GameState.IsLobby || GameState.IsInGamePlay)
        {
            _frameCounter = 0;
            _retryCount = 0;
            _hasUnsynced = false;
            return;
        }

        int threshold = SurferPlugin.AutoKickThreshold?.Value ?? 0;
        if (threshold <= 0) return;

        _frameCounter++;
        if (_frameCounter % 30 != 0) return;

        ScanPlayers(threshold);
    }

    private static void ScanPlayers(int threshold)
    {
        _hasUnsynced = false;

        foreach (var player in SurferPlugin.AllPlayerControls)
        {
            if (player == null || player.Data == null || player.Data.IsIncomplete) continue;
            if (player.IsLocalPlayer()) continue;

            uint rawLevel = player.Data.PlayerLevel;

            if (rawLevel > 100000 || rawLevel == 0)
            {
                _hasUnsynced = true;
                continue;
            }

            uint displayLevel = rawLevel + 1;
            if (displayLevel < (uint)threshold)
            {
                AmongUsClient.Instance.KickPlayer(player.Data.ClientId, SurferPlugin.AutoBan?.Value ?? false);
            }
        }

        if (_hasUnsynced && _retryCount < 4)
        {
            _retryCount++;
            LateTask.Schedule(() => ScanPlayers(threshold), 0.5f, "AutoKickRetry", false);
        }
        else if (!_hasUnsynced)
        {
            _retryCount = 0;
        }
    }
}