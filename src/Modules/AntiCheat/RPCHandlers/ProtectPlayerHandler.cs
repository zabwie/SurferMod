using AmongUs.GameOptions;
using Surfer.Attributes;
using Surfer.Helpers;
using Surfer.Managers;
using Hazel;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class ProtectPlayerHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.ProtectPlayer;

    internal override void HandleAntiCheat(PlayerControl? sender, MessageReader reader)
    {
        if (!sender.Is(RoleTypes.GuardianAngel))
        {
            if (BetterNotificationManager.NotifyCheat(sender, GetFormatActionText()))
            {
                LogRpcInfo($"Non-GuardianAngel attempted ProtectPlayer RPC");
            }
        }
    }
}
