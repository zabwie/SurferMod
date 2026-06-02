using Surfer.Data.Json;
using Surfer.Helpers;
using System.Linq;

namespace Surfer.Data;

/// <summary>
/// Manages data storage, settings, and ban lists for the Surfer mod.
/// </summary>
internal static class BetterDataManager
{
    /// <summary>
    /// The main data file containing outfit presets and cheat detection data.
    /// </summary>
    internal static BetterDataFile BetterDataFile = new();

    /// <summary>
    /// The game settings file with compressed storage.
    /// </summary>
    internal static BetterGameSettingsFile BetterGameSettingsFile = new();

    /// <summary>
    /// Legacy data file path (BetterData.json).
    /// </summary>
    internal static string dataPath_Legacy = GetFilePath("BetterData");

    /// <summary>
    /// Current data file path (BetterDataV2.json).
    /// </summary>
    internal static string dataPath = GetFilePath("BetterDataV2");

    /// <summary>
    /// Root directory for Surfer data.
    /// </summary>
    internal static string filePathFolder = Path.Combine(SurferPlugin.GetGamePathToAmongUs(), $"Better_Data");

    /// <summary>
    /// Directory for save information files.
    /// </summary>
    internal static string filePathFolderSaveInfo = Path.Combine(filePathFolder, $"SaveInfo");

    /// <summary>
    /// Directory for settings files.
    /// </summary>
    internal static string filePathFolderSettings = Path.Combine(filePathFolder, $"Settings");

    /// <summary>
    /// Directory for game replay files.
    /// </summary>
    internal static string filePathFolderReplays = Path.Combine(filePathFolder, $"Replays");

    /// <summary>
    /// Legacy settings file path.
    /// </summary>
    internal static string SettingsFile_Legacy = Path.Combine(filePathFolderSettings, "Settings.dat");

    /// <summary>
    /// Current compressed settings file path.
    /// </summary>
    internal static string SettingsFile => Path.Combine(filePathFolderSettings, $"Preset-{SurferPlugin.SettingsPreset?.Value ?? 0}.dat");

    /// <summary>
    /// File containing banned player identifiers.
    /// </summary>
    internal static string banPlayerListFile = Path.Combine(filePathFolderSaveInfo, "BanPlayerList.txt");

    /// <summary>
    /// File containing banned player names.
    /// </summary>
    internal static string banNameListFile = Path.Combine(filePathFolderSaveInfo, "BanNameList.txt");

    /// <summary>
    /// File containing banned words/patterns.
    /// </summary>
    internal static string banWordListFile = Path.Combine(filePathFolderSaveInfo, "BanWordList.txt");

    /// <summary>
    /// File containing anti-bot keywords.
    /// </summary>
    internal static string antiBotKeywordsFile = Path.Combine(filePathFolderSaveInfo, "AntiBotKeywords.txt");

    /// <summary>
    /// File containing anti-cheat whitelist (friend codes exempt from all checks).
    /// </summary>
    internal static string antiCheatWhitelistFile = Path.Combine(filePathFolderSaveInfo, "AntiCheatWhitelist.txt");

    /// <summary>
    /// Array of file paths that should be checked during initialization.
    /// </summary>
    private static string[] Paths =>
    [
        banPlayerListFile,
        banNameListFile,
        banWordListFile,
        antiBotKeywordsFile,
        antiCheatWhitelistFile
    ];

    /// <summary>
    /// Gets a file path by name in the Among Us data directory.
    /// </summary>
    /// <param name="name">The name of the file (without extension).</param>
    /// <returns>The full file path with .json extension.</returns>
    internal static string GetFilePath(string name)
    {
        return Path.Combine(SurferPlugin.GetDataPathToAmongUs(), $"{name}.json");
    }

