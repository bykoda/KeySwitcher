using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Windows;
using KeySwitcher.Models;
using WpfClipboard = System.Windows.Clipboard;
using WpfDataObject = System.Windows.IDataObject;

namespace KeySwitcher.Services;

public sealed class TranslationService
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(45) };

    public async Task<string> TranslateAsync(string text, AppSettings settings, CancellationToken ct = default)
    {
        var hasCyrillic = text.Any(c => c is >= '\u0400' and <= '\u04FF');
        var source = settings.TranslationSource == "auto" ? "auto" : settings.TranslationSource;
        var target = settings.TranslationTarget == "auto" ? (hasCyrillic ? "en" : "ru") : settings.TranslationTarget;
        var endpoint = settings.TranslationEndpoint.TrimEnd('/') + "/translate";
        var response = await _http.PostAsJsonAsync(endpoint, new { q = text, source, target, format = "text", api_key = settings.TranslationApiKey }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Response>(cancellationToken: ct))?.TranslatedText ?? "";
    }

    public async Task TranslateSelectionAsync(AppSettings settings, CancellationToken ct = default)
    {
        WpfDataObject? backup = null;
        try { backup = WpfClipboard.GetDataObject(); } catch { }
        try
        {
            WpfClipboard.Clear();
            TextInjector.Chord(0x11, 0x43);
            await Task.Delay(140, ct);
            if (!WpfClipboard.ContainsText()) throw new InvalidOperationException("Не удалось получить выделенный текст.");
            var original = WpfClipboard.GetText();
            var translated = await TranslateAsync(original, settings, ct);
            WpfClipboard.SetText(translated);
            TextInjector.Chord(0x11, 0x56);
            await Task.Delay(120, ct);
        }
        finally
        {
            if (backup is not null) try { WpfClipboard.SetDataObject(backup, true); } catch { }
        }
    }

    private sealed class Response { [JsonPropertyName("translatedText")] public string TranslatedText { get; set; } = ""; }
}
