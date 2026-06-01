using Surfer.Helpers;
using HarmonyLib;

namespace Surfer.Patches.Gameplay.Player;

[HarmonyPatch]
internal static class CosmeticsLayerPatch
{
    [HarmonyPatch(typeof(CosmeticsLayer), nameof(CosmeticsLayer.GetColorBlindText))]
    [HarmonyPrefix]
    private static bool CosmeticsLayer_GetColorBlindText_Prefix(CosmeticsLayer __instance, ref string __result)
    {
        // Skip processing if color ID is out of bounds (custom colors)
        if (__instance.bodyMatProperties.ColorId >= Palette.PlayerColors.Length) return true;

        string colorName = Palette.GetColorName(__instance.bodyMatProperties.ColorId);

        if (!string.IsNullOrEmpty(colorName))
        {
            __result = (char.ToUpperInvariant(colorName[0]) + colorName[1..].ToLowerInvariant())
                .ToColor(Palette.PlayerColors[__instance.bodyMatProperties.ColorId]);
        }
        else
        {
            __result = string.Empty;
        }

        return false;
    }
}