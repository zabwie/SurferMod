using HarmonyLib;
using System.Collections.Generic;

namespace Surfer.Modules;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetLevel))]
internal static class AutoKickPatch
{
    private static readonly List<byte> _handled = [];

    public static void Prefix(PlayerControl __instance, uint level)
    {
        if (!SurferPlugin.AutoKick?.Value ?? true) return;
        if (!AmongUsClient.Instance.AmHost) return;
        if (GameState.IsGameStarting) return;

        int threshold = SurferPlugin.AutoKickThreshold?.Value ?? 0;
        if (threshold <= 0) return;

        if (__instance.Data?.ClientId == AmongUsClient.Instance.HostId) return;
        if (_handled.Contains(__instance.PlayerId)) return;

        // Among Us stores levels 0-indexed: displayed level 10 = stored level 9.
        // Convert to display level before comparing against threshold.
        uint displayLevel = level + 1;
        if (displayLevel < (uint)threshold)
        {
            AmongUsClient.Instance.KickPlayer(__instance.Data.ClientId, false);
            _handled.Add(__instance.PlayerId);
        }
    }
}
