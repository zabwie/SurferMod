using Surfer.Modules;
using Surfer.Modules.Support;
using HarmonyLib;
using UnityEngine;

namespace Surfer.Patches.Gameplay.Ship;

[HarmonyPatch]
internal static class VentPatch
{
    [HarmonyPatch(typeof(Vent), nameof(Vent.SetOutline))]
    [HarmonyPrefix]
    private static bool Vent_SetOutline_Prefix(Vent __instance, bool on, bool mainTarget)
    {
        if (SurferModdedSupportFlags.HasFlag(SurferModdedSupportFlags.Disable_VentColorGroups)) return true;

        Color color = VentGroups.GetVentGroupColor(__instance);
        __instance.myRend.material.SetFloat("_Outline", on ? 1f : 0f);
        __instance.myRend.material.SetColor("_OutlineColor", color);
        __instance.myRend.material.SetColor("_AddColor", mainTarget ? color : Color.clear);

        return false;
    }
}
