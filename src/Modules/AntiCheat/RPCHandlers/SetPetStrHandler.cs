using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class SetPetStrHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.SetPetStr;
}
