using Surfer.Attributes;
using Surfer.Helpers;
using Surfer.Managers;
using Surfer.Mono;
using Surfer.Patches.Gameplay.UI.Settings;
using Hazel;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class SetLevelHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.SetLevel;

    internal override bool HandleAntiCheatCancel(PlayerControl? sender, MessageReader reader)
    {
        if (sender.DataIsCollected() == true && sender.BetterData().AntiCheatInfo.HasSetLevel && !GameState.IsLocalGame && GameState.IsVanillaServer)
        {
            if (BetterNotificationManager.NotifyCheat(sender, GetFormatSetText()))
            {
                LogRpcInfo($"Player attempted to set level multiple times");
            }

            return false;
        }

        sender.BetterData().AntiCheatInfo.HasSetLevel = true;

        return true;
    }

    internal override void HandleAntiCheat(PlayerControl? sender, MessageReader reader)
    {
        uint level = reader.ReadPackedUInt32() + 1;

        if (level > BetterGameSettings.DetectedLevelAbove.GetInt())
        {
            if (BetterNotificationManager.NotifyCheat(sender, string.Format(Translator.GetString("AntiCheat.InvalidLevelRPC"), level)))
            {
                LogRpcInfo($"Suspicious level set: {level} (max allowed: {BetterGameSettings.DetectedLevelAbove.GetInt()})");
            }
        }
    }
}
