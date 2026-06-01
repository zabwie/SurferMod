using HarmonyLib;
using Surfer.Data;

namespace Surfer.Modules;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
internal static class AntiBotPatch
{
    public static bool Prefix(PlayerControl sourcePlayer, string chatText)
    {
        if (!SurferPlugin.AntiBot?.Value ?? true) return true;
        if (!AmongUsClient.Instance.AmHost) return true;
        if (sourcePlayer == null || PlayerControl.LocalPlayer == null) return true;

        if (sourcePlayer.PlayerId == PlayerControl.LocalPlayer.PlayerId) return true;

        var keywords = BetterDataManager.LoadKeywords();
        if (keywords.Count == 0) return true;

        string lowerName = sourcePlayer.Data?.PlayerName?.ToLowerInvariant() ?? "";
        string lowerMsg = chatText?.ToLowerInvariant() ?? "";

        foreach (var kw in keywords)
        {
            if (lowerName.Contains(kw) || lowerMsg.Contains(kw))
            {
                int clientId = sourcePlayer.Data?.ClientId ?? -1;
                if (clientId >= 0)
                {
                    AmongUsClient.Instance.KickPlayer(clientId, false);
                }
                return false;
            }
        }

        return true;
    }
}
