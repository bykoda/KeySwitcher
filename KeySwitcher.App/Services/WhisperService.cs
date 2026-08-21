using System.IO;
using NAudio.Wave;
using Whisper.net;
using Whisper.net.Ggml;

namespace KeySwitcher.Services;

public sealed class WhisperService : IDisposable
{
    private readonly SettingsStore _store;
    private WaveInEvent? _wave;
    private WaveFileWriter? _writer;
    private string? _recordingPath;
    public bool IsRecording { get; private set; }
    public event Action<int>? DownloadProgress;

    public WhisperService(SettingsStore store) => _store = store;

    public void StartRecording()
    {
        if (IsRecording) return;
        _recordingPath = Path.Combine(Path.GetTempPath(), $"KeySwitcher-{Guid.NewGuid():N}.wav");
        _wave = new WaveInEvent { WaveFormat = new WaveFormat(16000, 16, 1), BufferMilliseconds = 50 };
        _writer = new WaveFileWriter(_recordingPath, _wave.WaveFormat);
        _wave.DataAvailable += (_, e) => _writer?.Write(e.Buffer, 0, e.BytesRecorded);
        _wave.StartRecording(); IsRecording = true;
    }

    public async Task<string> StopAndTranscribeAsync(string modelName, string language, CancellationToken ct = default)
    {
        if (!IsRecording || _recordingPath is null) return "";
        _wave?.StopRecording(); _wave?.Dispose(); _wave = null;
        _writer?.Dispose(); _writer = null; IsRecording = false;
        var path = _recordingPath;
        try
        {
            var model = ParseModel(modelName);
            var modelPath = Path.Combine(_store.ModelsPath, $"ggml-{modelName.ToLowerInvariant()}.bin");
            await EnsureModelAsync(model, modelPath, ct);
            using var factory = WhisperFactory.FromPath(modelPath);
            using var processor = factory.CreateBuilder().WithLanguage(string.IsNullOrWhiteSpace(language) ? "auto" : language).Build();
            using var audio = File.OpenRead(path);
            var result = new System.Text.StringBuilder();
            await foreach (var segment in processor.ProcessAsync(audio, ct)) result.Append(segment.Text);
            return result.ToString().Trim();
        }
        finally { try { File.Delete(path); } catch { } }
    }

    private async Task EnsureModelAsync(GgmlType model, string path, CancellationToken ct)
    {
        if (File.Exists(path) && new FileInfo(path).Length > 1_000_000) return;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var stream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(model, cancellationToken: ct);
        await using var file = File.Create(path);
        var buffer = new byte[1024 * 1024]; long total = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0) { await file.WriteAsync(buffer.AsMemory(0, read), ct); total += read; DownloadProgress?.Invoke((int)(total / 1024 / 1024)); }
    }

    private static GgmlType ParseModel(string value) => value.ToLowerInvariant() switch { "tiny" => GgmlType.Tiny, "small" => GgmlType.Small, "medium" => GgmlType.Medium, _ => GgmlType.Base };
    public void Dispose() { _wave?.Dispose(); _writer?.Dispose(); }
}
