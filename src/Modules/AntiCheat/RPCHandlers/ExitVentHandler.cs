using AmongUs.GameOptions;
using Surfer.Attributes;
using Surfer.Helpers;
using Surfer.Managers;
using Hazel;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class ExitVentHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.ExitVent;

    internal override void HandleAntiCheat(PlayerControl? sender, MessageReader reader)
    {
        if (!sender.IsImpostorTeam() && !sender.Is(RoleTypes.Engineer))
        {
            if (BetterNotificationManager.NotifyCheat(sender, GetFormatActionText()))
            {
                LogRpcInfo($"Non-impostor and non-engineer attempted ExitVent RPC");
            }
        }
    }
}
