using Android.App;
using Android.Content.PM;
using Android.OS;
using Plugin.MauiMtAdmob;

namespace StockTarget.Mobile;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // AdMob 초기화(이걸 하지 않으면 배너가 광고를 요청하지 않는다)
        CrossMauiMTAdmob.Current.Init(this, AdIds.AppId);
        AlertScheduler.EnsureScheduled(this); // 켜 둔 알림이 예약돼 있게(앱을 새로 설치 · 업데이트한 뒤에도)
    }
}
