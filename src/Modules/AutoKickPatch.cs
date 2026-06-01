using HarmonyLib;
using System.Collections.Generic;

namespace Surfer.Modules;

/// <summary>
/// Automatically kicks players below a configurable level threshold.
/// Hooks PlayerControl.SetLevel — fires once when player level is synced on join.
/// </summary>
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetLevel))]
internal static class AutoKickPatch
{
    private static readonly List<PlayerControl> HandledLevelKicks = [];

    public static void Prefix(PlayerControl __instance, uint level)
    {
        if (!SurferPlugin.AutoKick?.Value ?? true) return;
        if (!AmongUsClient.Instance.AmHost) return;

        int threshold = SurferPlugin.AutoKickThreshold?.Value ?? 0;
        if (threshold <= 0) return;

        // Skip host — never kick yourself
        if (__instance.Data?.ClientId == AmongUsClient.Instance.HostId) return;

        // Among Us stores levels 0-indexed (displayed level 1 = stored 0)
        // So "level < threshold - 1" means "displayed level < threshold"
        if (level >= (uint)(threshold - 1)) return;

        // Glitched lobby protection — skip level 0 (displayed level 1)
        if (level == 0) return;

        // Don't kick the same player twice
        if (HandledLevelKicks.Contains(__instance)) return;

        int clientId = __instance.Data?.ClientId ?? -1;
        if (clientId < 0) return;

        // Kick (not ban) — the MalumMenu option for ban is a separate toggle
        AmongUsClient.Instance.KickPlayer(clientId, false);

        HandledLevelKicks.Add(__instance);
    }
}
