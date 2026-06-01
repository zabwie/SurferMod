using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class SetNamePlateHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.SetNamePlate_Deprecated;
}
