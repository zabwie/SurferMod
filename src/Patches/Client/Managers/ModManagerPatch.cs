using Surfer.Helpers;
using Surfer.Managers;
using Surfer.Mono;
using Surfer.Modules.AntiCheat;
using HarmonyLib;
using UnityEngine;

namespace Surfer.Patches.Client.Managers;

[HarmonyPatch]
internal static class ModManagerPatch
{
    [HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
    [HarmonyPostfix]
    private static void LateUpdate_Postfix(ModManager __instance)
    {
        // Show the mod stamp only after the splash screen has fully loaded
        if (SplashIntroPatch.IsReallyDoneLoading)
        {
            __instance.ShowModStamp();
        }

        // Update various Surfer systems each frame
        BetterAntiCheat.Update();
        LateTask.UpdateAll(Time.deltaTime);
        BetterNotificationManager.Update();
    }
}