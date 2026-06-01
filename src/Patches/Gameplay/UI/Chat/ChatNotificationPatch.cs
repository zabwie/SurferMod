using Surfer.Modules.Support;
using HarmonyLib;
using UnityEngine;

namespace Surfer.Patches.Gameplay.UI.Chat;

[HarmonyPatch]
internal static class ChatNotificationPatch
{
    [HarmonyPatch(typeof(ChatNotification), nameof(ChatNotification.SetUp))]
    [HarmonyPostfix]
    private static void ChatNotification_SetUp_Postfix(ChatNotification __instance)
    {
        if (SurferModdedSupportFlags.HasFlag(SurferModdedSupportFlags.Disable_BetterPingTracker)) return;

        __instance.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(-2.8f, 0.3f, -40f);
        __instance.transform.localScale = new Vector3(0.45f, 0.42f, 1f);
    }
}
