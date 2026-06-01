using Surfer.Attributes;
using Hazel;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class CheckColorHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.CheckColor;

    internal override bool HandleAntiCheatCancel(PlayerControl? sender, MessageReader reader)
    {
        if (!GameState.IsHost)
        {
            return false;
        }

        return true;
    }
}