using Surfer.Attributes;
using Hazel;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class CheckSporeHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.CheckSpore;

    internal override bool HandleAntiCheatCancel(PlayerControl? sender, MessageReader reader)
    {
        if (!GameState.IsHost)
        {
            return false;
        }

        return true;
    }
}