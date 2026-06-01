using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class ClimbLadderHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.ClimbLadder;
}
