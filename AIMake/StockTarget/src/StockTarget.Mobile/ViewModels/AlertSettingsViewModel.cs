using StockTarget.Core;
using StockTarget.Mobile.Localization;
using StockTarget.Mobile.Services;

namespace StockTarget.Mobile.ViewModels;

/// <summary>알림 설정: 주간 예약 알림 · 매수 단계 알림을 켜고 끈다. 켤 때 알림 권한을 요청한다.</summary>
public sealed class AlertSettingsViewModel(StockDatabase db, StockService service) : ObservableObject
{
    private string _status = "";

    public bool WeeklyEnabled
    {
        get => Alerts.WeeklyEnabled;
        set => _ = ToggleAsync(value, v => Alerts.WeeklyEnabled = v, nameof(WeeklyEnabled));
    }

    public bool DailyEnabled
    {
        get => Alerts.DailyEnabled;
        set => _ = ToggleAsync(value, v => Alerts.DailyEnabled = v, nameof(DailyEnabled));
    }

    public string Status
    {
        get => _status;
        set => Set(ref _status, value);
    }

    private AsyncCommand? _testCommand;
    public AsyncCommand TestCommand => _testCommand ??= new AsyncCommand(TestAsync);

    private async Task ToggleAsync(bool value, Action<bool> save, string property)
    {
        if (value && !await EnsurePermissionAsync())
        {
            OnPropertyChanged(property); // 권한이 없으면 스위치를 되돌린다
            return;
        }
        save(value);
        OnPropertyChanged(property);
#if ANDROID
        AlertScheduler.Reschedule(Platform.AppContext);
#endif
        Status = "";
    }

    private async Task<bool> EnsurePermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<Permissions.PostNotifications>();
        if (status == PermissionStatus.Granted)
            return true;
        Status = L.Get("Alert_PermissionDenied");
        return false;
    }

    /// <summary>지금 시세로 주간 알림을 한 번 보내 본다(알림이 실제로 뜨는지 확인용).</summary>
    private async Task TestAsync()
    {
        if (!await EnsurePermissionAsync())
            return;
        Status = L.Get("Main_Loading");
        try
        {
            var message = await Alerts.WeeklySummaryAsync(db, service, force: true);
#if ANDROID
            if (message is not null)
                AlertNotifier.Show(Platform.AppContext, message);
#endif
            Status = L.Get("Alert_TestSent");
        }
        catch (Exception e)
        {
            Status = e.Message;
        }
    }
}
