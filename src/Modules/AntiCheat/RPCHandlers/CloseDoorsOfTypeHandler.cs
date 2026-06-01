using Surfer.Attributes;
using Surfer.Helpers;
using Surfer.Managers;
using Hazel;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class CloseDoorsOfTypeHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.CloseDoorsOfType;
    internal override void HandleAntiCheat(PlayerControl? sender, MessageReader reader)
    {
        if (!sender.IsImpostorTeam())
        {
            if (BetterNotificationManager.NotifyCheat(sender, GetFormatActionText()))
            {
                LogRpcInfo($"Non-impostor attempted CloseDoorsOfType RPC");
            }
        }
    }
}