    /// <summary>
    /// Initializes the data manager, loading files and ensuring required directories exist.
    /// </summary>
    internal static void Initialize()
    {
        LoadLegacyData();
        BetterDataFile.Init();
        BetterGameSettingsFile.Init();

        foreach (var path in Paths)
        {
            if (!File.Exists(path))
            {
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var writer = File.CreateText(path);
                if (path == banPlayerListFile)
                {
                    writer.WriteLine("// Example ban entries (friend code and/or hashed PUID)");
                    writer.WriteLine("// Format: [FriendCode], [HashedPUID]");
                    writer.WriteLine("// Example with both:");
                    writer.WriteLine("// FriendCode#0000, abc123def456789");
                    writer.WriteLine("// Example with just friend code:");
                    writer.WriteLine("// FriendCode#0000");
                    writer.WriteLine("// Example with just hashed PUID:");
                    writer.WriteLine("// , hash123xyz789");
                }
                else if (path == banNameListFile)
                {
                    writer.WriteLine("// Example banned player names");
                    writer.WriteLine("// Each name on a new line - supports wildcards with **");
                    writer.WriteLine("// ** at start and end: contains anywhere");
                    writer.WriteLine("// ** at start only: ends with");
                    writer.WriteLine("// ** at end only: starts with");
                    writer.WriteLine("// No **: exact match (case-insensitive)");
                    writer.WriteLine("// ");
                    writer.WriteLine("// HackerPlayer123");
                    writer.WriteLine("// CheaterAccount");
                    writer.WriteLine("// **Bot**");
                    writer.WriteLine("// **Script");
                    writer.WriteLine("// Exploit**");
                    writer.WriteLine("// **Cheat**");
                }
                else if (path == banWordListFile)
                {
                    writer.WriteLine("// Example banned words/patterns");
                    writer.WriteLine("// Each word or pattern on a new line - supports wildcards with **");
                    writer.WriteLine("// ** at start and end: contains anywhere");
                    writer.WriteLine("// ** at start only: ends with");
                    writer.WriteLine("// ** at end only: starts with");
                    writer.WriteLine("// No **: exact match (case-insensitive)");
                    writer.WriteLine("// ");
                    writer.WriteLine("// hack");
                    writer.WriteLine("// cheat");
                    writer.WriteLine("// exploit");
                    writer.WriteLine("// **bot**");
                    writer.WriteLine("// **script**");
                    writer.WriteLine("// modded");
                    writer.WriteLine("// aimbot");
                    writer.WriteLine("// wallhack");
                    writer.WriteLine("// **hack**");
                    writer.WriteLine("// **cheat**");
                    writer.WriteLine("// speed**");
                }
                else if (path == antiBotKeywordsFile)
                {
                    writer.WriteLine("// Anti-Bot Keywords - one per line, case-insensitive");
                    writer.WriteLine("// Players with names or messages containing these will be kicked");
                }
                else if (path == antiCheatWhitelistFile)
                {
                    writer.WriteLine("// Anti-Cheat Whitelist - one friend code per line");
                    writer.WriteLine("// Players matching entries here are EXEMPT from all anti-cheat checks");
                }
            }
        }
    }

    /// <summary>
    /// Converts and loads legacy data if it exists.
    /// </summary>
    private static void LoadLegacyData()
    {
        if (File.Exists(SettingsFile_Legacy))
        {
            SurferPlugin.SettingsPreset.Value = 1;
            File.Move(SettingsFile_Legacy, SettingsFile);
        }
    }

    /// <summary>
    /// Saves a setting with the specified ID.
    /// </summary>
    /// <param name="id">The setting identifier.</param>
    /// <param name="input">The setting value to save.</param>
    internal static void SaveSetting(int id, object? input)
    {
        BetterGameSettingsFile.Settings[id] = input;
        BetterGameSettingsFile.Save();
    }

