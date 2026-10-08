using System.Windows;
using System.Windows.Threading;
using StockTarget.App.ViewModels;
using StockTarget.Core;

namespace StockTarget.App;

public partial class App : Application
{
    private YahooChartClient? _client;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnhandled;

        // 구동 옵션: --db <path>(기본 %LOCALAPPDATA%\StockTarget\stocktarget.db), --kiwoom off|mock|real
        StartupOptions options;
        try
        {
            options = StartupOptions.Parse(e.Args);
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show($"{ex.Message}\n\n사용법: {StartupOptions.Usage}", "구동 옵션 오류",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        var db = new StockDatabase(options.DbPath ?? StockDatabase.DefaultPath);
        _client = new YahooChartClient();
        var vm = new MainViewModel(db, new StockService(db, _client), options.Kiwoom);
        MainWindow = new MainWindow(vm);
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _client?.Dispose();
        base.OnExit(e);
    }

    private static void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
