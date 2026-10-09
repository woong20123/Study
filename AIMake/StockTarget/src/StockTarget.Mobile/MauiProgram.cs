using StockTarget.Core;
using StockTarget.Mobile.ViewModels;
using StockTarget.Mobile.Views;

namespace StockTarget.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();

        // SQLite DB 경로: Android 내부 앱 데이터 저장소
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "stocktarget.db");
        var db = new StockDatabase(dbPath);
        builder.Services.AddSingleton(db);

        // Core 서비스 등록
        builder.Services.AddSingleton<YahooChartClient>();
        builder.Services.AddSingleton<StockService>();
        builder.Services.AddSingleton(_ => new NaverStockNameClient());
        builder.Services.AddSingleton<StockNameService>();

        // ViewModels
        builder.Services.AddSingleton<MobileMainViewModel>();
        builder.Services.AddTransient<MobileTargetEditViewModel>();
        builder.Services.AddTransient<MobileTargetDetailViewModel>();
        builder.Services.AddTransient<MobileReservationViewModel>();

        // Pages
        builder.Services.AddTransient<MainPage>(); // 언어를 바꾸면 화면을 새로 만든다
        builder.Services.AddTransient<TargetEditPage>();
        builder.Services.AddTransient<TargetDetailPage>();
        builder.Services.AddTransient<ReservationPage>();

        return builder.Build();
    }
}
