using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class StartMeetingHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.StartMeeting;
}
