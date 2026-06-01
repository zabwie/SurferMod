using AmongUs.QuickChat;
using HarmonyLib;

namespace Surfer.Patches.Gameplay.UI.Chat;

[HarmonyPatch]
internal static class QuickChatPatch
{
    [HarmonyPatch(typeof(QuickChatMenu), nameof(QuickChatMenu.Awake))]
    [HarmonyPrefix]
    private static void QuickChatMenu_Awake_Prefix(QuickChatMenu __instance)
    {
    }

    [HarmonyPatch(typeof(QuickChatMenuLandingPage), nameof(QuickChatMenuLandingPage.Initialize))]
    [HarmonyPrefix]
    private static void QuickChatMenuLandingPage_Initialize_Prefix(QuickChatMenuLandingPage __instance)
    {
    }

    [HarmonyPatch(typeof(QuickChatMenuPhrasesPage), nameof(QuickChatMenuPhrasesPage.Awake))]
    [HarmonyPrefix]
    private static void QuickChatMenuPhrasesPage_Awake_Prefix(QuickChatMenuPhrasesPage __instance)
    {
    }
}
