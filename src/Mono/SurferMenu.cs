using UnityEngine;
using System.Collections.Generic;
using Surfer.Modules;
using Surfer.Data;
using Surfer.Modules.OptionItems;
using Surfer.Patches.Gameplay.UI.Settings;

namespace Surfer;

public class SurferMenu : MonoBehaviour
{
    private Rect _windowRect;
    private bool _visible;
    public bool Visible => _visible;
    public static bool IsVisible { get; private set; }
    private Vector2 _scrollPos = Vector2.zero;
    private bool _showKeywordsWindow;
    private bool _showBanPlayerWindow, _showBanNameWindow, _showBanWordWindow;
    private Vector2 _keywordsScrollPos;
    private Vector2 _banPlayerScrollPos, _banNameScrollPos, _banWordScrollPos;
    private int _selectedTab;
    // Custom numeric input (IL2CPP-safe — TextField is stripped)
    private bool _editingNumber;
    private string _editBuffer = "";
    private int _editMin, _editMax;
    private System.Action<int>? _editCommit;
    private readonly List<(string name, System.Action draw)> _tabs = [];

    private static readonly Color32 PurpleOn = new(128, 0, 128, 255);
    private static readonly Color32 PurpleOff = new(60, 0, 60, 255);
    private static readonly Color32 BgColor = new(18, 18, 24, 240);
    private static readonly Color32 TabActive = new(40, 0, 60, 255);
    private static readonly Color32 TabInactive = new(25, 25, 35, 255);

    private void Start()
    {
        _tabs.Add(("General", DrawGeneralTab));
        _tabs.Add(("Host", DrawHostTab));
        _tabs.Add(("Anti-Cheat", DrawAntiCheatTab));
        _tabs.Add(("About", DrawAboutTab));

        _windowRect = new Rect(100, 60, 480, 500);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
            _visible = !_visible;
        IsVisible = _visible;
    }

    private void OnGUI()
    {
        // Custom numeric input via keyboard events (IL2CPP-safe)
        if (_editingNumber)
        {
            Event e = Event.current;
            if (e != null && e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    if (int.TryParse(_editBuffer, out int v))
                    {
                        v = Math.Clamp(v, _editMin, _editMax);
                        _editCommit?.Invoke(v);
                    }
                    _editingNumber = false; _editBuffer = ""; e.Use();
                }
                else if (e.keyCode == KeyCode.Escape)
                {
                    _editingNumber = false; _editBuffer = ""; e.Use();
                }
                else if (e.keyCode == KeyCode.Backspace && _editBuffer.Length > 0)
                {
                    _editBuffer = _editBuffer.Remove(_editBuffer.Length - 1); e.Use();
                }
                else if (e.keyCode >= KeyCode.Alpha0 && e.keyCode <= KeyCode.Alpha9)
                {
                    if (_editBuffer == "0") _editBuffer = "";
                    _editBuffer += (char)('0' + (e.keyCode - KeyCode.Alpha0)); e.Use();
                }
                else if (e.keyCode >= KeyCode.Keypad0 && e.keyCode <= KeyCode.Keypad9)
                {
                    if (_editBuffer == "0") _editBuffer = "";
                    _editBuffer += (char)('0' + (e.keyCode - KeyCode.Keypad0)); e.Use();
                }
            }
        }
        
        if (!_visible) return;

        // Consume scroll events over the window to prevent game zoom
        if (Event.current.type == EventType.ScrollWheel && _windowRect.Contains(Event.current.mousePosition))
            Event.current.Use();

        GUI.skin.toggle.fontSize = 14;
        GUI.skin.button.fontSize = 14;
        GUI.skin.label.fontSize = 14;
        GUI.backgroundColor = PurpleOn;
        GUI.contentColor = Color.white;

        _windowRect = GUI.Window(9969, _windowRect, (GUI.WindowFunction)DrawWindow, "  Surfer  v1.3.2");

        if (_showKeywordsWindow)
        {
            Rect kwRect = new Rect(_windowRect.x + _windowRect.width + 10, _windowRect.y, 300, 350);
            kwRect = GUI.Window(9970, kwRect, (GUI.WindowFunction)DrawKeywordsWindow, "Keywords");
        }

