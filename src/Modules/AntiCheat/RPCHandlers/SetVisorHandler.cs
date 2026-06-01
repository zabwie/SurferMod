using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class SetVisorHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.SetVisor_Deprecated;
}
