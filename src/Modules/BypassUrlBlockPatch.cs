using HarmonyLib;
using Surfer.Helpers;
using System.Text.RegularExpressions;

namespace Surfer.Modules;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendFreeChat))]
internal static class BypassUrlBlockPatch
{
    public static bool Prefix(ChatController __instance)
    {
        if (!SurferPlugin.BypassUrlBlock?.Value ?? true) return true;

        string text = __instance.freeChatField.Text;
        string modifiedText = ReplaceDotsInUrls(text);
        if (PlayerControl.LocalPlayer != null)
        {
            PlayerControl.LocalPlayer.RpcSendChat(modifiedText);
        }
        else
        {
            Logger_.Error("BypassUrlBlock: PlayerControl.LocalPlayer is null, chat message lost", "BypassUrlBlock");
        }

        return false;
    }

    private static string ReplaceDotsInUrls(string text)
    {
        string pattern = @"(http[s]?://)?([a-zA-Z0-9-]+\.)+[a-zA-Z]{2,6}(/[\w-./?%&=]*)?|([a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+)";
        return Regex.Replace(text, pattern, match => match.Value.Replace('.', ','));
    }
}