        if (_showBanPlayerWindow)
        {
            Rect r = new Rect(_windowRect.x + _windowRect.width + 10, _windowRect.y, 320, 350);
            r = GUI.Window(9971, r, (GUI.WindowFunction)DrawBanPlayerWindow, "Ban Player List");
        }
        if (_showBanNameWindow)
        {
            Rect r = new Rect(_windowRect.x + _windowRect.width + 10, _windowRect.y, 320, 350);
            r = GUI.Window(9972, r, (GUI.WindowFunction)DrawBanNameWindow, "Ban Name List");
        }
        if (_showBanWordWindow)
        {
            Rect r = new Rect(_windowRect.x + _windowRect.width + 10, _windowRect.y, 320, 350);
            r = GUI.Window(9973, r, (GUI.WindowFunction)DrawBanWordWindow, "Ban Word List");
        }
    }

    private void DrawWindow(int id)
    {
        GUILayout.Space(4);
        _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Width(480), GUILayout.Height(455));

        GUILayout.BeginHorizontal();
        for (int i = 0; i < _tabs.Count; i++)
        {
            GUI.backgroundColor = _selectedTab == i ? TabActive : TabInactive;
            if (GUILayout.Button(_tabs[i].name, GUILayout.Width(440f / _tabs.Count), GUILayout.Height(28)))
                _selectedTab = i;
        }
        GUI.backgroundColor = PurpleOn;
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        if (_selectedTab >= 0 && _selectedTab < _tabs.Count)
            _tabs[_selectedTab].draw();

        GUILayout.EndScrollView();

        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    private void CloseAllSubWindows()
    {
        _showKeywordsWindow = false;
        _showBanPlayerWindow = false;
        _showBanNameWindow = false;
        _showBanWordWindow = false;
    }

    // ── General Tab ──

    private void DrawGeneralTab()
    {
        GUILayout.Label("── Visual ──", SurferStyles.SectionLabel);
        DrawToggle("Chat Dark Mode", SurferPlugin.ChatDarkMode);
        DrawToggle("Disable Lobby Theme", SurferPlugin.DisableLobbyTheme);
        DrawToggle("Lobby Player Info", SurferPlugin.LobbyPlayerInfo);
        DrawToggle("Better Notifications", SurferPlugin.BetterNotifications);
        DrawToggle("Unlock FPS", SurferPlugin.UnlockFPS);
        DrawToggle("Show FPS", SurferPlugin.ShowFPS);
        DrawToggle("Force Own Language", SurferPlugin.ForceOwnLanguage);
        DrawToggle("Chat In Gameplay", SurferPlugin.ChatInGameplay);

        GUILayout.Space(8);
        GUILayout.Label("── Chat ──", SurferStyles.SectionLabel);
        DrawToggle("Longer Messages (120)", SurferPlugin.LongerMessages);
        DrawToggle("Unlock Clipboard", SurferPlugin.UnlockClipboard);
        DrawToggle("Bypass URL Block", SurferPlugin.BypassUrlBlock);
        DrawToggle("Lower Rate Limits", SurferPlugin.LowerRateLimits);

        GUILayout.Space(8);
        GUILayout.Label("── Lobby ──", SurferStyles.SectionLabel);
        DrawToggle("Copy Lobby Code on DC", SurferPlugin.CopyLobbyCode);
        DrawToggle("Private Only Lobbies", SurferPlugin.PrivateOnlyLobby);
        DrawToggle("Anti-Cheat", SurferPlugin.AntiCheat);
    }

    // ── Host Tab ──

    private void DrawHostTab()
    {
        GUILayout.Label("── Host Tools ──", SurferStyles.SectionLabel);

        DrawToggle("Anti-Bot (Keyword Kick)", SurferPlugin.AntiBot);
        if (SurferPlugin.AntiBot?.Value == true)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(24);
            if (GUILayout.Button("Keywords", GUILayout.Width(80), GUILayout.Height(20)))
            {
                CloseAllSubWindows();
                _showKeywordsWindow = !_showKeywordsWindow;
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(4);
        DrawToggle("Auto-Kick Low Level", SurferPlugin.AutoKick);
        if (SurferPlugin.AutoKick?.Value == true)
        {
            Rect row = GUILayoutUtility.GetRect(350, 24);
            Rect labelR = new Rect(row.x + 20, row.y, 70, 24);
            GUI.Label(labelR, "Threshold:", SurferStyles.AlignedLabel);
            
            int val = SurferPlugin.AutoKickThreshold?.Value ?? 0;
            Rect trackR = new Rect(labelR.xMax + 4, row.y + 7, 120, 10);
            GUI.Box(trackR, "");
            int newVal = (int)GUI.HorizontalSlider(trackR, val, 0, 100);
            
            Rect btnR = new Rect(trackR.xMax + 4, row.y, 35, 24);
            bool editingThis = _editingNumber && _editMin == 0 && _editMax == 100;
            string dsp = editingThis ? (_editBuffer + "_") : newVal.ToString();
            GUI.backgroundColor = editingThis ? new Color32(80, 80, 20, 255) : new Color32(40, 40, 60, 255);
            if (GUI.Button(btnR, dsp))
            {
                _editingNumber = true;
                _editBuffer = newVal.ToString();
                _editMin = 0; _editMax = 100;
                _editCommit = (v) => { if (SurferPlugin.AutoKickThreshold != null) SurferPlugin.AutoKickThreshold.Value = v; };
            }
            GUI.backgroundColor = PurpleOn;
            
            if (newVal != val && SurferPlugin.AutoKickThreshold != null)
                SurferPlugin.AutoKickThreshold.Value = newVal;
        }
    }

    // ── Anti-Cheat Tab ──

    private void DrawAntiCheatTab()
    {
        GUILayout.Label("── Anti-Cheat Detection ──", SurferStyles.SectionLabel);
        DrawOptionDropdown("When Cheating", BetterGameSettings.WhenCheating);

        GUILayout.Space(8);
        GUILayout.Label("── Detection Settings ──", SurferStyles.SectionLabel);
        DrawOptionToggle("Invalid Friend Code", BetterGameSettings.InvalidFriendCode);
        DrawOptionToggle("Cancel Invalid Sabotage", BetterGameSettings.CancelInvalidSabotage);
        GUILayout.BeginHorizontal();
        DrawOptionToggle("Use Ban Player List", BetterGameSettings.UseBanPlayerList);
        if (BetterGameSettings.UseBanPlayerList?.GetValue() == true)
        {
            if (GUILayout.Button("List", GUILayout.Width(50), GUILayout.Height(20)))
            {
                CloseAllSubWindows();
                _showBanPlayerWindow = !_showBanPlayerWindow;
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        DrawOptionToggle("Use Ban Name List", BetterGameSettings.UseBanNameList);
        if (BetterGameSettings.UseBanNameList?.GetValue() == true)
        {
            if (GUILayout.Button("List", GUILayout.Width(50), GUILayout.Height(20)))
            {
                CloseAllSubWindows();
                _showBanNameWindow = !_showBanNameWindow;
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        DrawOptionToggle("Use Ban Word List", BetterGameSettings.UseBanWordList);
        if (BetterGameSettings.UseBanWordList?.GetValue() == true)
        {
            if (GUILayout.Button("List", GUILayout.Width(50), GUILayout.Height(20)))
            {
                CloseAllSubWindows();
                _showBanWordWindow = !_showBanWordWindow;
            }
        }
        GUILayout.EndHorizontal();
        if (BetterGameSettings.UseBanWordList?.GetValue() == true)
            DrawOptionToggle("  Ban Words in Lobby Only", BetterGameSettings.UseBanWordListOnlyLobby);
        DrawOptionToggle("Censor Detection Reason", BetterGameSettings.CensorDetectionReason);
        DrawOptionToggle("Detect Cheat Clients", BetterGameSettings.DetectCheatClients);
        DrawOptionToggle("Detect Invalid RPCs", BetterGameSettings.DetectInvalidRPCs);

        GUILayout.Space(8);
        GUILayout.Label("── Thresholds ──", SurferStyles.SectionLabel);
        DrawOptionSlider("Min Level to Detect", BetterGameSettings.DetectedLevelAbove);
        DrawOptionSlider("Min Level to Kick", BetterGameSettings.KickLevelBelow);

        GUILayout.Space(8);
        GUILayout.Label("── Role Algorithm ──", SurferStyles.SectionLabel);
        DrawOptionDropdown("Role Randomizer", BetterGameSettings.RoleRandomizer);
        DrawOptionToggle("Desync Roles", BetterGameSettings.DesyncRoles);

        GUILayout.Space(8);
        GUILayout.Label("── Gameplay ──", SurferStyles.SectionLabel);
        DrawOptionToggle("Disable Sabotages", BetterGameSettings.DisableSabotages);
        DrawOptionToggle("Remove Pet on Death", BetterGameSettings.RemovePetOnDeath);

        if (GameState.IsHideNSeek)
        {
            GUILayout.Space(8);
            GUILayout.Label("── Hide & Seek ──", SurferStyles.SectionLabel);
            DrawOptionSlider("Impostor Count", BetterGameSettings.HideAndSeekImpNum);
            int impNum = BetterGameSettings.HideAndSeekImpNum?.GetValue() ?? 1;
            if (impNum >= 2) DrawPlayerDropdown("Impostor 2", BetterGameSettingsTemp.HideAndSeekImp2);
            if (impNum >= 3) DrawPlayerDropdown("Impostor 3", BetterGameSettingsTemp.HideAndSeekImp3);
            if (impNum >= 4) DrawPlayerDropdown("Impostor 4", BetterGameSettingsTemp.HideAndSeekImp4);
            if (impNum >= 5) DrawPlayerDropdown("Impostor 5", BetterGameSettingsTemp.HideAndSeekImp5);
        }
    }

    // ── About Tab ──

    private void DrawAboutTab()
    {
        GUILayout.Label("Surfer Mod", SurferStyles.TitleLabel);
        GUILayout.Label("v1.3.2  —  Quality-of-life mod for Among Us");
        GUILayout.Space(10);
        GUILayout.Label("Features:");
        GUILayout.Label("  • Auto-Kick low-level players (host)");
        GUILayout.Label("  • Anti-Bot keyword detection (host)");
        GUILayout.Label("  • Longer chat messages (120 chars)");
        GUILayout.Label("  • Clipboard support in chat");
        GUILayout.Label("  • Bypass URL censorship");
        GUILayout.Label("  • Copy lobby code on disconnect");
        GUILayout.Label("  • Lower chat rate limits");
        GUILayout.Space(10);
        GUILayout.Label("Based on BetterAmongUs by D1GQ");
        GUILayout.Label("Rebuilt as Surfer");
        GUILayout.Space(10);
        GUILayout.Label("Press Delete to toggle this menu");
    }

    // ── Toggle Helper ──

    private static void DrawToggle(string label, BepInEx.Configuration.ConfigEntry<bool>? config)
    {
        if (config == null) return;

        GUILayout.BeginHorizontal();
        bool val = config.Value;
        GUI.backgroundColor = val ? PurpleOn : PurpleOff;
        bool newVal = GUILayout.Toggle(val, "", GUILayout.Width(20));
        GUI.backgroundColor = PurpleOn;

        GUILayout.Space(4);
        GUI.contentColor = val ? Color.white : new Color(0.6f, 0.6f, 0.6f);
        GUILayout.Label(label);
        GUI.contentColor = Color.white;

        GUILayout.EndHorizontal();

        if (newVal != val)
            config.Value = newVal;
    }

    // ── OptionItem Helpers ──

    private static void DrawOptionToggle(string label, OptionCheckboxItem? item)
    {
        if (item == null) return;

        GUILayout.BeginHorizontal();
        bool val = item.GetValue();
        GUI.backgroundColor = val ? PurpleOn : PurpleOff;
        bool newVal = GUILayout.Toggle(val, "", GUILayout.Width(20));
        GUI.backgroundColor = PurpleOn;

        GUILayout.Space(4);
        GUI.contentColor = val ? Color.white : new Color(0.6f, 0.6f, 0.6f);
        GUILayout.Label(label);
        GUI.contentColor = Color.white;

        GUILayout.EndHorizontal();

        if (newVal != val)
            item.SetValue(newVal);
    }

    private static void DrawOptionDropdown(string label, OptionStringItem? item)
    {
        if (item == null) return;

        GUILayout.BeginHorizontal();
        GUILayout.Label(label + ":", GUILayout.Width(145));

        int currentIdx = item.GetValue();
        string displayText = item.ValueAsString();

        GUI.backgroundColor = PurpleOn;
        if (GUILayout.Button(displayText, GUILayout.MinWidth(120)))
        {
            int nextIdx = currentIdx + 1;
            item.SetValue(nextIdx);
            // Wrap around if we hit the max (SetValue clamps)
            if ((item.GetValue()) == currentIdx)
                item.SetValue(0);
        }
        GUI.backgroundColor = PurpleOn;

        GUILayout.EndHorizontal();
    }

    private void DrawOptionSlider(string label, OptionIntItem? item)
    {
        if (item == null) return;
        
        int val = item.GetValue();
        
        Rect row = GUILayoutUtility.GetRect(350, 24);
        GUI.Label(new Rect(row.x, row.y, 140, 24), label + ":", SurferStyles.AlignedLabel);
        
        Rect trackRect = new Rect(row.x + 145, row.y + 7, 140, 10);
        GUI.Box(trackRect, "");
        int newVal = (int)GUI.HorizontalSlider(trackRect, val, 0, 10000);
        
        bool editingThis = _editingNumber && _editMin == 0 && _editMax == 10000;
        string dsp = editingThis ? (_editBuffer + "_") : newVal.ToString();
        GUI.backgroundColor = editingThis ? new Color32(80, 80, 20, 255) : new Color32(40, 40, 60, 255);
        if (GUI.Button(new Rect(row.x + 289, row.y, 45, 24), dsp))
        {
            _editingNumber = true;
            _editBuffer = newVal.ToString();
            _editMin = 0; _editMax = 10000;
            _editCommit = (v) => item.SetValue(v);
        }
        GUI.backgroundColor = PurpleOn;
        
        if (newVal != val)
            item.SetValue(newVal);
    }

    private static void DrawPlayerDropdown(string label, OptionPlayerItem? item)
    {
        if (item == null) return;

        GUILayout.BeginHorizontal();
        GUILayout.Space(20);
        GUILayout.Label(label + ": [Player Selection]", GUILayout.Width(200));
        GUILayout.EndHorizontal();
    }

    private void DrawKeywordsWindow(int id)
    {
        GUILayout.Label("Anti-Bot Keywords", SurferStyles.SectionLabel);
        GUILayout.Label("Names/messages containing these trigger a kick.");
        GUILayout.Space(5);
        
        GUILayout.Label("Keywords (edit AntiBotKeywords.txt to add):");
        GUILayout.BeginHorizontal();
        GUI.backgroundColor = PurpleOn;
        if (GUILayout.Button("Add from Clipboard", GUILayout.Width(140)))
        {
            string clip = GUIUtility.systemCopyBuffer;
            if (!string.IsNullOrWhiteSpace(clip))
                BetterDataManager.AddKeyword(clip.Trim());
        }
        GUI.backgroundColor = PurpleOn;
        GUILayout.EndHorizontal();
        
        GUILayout.Space(8);
        
        var keywords = BetterDataManager.LoadKeywords();
        _keywordsScrollPos = GUILayout.BeginScrollView(_keywordsScrollPos, GUILayout.Height(220));
        foreach (string kw in keywords)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("• " + kw);
            GUI.backgroundColor = new Color32(180, 40, 40, 255);
            if (GUILayout.Button("X", GUILayout.Width(30)))
                BetterDataManager.RemoveKeyword(kw);
            GUI.backgroundColor = PurpleOn;
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
        
        GUI.DragWindow(new Rect(0, 0, 300, 20));
    }

    private void DrawBanPlayerWindow(int id)
    {
        GUILayout.Label("Ban Player List", SurferStyles.SectionLabel);
        GUILayout.Label("Friend codes or hash PUIDs");
        GUILayout.Space(5);
        
        var players = BetterDataManager.LoadBanPlayers();
        _banPlayerScrollPos = GUILayout.BeginScrollView(_banPlayerScrollPos, GUILayout.Height(260));
        foreach (string p in players)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("• " + p);
            GUI.backgroundColor = new Color32(180, 40, 40, 255);
            if (GUILayout.Button("X", GUILayout.Width(30)))
                BetterDataManager.RemoveBanPlayer(p);
            GUI.backgroundColor = PurpleOn;
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
        GUI.DragWindow(new Rect(0, 0, 320, 20));
    }

    private void DrawBanNameWindow(int id)
    {
        GUILayout.Label("Ban Name List", SurferStyles.SectionLabel);
        GUILayout.Label("Use ** for wildcards (e.g., **hacker**)");
        GUILayout.Space(5);
        
        GUILayout.Label("Names (edit BanNameList.txt to add):");
        GUILayout.BeginHorizontal();
        GUI.backgroundColor = PurpleOn;
        if (GUILayout.Button("Add from Clipboard", GUILayout.Width(140)))
        {
            string clip = GUIUtility.systemCopyBuffer;
            if (!string.IsNullOrWhiteSpace(clip))
                BetterDataManager.AddBanName(clip.Trim());
        }
        GUI.backgroundColor = PurpleOn;
        GUILayout.EndHorizontal();
        
        GUILayout.Space(8);
        var names = BetterDataManager.LoadBanNames();
        _banNameScrollPos = GUILayout.BeginScrollView(_banNameScrollPos, GUILayout.Height(220));
        foreach (string n in names)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("• " + n);
            GUI.backgroundColor = new Color32(180, 40, 40, 255);
            if (GUILayout.Button("X", GUILayout.Width(30)))
                BetterDataManager.RemoveBanName(n);
            GUI.backgroundColor = PurpleOn;
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
        GUI.DragWindow(new Rect(0, 0, 320, 20));
    }

    private void DrawBanWordWindow(int id)
    {
        GUILayout.Label("Ban Word List", SurferStyles.SectionLabel);
        GUILayout.Label("Messages containing these trigger a kick/ban");
        GUILayout.Space(5);
        
        GUILayout.Label("Words (edit BanWordList.txt to add):");
        GUILayout.BeginHorizontal();
        GUI.backgroundColor = PurpleOn;
        if (GUILayout.Button("Add from Clipboard", GUILayout.Width(140)))
        {
            string clip = GUIUtility.systemCopyBuffer;
            if (!string.IsNullOrWhiteSpace(clip))
                BetterDataManager.AddBanWord(clip.Trim());
        }
        GUI.backgroundColor = PurpleOn;
        GUILayout.EndHorizontal();
        
        GUILayout.Space(8);
        var words = BetterDataManager.LoadBanWords();
        _banWordScrollPos = GUILayout.BeginScrollView(_banWordScrollPos, GUILayout.Height(220));
        foreach (string w in words)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("• " + w);
            GUI.backgroundColor = new Color32(180, 40, 40, 255);
            if (GUILayout.Button("X", GUILayout.Width(30)))
                BetterDataManager.RemoveBanWord(w);
            GUI.backgroundColor = PurpleOn;
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
        GUI.DragWindow(new Rect(0, 0, 320, 20));
    }
}

internal static class SurferStyles
{
    public static GUIStyle SectionLabel => new(GUI.skin.label)
    {
        fontStyle = FontStyle.Bold,
        normal = { textColor = new Color(0.8f, 0.5f, 1f) }
    };

    public static GUIStyle TitleLabel => new(GUI.skin.label)
    {
        fontSize = 18,
        fontStyle = FontStyle.Bold,
        normal = { textColor = new Color(0.8f, 0.5f, 1f) }
    };

    public static GUIStyle AlignedLabel => new(GUI.skin.label)
    {
        alignment = TextAnchor.MiddleLeft
    };
}
