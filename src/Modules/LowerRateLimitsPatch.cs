using HarmonyLib;

namespace Surfer.Modules;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
internal static class LowerRateLimitsPatch
{
    public static void Postfix(ChatController __instance)
    {
        if (!SurferPlugin.LowerRateLimits?.Value ?? true) return;

        if (__instance.timeSinceLastMessage == 0f)
        {
            __instance.timeSinceLastMessage += 1f;
        }
    }
}
