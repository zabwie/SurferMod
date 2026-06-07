using Surfer.Helpers;
using Surfer.Managers;
using Surfer.Modules;
using Surfer.Modules.Support;
using Surfer.Mono;
using HarmonyLib;
using InnerNet;

namespace Surfer.Patches.Gameplay.Anticheat;

[HarmonyPatch]
internal class PlatformSpoofPatch
{
    [HarmonyPatch(typeof(PlatformSpecificData), nameof(PlatformSpecificData.Deserialize))]
    [HarmonyPostfix]
    internal static void PlatformSpecificData_Deserialize_Postfix(PlatformSpecificData __instance)
    {
        if (!SurferPlugin.AntiCheat.Value || SurferModdedSupportFlags.HasFlag(SurferModdedSupportFlags.Disable_Anticheat) || !GameState.IsVanillaServer) return;

        if (GameState.IsLobby)
        {
            try
            {
                LateTask.Schedule(() =>
                {
                    var player = SurferPlugin.AllPlayerControls.FirstOrDefault(pc => pc.GetClient().PlatformData == __instance);

                    if (player != null && __instance?.Platform != null)
                    {
                        if (player.IsLocalPlayer()) return;

                        if (__instance.Platform is Platforms.StandaloneWin10 or Platforms.Xbox)
                        {
                            if (__instance.XboxPlatformId.ToString().Length is < 10 or > 16)
                            {
                                player.ReportPlayer(ReportReasons.Cheating_Hacking);
                                BetterNotificationManager.NotifyCheat(player,
                                    Translator.GetString("AntiCheat.Reason.PlatformSpoofer"),
                                    Translator.GetString("AntiCheat.HasBeenDetectedWithCheat")
                                );
                                Logger_.LogCheat($"{player.BetterData().RealName} {Translator.GetString("AntiCheat.PlatformSpoofer")}: {__instance.XboxPlatformId}");
                            }
                        }

                        if (__instance.Platform is Platforms.Playstation)
                        {
                            if (__instance.PsnPlatformId.ToString().Length is < 14 or > 20)
                            {
                                player.ReportPlayer(ReportReasons.Cheating_Hacking);
                                BetterNotificationManager.NotifyCheat(player,
                                    Translator.GetString("AntiCheat.Reason.PlatformSpoofer"),
                                    Translator.GetString("AntiCheat.HasBeenDetectedWithCheat")
                                );
                                Logger_.LogCheat($"{player.BetterData().RealName} {Translator.GetString("AntiCheat.PlatformSpoofer")}: {__instance.PsnPlatformId}");
                            }
                        }

                        if (__instance.Platform is Platforms.Unknown || !Enum.IsDefined(__instance.Platform))
                        {
                            BetterNotificationManager.NotifyCheat(player,
                                Translator.GetString("AntiCheat.Reason.PlatformSpoofer"),
                                Translator.GetString("AntiCheat.HasBeenDetectedWithCheat")
                            );
                        }
                    }

                }, 3.5f, shouldLog: false);
            }
            catch (Exception ex) { Logger_.Error(ex, "PlatformSpoofPatch"); }
        }
    }
}
