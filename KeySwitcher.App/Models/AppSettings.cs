using System.Text.Json.Serialization;

namespace KeySwitcher.Models;

public sealed class AppSettings
{
    public bool AutoCorrect { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool ShowNotifications { get; set; } = true;
    public string CorrectHotkey { get; set; } = "Ctrl+Alt+Space";
    public string VoiceHotkey { get; set; } = "Ctrl+Alt+D";
    public string TranslateHotkey { get; set; } = "Ctrl+Alt+T";
    public string WhisperModel { get; set; } = "Base";
    public string VoiceLanguage { get; set; } = "auto";
    public bool AutoPunctuation { get; set; } = true;
    public string TranslationEndpoint { get; set; } = "http://127.0.0.1:5000";
    public string TranslationApiKey { get; set; } = "";
    public string TranslationSource { get; set; } = "auto";
    public string TranslationTarget { get; set; } = "auto";
    public List<string> ExcludedApps { get; set; } = ["KeePass.exe", "mstsc.exe"];
    public List<string> IgnoredWords { get; set; } = ["вк", "тг", "vk"];
    public List<string> ForceSwapWords { get; set; } = [];
    public Dictionary<string, string> Snippets { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["сув"] = "С уважением,",
        ["br"] = "Best regards,"
    };

    [JsonIgnore] public string ExcludedAppsText { get => string.Join(Environment.NewLine, ExcludedApps); set => ExcludedApps = Split(value); }
    [JsonIgnore] public string IgnoredWordsText { get => string.Join(Environment.NewLine, IgnoredWords); set => IgnoredWords = Split(value); }
    [JsonIgnore] public string ForceSwapWordsText { get => string.Join(Environment.NewLine, ForceSwapWords); set => ForceSwapWords = Split(value); }
    [JsonIgnore] public string SnippetsText
    {
        get => string.Join(Environment.NewLine, Snippets.Select(x => $"{x.Key}={x.Value}"));
        set => Snippets = value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Split('=', 2)).Where(x => x.Length == 2 && x[0].Trim().Length > 0)
            .ToDictionary(x => x[0].Trim(), x => x[1].Trim(), StringComparer.OrdinalIgnoreCase);
    }

    private static List<string> Split(string value) => value.Split(['\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
}
