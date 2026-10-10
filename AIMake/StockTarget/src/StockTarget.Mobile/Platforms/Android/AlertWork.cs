using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Work;
using StockTarget.Core;
using StockTarget.Mobile.Localization;
using StockTarget.Mobile.Services;
using Java.Util.Concurrent;

namespace StockTarget.Mobile;

/// <summary>
/// 알림 예약(WorkManager 주기 작업). 앱이 꺼져 있어도 정해진 무렵에 작업을 깨워 시세를 보고 알림을 띄운다.
/// 주기 작업은 처음 시각만 맞춰 두면 그 뒤로 하루(일주일)마다 돈다.
/// </summary>
public static class AlertScheduler
{
    private const string WeeklyWork = "alert_weekly";
    private const string DailyWork = "alert_daily";

    /// <summary>앱이 뜰 때: 켜 둔 알림이 예약돼 있게 한다(이미 있으면 시각을 건드리지 않는다).</summary>
    public static void EnsureScheduled(Context context)
    {
        Apply(context, WeeklyWork, Alerts.WeeklyEnabled, ExistingPeriodicWorkPolicy.Keep!);
        Apply(context, DailyWork, Alerts.DailyEnabled, ExistingPeriodicWorkPolicy.Keep!);
    }

    /// <summary>설정을 바꿨을 때: 켜면 다음 시각부터 새로 예약하고, 끄면 취소한다.</summary>
    public static void Reschedule(Context context)
    {
        Apply(context, WeeklyWork, Alerts.WeeklyEnabled, ExistingPeriodicWorkPolicy.CancelAndReenqueue!);
        Apply(context, DailyWork, Alerts.DailyEnabled, ExistingPeriodicWorkPolicy.CancelAndReenqueue!);
    }

    private static void Apply(Context context, string name, bool enabled, ExistingPeriodicWorkPolicy policy)
    {
        var manager = WorkManager.GetInstance(context);
        if (!enabled)
        {
            manager.CancelUniqueWork(name);
            return;
        }

        var weekly = name == WeeklyWork;
        var delay = weekly
            ? Alerts.DelayUntil(DateTime.Now, Alerts.WeeklyTime, Alerts.WeeklyDay)
            : Alerts.DelayUntil(DateTime.Now, Alerts.DailyTime);
        var worker = Java.Lang.Class.FromType(weekly ? typeof(WeeklyReminderWorker) : typeof(StageCheckWorker));
        var request = new PeriodicWorkRequest.Builder(worker, weekly ? 7 : 1, TimeUnit.Days!)
            .SetInitialDelay((long)delay.TotalMinutes, TimeUnit.Minutes!)
            .SetConstraints(new Constraints.Builder().SetRequiredNetworkType(NetworkType.Connected!).Build())
            .Build();
        manager.EnqueueUniquePeriodicWork(name, policy, (PeriodicWorkRequest)request);
    }
}

/// <summary>알림 띄우기(채널 · 눌렀을 때 앱 열기).</summary>
public static class AlertNotifier
{
    private const string ChannelId = "buy_alerts";

    public static void Show(Context context, AlertMessage message)
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(ChannelId, L.Get("Notif_ChannelName"), NotificationImportance.Default);
            ((NotificationManager)context.GetSystemService(Context.NotificationService)!).CreateNotificationChannel(channel);
        }

        var intent = new Intent(context, typeof(MainActivity)).AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
        var pending = PendingIntent.GetActivity(context, message.Id, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var notification = new NotificationCompat.Builder(context, ChannelId)
            .SetSmallIcon(Resource.Drawable.ic_notify)
            .SetContentTitle(message.Title)
            .SetContentText(message.Body)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(message.Body)) // 여러 종목이면 펼쳐서 다 보이게
            .SetContentIntent(pending)
            .SetAutoCancel(true)
            .Build();
        NotificationManagerCompat.From(context).Notify(message.Id, notification); // 권한이 없으면 조용히 무시된다
    }
}

/// <summary>백그라운드 작업 공통: 앱의 DB · 시세 서비스를 꺼내 알림 내용을 만들고 띄운다.</summary>
public abstract class AlertWorker(Context context, WorkerParameters parameters) : Worker(context, parameters)
{
    protected abstract Task<AlertMessage?> BuildAsync(StockDatabase db, StockService service);

    public override Result DoWork()
    {
        try
        {
            L.ApplySavedLanguage(); // 화면 없이 깨어날 수 있어 언어를 여기서 정한다
            var services = IPlatformApplication.Current?.Services;
            if (services?.GetService<StockDatabase>() is not { } db || services.GetService<StockService>() is not { } service)
                return Result.InvokeRetry();
            var message = Task.Run(() => BuildAsync(db, service)).GetAwaiter().GetResult();
            if (message is not null)
                AlertNotifier.Show(ApplicationContext, message);
            return Result.InvokeSuccess();
        }
        catch (Exception e)
        {
            Android.Util.Log.Warn("StockTarget", $"alert work failed: {e}");
            return Result.InvokeRetry(); // 네트워크 문제 등은 WorkManager가 조금 뒤 다시 시도한다
        }
    }
}

/// <summary>매일 아침: 매수 단계가 새로 올라간 종목 알림.</summary>
// 예약된 작업은 자바 클래스 이름으로 저장되므로 이름을 고정한다(바꾸면 이미 예약된 작업이 깨진다)
[Android.Runtime.Register("com.stocktarget.mobile.StageCheckWorker")]
public sealed class StageCheckWorker(Context context, WorkerParameters parameters) : AlertWorker(context, parameters)
{
    protected override Task<AlertMessage?> BuildAsync(StockDatabase db, StockService service) => Alerts.StageCheckAsync(db, service);
}

/// <summary>매주 일요일 저녁: 다음 주 LOC 예약할 매수 필요 종목 알림.</summary>
[Android.Runtime.Register("com.stocktarget.mobile.WeeklyReminderWorker")]
public sealed class WeeklyReminderWorker(Context context, WorkerParameters parameters) : AlertWorker(context, parameters)
{
    protected override Task<AlertMessage?> BuildAsync(StockDatabase db, StockService service) => Alerts.WeeklySummaryAsync(db, service);
}
