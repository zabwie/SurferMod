using Surfer.Helpers;
using HarmonyLib;

namespace Surfer.Patches.Client;

[HarmonyPatch]
internal static class VersionShowerPatch
{
    [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
    [HarmonyPostfix]
    private static void VersionShower_Start_Postfix(VersionShower __instance)
    {
        __instance.text.text = $"Surfer {SurferPlugin.GetVersionText()} | {Utils.GetPlatformName(SurferPlugin.PlatformData.Platform)} v{SurferPlugin.AmongUsVersion}";
    }
}
