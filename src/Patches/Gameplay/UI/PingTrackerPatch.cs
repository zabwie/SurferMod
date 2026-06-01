using Surfer.Modules.Support;
using Surfer.Mono;
using HarmonyLib;

namespace Surfer.Patches.Gameplay.UI;

[HarmonyPatch]
internal static class PingTrackerPatch
{
    [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
    [HarmonyPrefix]
    private static bool PingTracker_Update_Prefix(PingTracker __instance)
    {
        if (SurferModdedSupportFlags.HasFlag(SurferModdedSupportFlags.Disable_BetterPingTracker)) return true;

        var betterPingTracker = __instance.gameObject.AddComponent<BetterPingTracker>();
        betterPingTracker.SetUp(__instance.text, __instance.aspectPosition);
        __instance.enabled = false;

        return false;
    }
}
