using AmongUs.Data.Player;
using Assets.InnerNet;
using Surfer.Modules;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Globalization;

namespace Surfer.Patches.Client;

[HarmonyPatch]
internal static class AnnouncementPanelPatch
{
    [HarmonyPatch(typeof(PlayerAnnouncementData), nameof(PlayerAnnouncementData.SetAnnouncements))]
    [HarmonyPrefix]
    private static void PlayerAnnouncementData_SetModAnnouncements_Prefix(PlayerAnnouncementData __instance, ref Il2CppReferenceArray<Announcement> aRange)
    {
        // Load and process mod news from github
        ModNews.ProcessModNewsFiles();

        // Sort mod news by date, newest first (higher number = newer date)
        ModNews.AllModNews.Sort((a1, a2) => DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)));

        // Convert all mod news to Announcement objects
        var finalAllNews = ModNews.AllModNews.Select(n => n.ToAnnouncement()).ToList();

        // Add original game announcements that aren't mod news
        foreach (var news in aRange)
        {
            if (!ModNews.AllModNews.Any(x => x.Number == news.Number))
            {
                finalAllNews.Add(news);
            }
        }

        // Sort combined list by date (newest first) using proper date parsing
        finalAllNews.Sort((a1, a2) =>
            DateTime.Compare(
                DateTime.Parse(a2.Date, null, DateTimeStyles.RoundtripKind),
                DateTime.Parse(a1.Date, null, DateTimeStyles.RoundtripKind)
            ));

        // Convert List<Announcement> back to Il2CppReferenceArray<Announcement> for game compatibility
        aRange = new Il2CppReferenceArray<Announcement>(finalAllNews.Count);
        for (int i = 0; i < finalAllNews.Count; i++)
        {
            aRange[i] = finalAllNews[i];
        }
    }


}