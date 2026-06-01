using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class SetVisorStrHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.SetVisorStr;
}
