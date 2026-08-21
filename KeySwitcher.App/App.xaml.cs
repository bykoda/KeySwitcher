using System.Threading;
using System.Windows;
using System.Windows.Threading;
using KeySwitcher.Services;

namespace KeySwitcher;

public partial class App : System.Windows.Application
{
    private Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        AppLog.Write($"Запуск KeySwitcher {Environment.Version}; ОС: {Environment.OSVersion}; аргументы: {string.Join(' ', e.Args)}");
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) => AppLog.Write("Необработанная ошибка AppDomain", args.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, args) => { AppLog.Write("Необработанная ошибка Task", args.Exception); args.SetObserved(); };

        try
        {
            _mutex = new Mutex(true, @"Local\by_koda_KeySwitcher", out var first);
            if (!first)
            {
                AppLog.Write("Запуск отменён: другой экземпляр уже работает");
                System.Windows.MessageBox.Show("KeySwitcher уже запущен. Проверьте скрытые значки в области уведомлений или завершите KeySwitcher.exe в Диспетчере задач.", "KeySwitcher", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            base.OnStartup(e);
            var window = new MainWindow();
            MainWindow = window;
            window.Show();
            AppLog.Write("Главное окно успешно показано");

            if (e.Args.Contains("--smoke-test", StringComparer.OrdinalIgnoreCase))
            {
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                timer.Tick += (_, _) => { timer.Stop(); AppLog.Write("Smoke test успешно завершён"); Shutdown(0); };
                timer.Start();
            }
        }
        catch (Exception ex)
        {
            Fatal("KeySwitcher не смог запуститься", ex);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        AppLog.Write("Необработанная ошибка интерфейса", e.Exception);
        System.Windows.MessageBox.Show($"Произошла ошибка:\n\n{e.Exception.Message}\n\nЖурнал:\n{AppLog.FilePath}", "KeySwitcher", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void Fatal(string title, Exception ex)
    {
        AppLog.Write(title, ex);
        System.Windows.MessageBox.Show($"{title}:\n\n{ex.Message}\n\nЖурнал:\n{AppLog.FilePath}", "KeySwitcher", MessageBoxButton.OK, MessageBoxImage.Error);
        Shutdown(1);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppLog.Write($"Завершение KeySwitcher, код {e.ApplicationExitCode}");
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
