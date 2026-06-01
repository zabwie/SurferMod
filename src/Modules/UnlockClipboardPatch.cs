using HarmonyLib;
using UnityEngine;

namespace Surfer.Modules;

[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.Update))]
internal static class UnlockClipboardPatch
{
    public static void Postfix(TextBoxTMP __instance)
    {
        if (!SurferPlugin.UnlockClipboard?.Value ?? true) return;
        if (!__instance.hasFocus) return;
        if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)) return;

        if (Input.GetKeyDown(KeyCode.C))
        {
            GUIUtility.systemCopyBuffer = __instance.text;
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            __instance.SetText(__instance.text + GUIUtility.systemCopyBuffer);
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            GUIUtility.systemCopyBuffer = __instance.text;
            __instance.SetText("");
        }
    }
}
