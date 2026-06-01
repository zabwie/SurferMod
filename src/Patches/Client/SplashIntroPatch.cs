using HarmonyLib;
using UnityEngine;

namespace Surfer.Patches.Client;

[HarmonyPatch]
internal static class SplashIntroPatch
{
    internal static bool Skip = false;
    internal static bool IsReallyDoneLoading = false;

    [HarmonyPatch(typeof(SplashManager), nameof(SplashManager.Start))]
    [HarmonyPrefix]
    private static void SplashManager_Start_Prefix(SplashManager __instance)
    {
        // Reset all flags when splash screen starts
        Skip = false;
        IsReallyDoneLoading = false;

        // Hide black overlay by moving it out of view
        __instance.logoAnimFinish.transform
            .Find("BlackOverlay").transform
            .SetLocalY(100f);
    }

    [HarmonyPatch(typeof(SplashManager), nameof(SplashManager.Update))]
    [HarmonyPrefix]
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

    private static bool TryHandleSkipClick(SplashManager __instance)
    {
        if (!Input.GetKeyDown(KeyCode.Mouse0) && !Input.GetKeyDown(KeyCode.Mouse1))
            return false;

        CheckIfDone(__instance);
        return true;
    }

    private static void CheckIfDone(SplashManager __instance)
    {
        IsReallyDoneLoading = true;

        // Allow scene transition to proceed
        __instance.sceneChanger.AllowFinishLoadingScene();
        __instance.startedSceneLoad = true;
        __instance.loadingObject.SetActive(true);
    }
}
