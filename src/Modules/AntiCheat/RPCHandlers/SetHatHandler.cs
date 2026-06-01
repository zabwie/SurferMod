using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class SetHatHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.SetHat_Deprecated;
}
