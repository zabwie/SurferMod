using HarmonyLib;

namespace Surfer.Modules;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
internal static class LongerMessagesPatch
{
    // 120 is the max safe limit — going higher triggers the anticheat RPC validation
    private const int SafeLimit = 120;

    public static void Postfix(ChatController __instance)
    {
        if (!SurferPlugin.LongerMessages?.Value ?? true) return;

        if (__instance.freeChatField?.textArea != null)
        {
            __instance.freeChatField.textArea.characterLimit = SafeLimit;
        }
    }
}
