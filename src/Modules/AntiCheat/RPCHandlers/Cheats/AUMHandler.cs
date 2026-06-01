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
internal sealed class AUMHandler : RPCHandler
{
    internal override byte CallId => unchecked((byte)CustomRPC.AUM);

    internal override void HandleCheatRpcCheck(PlayerControl? sender, MessageReader reader)
    {
        if (SurferPlugin.AntiCheat.Value && !SurferModdedSupportFlags.HasFlag(SurferModdedSupportFlags.Disable_Anticheat) && BetterGameSettings.DetectCheatClients.GetBool())
        {
            var aumid = reader.ReadByte();

            if (aumid == sender.PlayerId)
            {
                if (!BetterDataManager.BetterDataFile.AUMData.Any(info => info.CheckPlayerData(sender.Data)))
                {
                    sender.ReportPlayer(ReportReasons.Cheating_Hacking);
                    BetterDataManager.BetterDataFile.AUMData.Add(new(sender?.BetterData().RealName ?? sender.Data.PlayerName, sender.GetHashPuid(), sender.Data.FriendCode, "AUM RPC"));
                    BetterDataManager.BetterDataFile.Save();
                    BetterNotificationManager.NotifyCheat(sender, Translator.GetString("AntiCheat.Cheat.AUM"), Translator.GetString("AntiCheat.HasBeenDetectedWithCheat2"));
                }
            }
        }
    }
}