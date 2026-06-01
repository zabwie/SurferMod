using HarmonyLib;

namespace Surfer.Modules;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.Update))]
internal static class AutoKickPatch
{
    private static int _cooldownFrames;

    public static void Postfix()
    {
        if (!SurferPlugin.AutoKick?.Value ?? true) return;
        if (!AmongUsClient.Instance.AmHost) return;

        int threshold = SurferPlugin.AutoKickThreshold?.Value ?? 0;
        if (threshold <= 0) return;

        if (_cooldownFrames > 0)
        {
            _cooldownFrames--;
            return;
        }
        _cooldownFrames = 60;

        var local = PlayerControl.LocalPlayer;
        if (local == null) return;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null) continue;
            if (player.PlayerId == local.PlayerId) continue;

            var data = player.Data;
            if (data == null) continue;

            int clientId = data.ClientId;
            int displayedLevel = (int)data.PlayerLevel;

            if (displayedLevel < threshold && clientId >= 0)
            {
                AmongUsClient.Instance.KickPlayer(clientId, true);
            }
        }
    }
}

