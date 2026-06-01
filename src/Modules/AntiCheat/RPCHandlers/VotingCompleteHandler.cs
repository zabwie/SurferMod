using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class VotingCompleteHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.VotingComplete;
}
