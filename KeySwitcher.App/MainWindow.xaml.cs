using System.ComponentModel;
using System.Drawing;
using System.Windows;
using KeySwitcher.Services;
using KeySwitcher.ViewModels;
using WinForms = System.Windows.Forms;

namespace KeySwitcher;

public partial class MainWindow : Window
{
    private readonly SettingsStore _store = new();
    private readonly WhisperService _whisper;
    private readonly TranslationService _translation = new();
    private readonly MainViewModel _viewModel;
    private KeyboardEngine? _engine;
    private HotkeyManager? _hotkeys;
    private WinForms.NotifyIcon? _tray;
    private bool _reallyExit;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel(_store); DataContext = _viewModel;
        _whisper = new WhisperService(_store);
        _whisper.DownloadProgress += mb => Dispatcher.Invoke(() => _viewModel.Status = $"Загрузка модели Whisper: {mb} МБ");
        _viewModel.SettingsSaved += RegisterHotkeys;
        SourceInitialized += (_, _) => InitializeRuntime();
        StateChanged += (_, _) => { if (WindowState == WindowState.Minimized) Hide(); };
        Closing += OnClosing;
    }

    private void InitializeRuntime()
    {
        _engine = new KeyboardEngine(Dispatcher, () => _viewModel.Settings); _engine.Start();
        _hotkeys = new HotkeyManager(this); RegisterHotkeys();
        CreateTray();
    }

    private void RegisterHotkeys()
    {
        if (_hotkeys is null) return;
        try
        {
            _hotkeys.Clear();
            _hotkeys.Register(1, _viewModel.Settings.CorrectHotkey, () => _engine?.CorrectCurrentWord());
            _hotkeys.Register(2, _viewModel.Settings.VoiceHotkey, () => _ = ToggleVoiceAsync());
            _hotkeys.Register(3, _viewModel.Settings.TranslateHotkey, () => _ = TranslateAsync());
            _hotkeys.Register(4, "Ctrl+Alt+K", () => { _viewModel.Settings.AutoCorrect = !_viewModel.Settings.AutoCorrect; _viewModel.Status = _viewModel.Settings.AutoCorrect ? "Автоисправление включено" : "Автоисправление выключено"; });
        }
        catch (Exception ex) { _viewModel.Status = "Ошибка горячих клавиш: " + ex.Message; }
    }

    private async Task ToggleVoiceAsync()
    {
        try
        {
            if (!_whisper.IsRecording) { _whisper.StartRecording(); _viewModel.Status = "● Запись голоса… Нажмите хоткей ещё раз"; return; }
            _viewModel.Status = "Распознавание Whisper…";
            var text = await _whisper.StopAndTranscribeAsync(_viewModel.Settings.WhisperModel, _viewModel.Settings.VoiceLanguage);
            if (text.Length > 0) TextInjector.Text(text + " ");
            _viewModel.Status = text.Length > 0 ? "Голосовой текст вставлен" : "Речь не распознана";
        }
        catch (Exception ex) { _viewModel.Status = "Ошибка Whisper: " + ex.Message; ShowBalloon("Whisper", ex.Message); }
    }

    private async Task TranslateAsync()
    {
        try { _viewModel.Status = "Перевод…"; await _translation.TranslateSelectionAsync(_viewModel.Settings); _viewModel.Status = "Перевод вставлен"; }
        catch (Exception ex) { _viewModel.Status = "Ошибка перевода: " + ex.Message; ShowBalloon("Перевод недоступен", ex.Message); }
    }

    private void CreateTray()
    {
        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("Открыть настройки", null, (_, _) => ShowWindow());
        menu.Items.Add("Исправить последнее слово", null, (_, _) => _engine?.CorrectCurrentWord());
        menu.Items.Add("Голосовой ввод", null, (_, _) => _ = ToggleVoiceAsync());
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => { _reallyExit = true; Close(); });
        _tray = new WinForms.NotifyIcon { Text = "KeySwitcher by koda", Icon = SystemIcons.Application, Visible = true, ContextMenuStrip = menu };
        _tray.DoubleClick += (_, _) => ShowWindow();
    }

    private void ShowWindow() { Show(); WindowState = WindowState.Normal; Activate(); }
    private void ShowBalloon(string title, string text) { if (_viewModel.Settings.ShowNotifications) _tray?.ShowBalloonTip(3500, title, text, WinForms.ToolTipIcon.Info); }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_reallyExit) { e.Cancel = true; Hide(); return; }
        _viewModel.Save(); _hotkeys?.Dispose(); _engine?.Dispose(); _whisper.Dispose();
        if (_tray is not null) { _tray.Visible = false; _tray.Dispose(); }
        System.Windows.Application.Current.Shutdown();
    }
}