    /// <summary>
    /// Checks if a setting can be loaded as the specified type.
    /// </summary>
    /// <typeparam name="T">The type to check against.</typeparam>
    /// <param name="id">The setting identifier.</param>
    /// <returns>True if the setting exists and can be cast to type T, false otherwise.</returns>
    internal static bool CanLoadSetting<T>(int id)
    {
        if (BetterGameSettingsFile.Settings.TryGetValue(id, out var value))
        {
            if (value is T)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Loads a setting with the specified ID, returning a default value if not found.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="id">The setting identifier.</param>
    /// <param name="Default">The default value to return and save if the setting doesn't exist.</param>
    /// <returns>The setting value or the default value if not found.</returns>
    internal static T? LoadSetting<T>(int id, T? Default = default)
    {
        if (BetterGameSettingsFile.Settings.TryGetValue(id, out var value))
        {
            if (value is T castValue)
            {
                return castValue;
            }
        }

        SaveSetting(id, Default);
        return Default;
    }

    /// <summary>
    /// Adds a player to the ban list by friend code and/or hashed PUID.
    /// </summary>
    /// <param name="friendCode">The player's friend code (optional).</param>
    /// <param name="hashPUID">The player's hashed PUID (optional).</param>
    internal static void AddToBanList(string friendCode = "", string hashPUID = "")
    {
        if (!string.IsNullOrEmpty(friendCode) || !string.IsNullOrEmpty(hashPUID))
        {
            // Create the new string with the separator if both are not empty
            string newText = string.Empty;

            if (!string.IsNullOrEmpty(friendCode))
            {
                newText = friendCode;
            }

            if (!string.IsNullOrEmpty(hashPUID))
            {
                if (!string.IsNullOrEmpty(newText))
                {
                    newText += ", ";
                }
                newText += hashPUID.GetHashStr();
            }

            // Check if the file already contains the new entry
            if (!File.Exists(banPlayerListFile) || !File.ReadLines(banPlayerListFile).Any(line => line.Equals(newText)))
            {
                // Append the new string to the file if it's not already present
                File.AppendAllText(banPlayerListFile, Environment.NewLine + newText);
            }
        }
    }

    /// <summary>
    /// Removes a player from all cheat detection lists by identifier.
    /// </summary>
    /// <param name="identifier">The player identifier (name, hashPUID, or friend code).</param>
    /// <returns>True if the player was found and removed, false otherwise.</returns>
    internal static bool RemovePlayer(string identifier)
    {
        identifier = identifier.Replace(' ', '_');
        bool didFind = false;

        foreach (var info in BetterDataFile.CheatData.ToArray())
        {
            if (info.PlayerName.Replace(' ', '_') == identifier || info.HashPuid == identifier || info.FriendCode == identifier)
            {
                BetterDataFile.CheatData.Remove(info);
                didFind = true;
            }
        }
        foreach (var info in BetterDataFile.SickoData.ToArray())
        {
            if (info.PlayerName.Replace(' ', '_') == identifier || info.HashPuid == identifier || info.FriendCode == identifier)
            {
                BetterDataFile.SickoData.Remove(info);
                didFind = true;
            }
        }
        foreach (var info in BetterDataFile.AUMData.ToArray())
        {
            if (info.PlayerName.Replace(' ', '_') == identifier || info.HashPuid == identifier || info.FriendCode == identifier)
            {
                BetterDataFile.AUMData.Remove(info);
                didFind = true;
            }
        }
        foreach (var info in BetterDataFile.KNData.ToArray())
        {
            if (info.PlayerName.Replace(' ', '_') == identifier || info.HashPuid == identifier || info.FriendCode == identifier)
            {
                BetterDataFile.KNData.Remove(info);
                didFind = true;
            }
        }

        if (didFind)
        {
            BetterDataFile.Save();
        }

        return didFind;
    }

    /// <summary>
    /// Clears all cheat detection data from all categories.
    /// </summary>
    internal static void ClearCheatData()
    {
        BetterDataFile.CheatData.Clear();
        BetterDataFile.SickoData.Clear();
        BetterDataFile.AUMData.Clear();
        BetterDataFile.KNData.Clear();
        BetterDataFile.Save();
    }

    /// <summary>
    /// Loads the anti-bot keywords from file.
    /// </summary>
    internal static List<string> LoadKeywords()
    {
        if (!File.Exists(antiBotKeywordsFile))
            return [];
        return File.ReadLines(antiBotKeywordsFile)
            .Where(line => !string.IsNullOrWhiteSpace(line) &&
                           !line.StartsWith("//") &&
                           !line.StartsWith("#"))
            .Select(line => line.Trim().ToLower())
            .ToList();
    }

    /// <summary>
    /// Adds a keyword to the anti-bot list.
    /// </summary>
    internal static void AddKeyword(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return;
        keyword = keyword.Trim().ToLower();
        var existing = LoadKeywords();
        if (existing.Contains(keyword, StringComparer.OrdinalIgnoreCase)) return;
        File.AppendAllText(antiBotKeywordsFile, keyword + Environment.NewLine);
    }

    /// <summary>
    /// Removes a keyword from the anti-bot list.
    /// </summary>
    internal static void RemoveKeyword(string keyword)
    {
        if (!File.Exists(antiBotKeywordsFile)) return;
        var lines = File.ReadAllLines(antiBotKeywordsFile)
            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.Trim().StartsWith("//") && !l.Trim().StartsWith("#"))
            .Where(l => !l.Trim().Equals(keyword.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();
        File.WriteAllLines(antiBotKeywordsFile, lines);
    }

    // ── Anti-Cheat Whitelist ──

    /// <summary>
    /// Checks if a player's friend code is on the anti-cheat whitelist.
    /// </summary>
    internal static bool IsWhitelisted(string? friendCode)
    {
        if (string.IsNullOrEmpty(friendCode)) return false;
        if (!File.Exists(antiCheatWhitelistFile)) return false;
        return File.ReadAllLines(antiCheatWhitelistFile)
            .Any(line => line.Trim().Equals(friendCode.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    // ── Ban Name List ──

    /// <summary>
    /// Loads the ban name patterns from file.
    /// </summary>
    internal static List<string> LoadBanNames()
    {
        if (!File.Exists(banNameListFile))
            return [];
        var lines = File.ReadAllLines(banNameListFile)
            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.Trim().StartsWith("//") && !l.Trim().StartsWith("#"))
            .ToList();
        return lines;
    }

    /// <summary>
    /// Adds a name pattern to the ban name list.
    /// </summary>
    internal static void AddBanName(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return;
        pattern = pattern.Trim();
        var existing = LoadBanNames();
        if (existing.Any(e => e.Equals(pattern, StringComparison.OrdinalIgnoreCase))) return;
        File.AppendAllText(banNameListFile, pattern + Environment.NewLine);
    }

    /// <summary>
    /// Removes a name pattern from the ban name list.
    /// </summary>
    internal static void RemoveBanName(string pattern)
    {
        if (!File.Exists(banNameListFile)) return;
        var lines = File.ReadAllLines(banNameListFile)
            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.Trim().StartsWith("//") && !l.Trim().StartsWith("#"))
            .Where(l => !l.Trim().Equals(pattern.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();
        File.WriteAllLines(banNameListFile, lines);
    }

    // ── Ban Word List ──

    /// <summary>
    /// Loads the ban words from file.
    /// </summary>
    internal static List<string> LoadBanWords()
    {
        if (!File.Exists(banWordListFile))
            return [];
        var lines = File.ReadAllLines(banWordListFile)
            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.Trim().StartsWith("//") && !l.Trim().StartsWith("#"))
            .ToList();
        return lines;
    }

    /// <summary>
    /// Adds a word to the ban word list.
    /// </summary>
    internal static void AddBanWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return;
        word = word.Trim().ToLower();
        var existing = LoadBanWords();
        if (existing.Any(e => e.Equals(word, StringComparison.OrdinalIgnoreCase))) return;
        File.AppendAllText(banWordListFile, word + Environment.NewLine);
    }

    /// <summary>
    /// Removes a word from the ban word list.
    /// </summary>
    internal static void RemoveBanWord(string word)
    {
        if (!File.Exists(banWordListFile)) return;
        var lines = File.ReadAllLines(banWordListFile)
            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.Trim().StartsWith("//") && !l.Trim().StartsWith("#"))
            .Where(l => !l.Trim().Equals(word.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();
        File.WriteAllLines(banWordListFile, lines);
    }

    // ── Ban Player List ──

    /// <summary>
    /// Loads the ban player entries from file.
    /// </summary>
    internal static List<string> LoadBanPlayers()
    {
        if (!File.Exists(banPlayerListFile))
            return [];
        var lines = File.ReadAllLines(banPlayerListFile)
            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.Trim().StartsWith("//") && !l.Trim().StartsWith("#"))
            .ToList();
        return lines;
    }

    /// <summary>
    /// Removes a player entry from the ban player list.
    /// </summary>
    internal static void RemoveBanPlayer(string entry)
    {
        if (!File.Exists(banPlayerListFile)) return;
        var lines = File.ReadAllLines(banPlayerListFile)
            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.Trim().StartsWith("//") && !l.Trim().StartsWith("#"))
            .Where(l => !l.Trim().Equals(entry.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();
        File.WriteAllLines(banPlayerListFile, lines);
    }
}