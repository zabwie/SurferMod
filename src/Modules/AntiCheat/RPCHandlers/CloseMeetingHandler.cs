using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class CloseMeetingHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.CloseMeeting;
}
