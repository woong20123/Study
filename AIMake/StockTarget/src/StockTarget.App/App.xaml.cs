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

        // DB 경로: 인자로 지정하거나(--db <path>) 기본 %LOCALAPPDATA%\StockTarget\stocktarget.db
        var dbPath = StockDatabase.DefaultPath;
        var idx = Array.IndexOf(e.Args, "--db");
        if (idx >= 0 && idx + 1 < e.Args.Length)
            dbPath = e.Args[idx + 1];

        var db = new StockDatabase(dbPath);
        _client = new YahooChartClient();
        var vm = new MainViewModel(db, new StockService(db, _client));
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
