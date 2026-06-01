using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class ClearVoteHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.ClearVote;
}
