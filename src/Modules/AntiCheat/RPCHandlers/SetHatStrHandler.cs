using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class SetHatStrHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.SetHatStr;
}
