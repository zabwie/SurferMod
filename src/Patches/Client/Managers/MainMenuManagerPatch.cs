using Surfer.Managers;
using HarmonyLib;
using UnityEngine;

namespace Surfer.Patches.Client.Managers;

[HarmonyPatch]
internal static class MainMenuManagerPatch
{
    internal static PassiveButton? ButtonPrefab;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    [HarmonyPostfix]
    private static void MainMenuManager_Start_Postfix(MainMenuManager __instance)
    {
        // Create a reusable button prefab if it doesn't exist yet
        if (ButtonPrefab == null)
        {
            // Clone inventory button as template for custom UI elements
            ButtonPrefab = UnityEngine.Object.Instantiate(__instance.inventoryButton);
            ButtonPrefab.gameObject.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(ButtonPrefab);
        }

        // Notify UpdateManager that we're in the main menu
        UpdateManager.Instance?.OnMainMenu();
    }
}