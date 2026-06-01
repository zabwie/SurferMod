using Surfer.Modules;
using HarmonyLib;

namespace Surfer.Patches.Gameplay.Ship;

[HarmonyPatch]
internal static class ShipStatusPatch
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Awake))]
    [HarmonyPostfix]
    private static void ShipStatus_Awake_Postfix(ShipStatus __instance)
    {
        VentGroups.CalculateAllVentGroups(__instance.AllVents);
    }
}
