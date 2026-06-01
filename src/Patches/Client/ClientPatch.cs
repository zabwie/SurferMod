using BepInEx.Unity.IL2CPP.Utils;
using Surfer.Helpers;
using Surfer.Managers;
using Surfer.Modules;
using Surfer.Patches.Gameplay.UI.Chat;
using HarmonyLib;
using InnerNet;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Surfer.Patches.Client;

[HarmonyPatch]
internal static class ClientPatch
{
    [HarmonyPatch(typeof(SignInStatusComponent), nameof(SignInStatusComponent.SetOnline))]
    [HarmonyPrefix]
    private static bool SignInStatusComponent_SetOnline_Prefix(SignInStatusComponent __instance)
    {
        // Get supported Among Us versions for Surfer
        var varSupportedVersions = SurferPlugin.SupportedAmongUsVersions;
        if (!varSupportedVersions.Any()) return true;
        Version currentVersion = new(SurferPlugin.AppVersion);
        Version firstSupportedVersion = new(varSupportedVersions.First());
        Version lastSupportedVersion = new(varSupportedVersions.Last());

        // Check if current Among Us version is higher than supported range
        if (currentVersion > firstSupportedVersion)
        {
            var verText = $"<b>{varSupportedVersions.First()}</b>";
            // Format version range if there are multiple supported versions
            if (firstSupportedVersion != lastSupportedVersion)
            {
                verText = $"<b>{varSupportedVersions.Last()}</b> - <b>{varSupportedVersions.First()}</b>";
            }

            // Warning popup removed — version check preserved
        }
        // Check if current Among Us version is lower than supported range
        else if (currentVersion < lastSupportedVersion)
        {
            var verText = $"<b>{varSupportedVersions.First()}</b>";
            if (firstSupportedVersion != lastSupportedVersion)
            {
                verText = $"<b>{varSupportedVersions.Last()}</b> - <b>{varSupportedVersions.First()}</b>";
            }

            // Warning popup removed — version check preserved
        }

        return true;
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.ExitGame))]
    [HarmonyPostfix]
    private static void AmongUsClient_ExitGame_Postfix([HarmonyArgument(0)] DisconnectReasons reason)
    {
        Logger_.Log($"[CRASH-TRACE] ExitGame postfix START — reason={reason}");
        // Hide custom loading bar when exiting game
        CustomLoadingBarManager.ToggleLoadingBar(false);
        Logger_.Log($"Client has left game for: {Enum.GetName(reason)}", "AmongUsClientPatch");
        Logger_.Log($"[CRASH-TRACE] ExitGame postfix END");
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
    [HarmonyPrefix]
    private static void AmongUsClient_OnGameEnd_Prefix()
    {
        // Preserve all player GameObjects during scene transitions
        foreach (var data in GameData.Instance.AllPlayers)
        {
            UnityEngine.Object.DontDestroyOnLoad(data.gameObject);
        }

        // Move player GameObjects to active scene after a short delay
        LateTask.Schedule(() =>
        {
            foreach (var data in GameData.Instance.AllPlayers)
            {
                SceneManager.MoveGameObjectToScene(data.gameObject, SceneManager.GetActiveScene());
            }
        }, 0.6f, shouldLog: false);
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
    [HarmonyPostfix]
    private static void AmongUsClient_CoStartGame_Postfix(AmongUsClient __instance)
    {
        // Clear in-game chat if chat feature is enabled
        if (SurferPlugin.ChatInGameplay.Value)
        {
            ChatPatch.ClearChat();
        }

        // Start custom loading sequence
        __instance.StartCoroutine(CoLoading());
    }

    private static IEnumerator CoLoading()
    {
        // Show custom loading bar
        CustomLoadingBarManager.ToggleLoadingBar(true);

        // Run different loading logic for host vs client
        if (GameState.IsHost)
        {
            yield return CoLoadingHost();
        }
        else
        {
            yield return CoLoadingClient();
        }

        // Mark loading as complete and hide bar after delay
        CustomLoadingBarManager.SetLoadingPercent(100f, "Complete");
        yield return new WaitForSeconds(0.25f);
        CustomLoadingBarManager.ToggleLoadingBar(false);
    }

    private static IEnumerator CoLoadingHost()
    {
        if (AmongUsClient.Instance == null) yield break;
        var client = AmongUsClient.Instance.GetClient(AmongUsClient.Instance.ClientId);
        var clients = AmongUsClient.Instance.allClients;

        // Continue loading while there are unassigned roles
        while (SurferPlugin.AllPlayerControls.Count > 0 && SurferPlugin.AllPlayerControls.Any(pc => !pc.roleAssigned))
        {
            // Early exit if game ended during loading
            if (!GameState.IsInGame)
            {
                CustomLoadingBarManager.ToggleLoadingBar(false);
                yield break;
            }

            string loadingText = "Initializing Game";
            float progress = 0f;

            // Progress through different loading stages
            if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
            {
                loadingText = "Starting Game Session";
                progress = 0.1f;
            }
            else if (LobbyBehaviour.Instance)
            {
                loadingText = "Loading";
                progress = 0.2f;
            }
            else if (!ShipStatus.Instance || (AmongUsClient.Instance?.ShipLoadingAsyncHandle.IsValid() == true))
            {
                bool isShipLoading = AmongUsClient.Instance?.ShipLoadingAsyncHandle.IsValid() == true;

                loadingText = isShipLoading ? "Loading Ship Async" : "Spawning Ship";
                progress = isShipLoading ? 0.3f : 0.4f;
            }
            else if (SurferPlugin.AllPlayerControls.Any(player => !player.roleAssigned))
            {
                // Calculate role assignment progress
                int totalPlayers = SurferPlugin.AllPlayerControls.Count;
                int assignedPlayers = SurferPlugin.AllPlayerControls.Count(pc => pc.roleAssigned);
                float assignmentProgress = (float)assignedPlayers / Mathf.Max(1, totalPlayers);

                loadingText = $"Assigning Roles ({assignedPlayers}/{totalPlayers})";
                progress = 0.4f + 0.3f * assignmentProgress;
            }
            else if (!client.IsReady)
            {
                // Wait for other clients to be ready
                int readyClients = clients.CountIl2Cpp(c => c?.Character != null && c.IsReady);
                int totalClients = clients.CountIl2Cpp(c => c?.Character != null);

                loadingText = $"Waiting for Players ({readyClients}/{totalClients})";
                progress = 0.8f + 0.2f * readyClients / Mathf.Max(1, totalClients);
            }

            // Update loading bar with current progress
            int percent = Mathf.RoundToInt(progress * 100f);
            CustomLoadingBarManager.SetLoadingPercent(percent, loadingText);

            yield return null;
        }
    }

    private static IEnumerator CoLoadingClient()
    {
        var client = AmongUsClient.Instance.GetClient(AmongUsClient.Instance.ClientId);
        var clients = AmongUsClient.Instance.allClients;

        // Client loading logic (similar to host but with some differences)
        while (SurferPlugin.AllPlayerControls.Count > 0 && SurferPlugin.AllPlayerControls.Any(pc => !pc.roleAssigned))
        {
            // Switch to host logic if client becomes host mid-loading
            if (GameState.IsHost)
            {
                yield return CoLoadingHost();
                yield break;
            }

            if (!GameState.IsInGame)
            {
                CustomLoadingBarManager.ToggleLoadingBar(false);
                yield break;
            }

            string loadingText = "Initializing Game";
            float progress = 0;

            if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
            {
                loadingText = "Starting Game Session";
                progress = 0.1f;
            }
            else if (LobbyBehaviour.Instance)
            {
                loadingText = "Loading";
                progress = 0.25f;
            }
            else if (!ShipStatus.Instance || (AmongUsClient.Instance?.ShipLoadingAsyncHandle.IsValid() == true))
            {
                bool isShipLoading = AmongUsClient.Instance?.ShipLoadingAsyncHandle.IsValid() == true;

                loadingText = isShipLoading ? "Loading Ship Async" : "Spawning Ship";
                progress = isShipLoading ? 0.35f : 0.4f;
            }
            else if (!client.IsReady)
            {
                loadingText = "Finalizing Connection";
                progress = 0.75f;
            }
            else
            {
                // Wait for other players (including host) to be ready
                int readyClients = clients.CountIl2Cpp(c => c?.Character != null && c.IsReady);
                int totalClients = clients.CountIl2Cpp(c => c?.Character != null);

                loadingText = $"Waiting for Players ({readyClients}/{totalClients})";
                progress = 0.85f + 0.15f * readyClients / Mathf.Max(1, totalClients);
            }

            int percent = Mathf.RoundToInt(progress * 100f);
            CustomLoadingBarManager.SetLoadingPercent(percent, loadingText);

            yield return null;
        }
    }
}