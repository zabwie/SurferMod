using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class CancelPetHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.CancelPet;
}
