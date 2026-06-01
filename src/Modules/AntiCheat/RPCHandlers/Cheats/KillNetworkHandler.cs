using Surfer.Attributes;
using Surfer.Data;
using Surfer.Enums;
using Surfer.Helpers;
using Surfer.Managers;
using Surfer.Modules.Support;
using Surfer.Mono;
using Surfer.Patches.Gameplay.UI.Settings;
using Hazel;
using InnerNet;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class KillNetworkHandler : RPCHandler
{
    internal override byte CallId => unchecked((byte)CustomRPC.KillNetwork);

    internal override void HandleCheatRpcCheck(PlayerControl? sender, MessageReader reader)
    {
        if (SurferPlugin.AntiCheat.Value && !SurferModdedSupportFlags.HasFlag(SurferModdedSupportFlags.Disable_Anticheat) && BetterGameSettings.DetectCheatClients.GetBool())
        {
            if (!BetterDataManager.BetterDataFile.KNData.Any(info => info.CheckPlayerData(sender.Data)))
            {
                sender.ReportPlayer(ReportReasons.Cheating_Hacking);
                BetterDataManager.BetterDataFile.KNData.Add(new(sender?.BetterData().RealName ?? sender.Data.PlayerName, sender.GetHashPuid(), sender.Data.FriendCode, "KillNetwork RPC"));
                BetterDataManager.BetterDataFile.Save();
                BetterNotificationManager.NotifyCheat(sender, Translator.GetString("AntiCheat.Cheat.KN"), Translator.GetString("AntiCheat.HasBeenDetectedWithCheat2"));
            }
        }
    }
}