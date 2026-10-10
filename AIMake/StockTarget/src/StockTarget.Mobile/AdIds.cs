namespace StockTarget.Mobile;

/// <summary>
/// AdMob 광고 단위 ID. 지금은 Google 공식 테스트 ID(항상 테스트 광고가 뜬다)이므로, 출시 전에 AdMob 콘솔에서 만든 ID로 바꾼다.
/// 앱 ID는 Platforms/Android/AndroidManifest.xml의 com.google.android.gms.ads.APPLICATION_ID에도 있다(둘 다 바꾼다).
/// 개발 중에 실제 ID로 자기 광고를 누르면 계정이 정지될 수 있으니 테스트 ID나 테스트 기기로 확인한다.
/// </summary>
public static class AdIds
{
    /// <summary>AdMob 앱 ID(MainActivity에서 초기화할 때 쓴다).</summary>
    public const string AppId = "ca-app-pub-3940256099942544~3347511713";

    /// <summary>메인 화면 하단 배너.</summary>
    public const string Banner = "ca-app-pub-3940256099942544/6300978111";
}
