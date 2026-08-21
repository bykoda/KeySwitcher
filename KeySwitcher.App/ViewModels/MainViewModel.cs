using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using KeySwitcher.Models;
using KeySwitcher.Services;

namespace KeySwitcher.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly SettingsStore _store;
    private string _status = "Готов к работе";
    public AppSettings Settings { get; }
    public string Status { get => _status; set { _status = value; Changed(); } }
    public IReadOnlyList<string> WhisperModels { get; } = ["Tiny", "Base", "Small", "Medium"];
    public IReadOnlyList<string> Languages { get; } = ["auto", "ru", "en"];
    public RelayCommand SaveCommand { get; }
    public RelayCommand OpenDataCommand { get; }
    public event Action? SettingsSaved;
    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel(SettingsStore store)
    {
        _store = store; Settings = store.Load();
        SaveCommand = new RelayCommand(Save);
        OpenDataCommand = new RelayCommand(() => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", _store.DirectoryPath) { UseShellExecute = true }));
    }

    public void Save()
    {
        try
        {
            _store.Save(Settings); StartupManager.Set(Settings.StartWithWindows);
            Status = "Настройки сохранены"; SettingsSaved?.Invoke();
        }
        catch (Exception ex) { Status = "Ошибка: " + ex.Message; System.Windows.MessageBox.Show(ex.Message, "KeySwitcher", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}
