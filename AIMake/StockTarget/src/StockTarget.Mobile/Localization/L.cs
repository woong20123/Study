using System.Globalization;
using System.Resources;
using StockTarget.Core;

namespace StockTarget.Mobile.Localization;

/// <summary>
/// 화면 문자열(Resources/Strings/AppResources*.resx). 기본은 기기 언어(한국어면 한국어, 그 밖에는 영어)이고,
/// 메인 화면의 언어 버튼으로 고르면 그 언어를 저장해 다음 실행에도 쓴다.
/// </summary>
public static class L
{
    private static readonly ResourceManager Resources =
        new("StockTarget.Mobile.Resources.Strings.AppResources", typeof(L).Assembly);

    private const string LanguageKey = "app_language";

    /// <summary>앱이 뜰 때의 기기 언어 · 지역 형식. 저장한 언어가 없으면 이것을 쓴다.</summary>
    private static readonly CultureInfo DeviceUICulture = CultureInfo.CurrentUICulture;
    private static readonly CultureInfo DeviceCulture = CultureInfo.CurrentCulture;

    /// <summary>
    /// 고를 수 있는 언어(코드, 그 언어로 쓴 이름). 언어를 늘리려면 Resources/Strings/AppResources.{코드}.resx를 만들고 여기에 한 줄 더한다.
    /// 영어(en)는 기본 AppResources.resx다.
    /// </summary>
    public static readonly IReadOnlyList<(string Code, string Name)> Languages =
    [
        ("ko", "한국어"),
        ("en", "English"),
    ];

    /// <summary>저장한 언어 코드를 적용한다. 화면을 만들기 전에 부른다.</summary>
    public static void ApplySavedLanguage() => Apply(Preferences.Default.Get(LanguageKey, ""));

    /// <summary>지금 화면 언어의 코드. 목록에 없는 기기 언어면 영어(기본 리소스로 보이므로).</summary>
    public static string CurrentCode
    {
        get
        {
            var code = _ui.TwoLetterISOLanguageName;
            return Languages.Any(l => l.Code == code) ? code : "en";
        }
    }

    /// <summary>언어를 바꾸고 저장한다. 이미 만든 화면은 호출한 쪽에서 새로 만든다.</summary>
    public static void SetLanguage(string code)
    {
        Preferences.Default.Set(LanguageKey, code);
        Apply(code);
    }

    /// <summary>
    /// 고른 화면 언어 · 표기 형식. CultureInfo.CurrentUICulture만 믿지 않는다: async 메서드 안에서 바꾼 값은
    /// 그 메서드가 끝나면 원래대로 돌아가서(실행 컨텍스트), 나중에 만드는 화면이 옛 언어로 보인다.
    /// </summary>
    private static CultureInfo _ui = DeviceUICulture;
    private static CultureInfo _format = DeviceCulture;

    private static void Apply(string code)
    {
        // 숫자 · 날짜 표기도 고른 언어에 맞춘다(독일어 기기에서 영어를 골라도 1,350.25)
        var ui = code == "" ? DeviceUICulture : CultureInfo.GetCultureInfo(code);
        var format = code == "" ? DeviceCulture : ui;
        _ui = ui;
        _format = format;
        CultureInfo.CurrentUICulture = ui;
        CultureInfo.CurrentCulture = format;
        CultureInfo.DefaultThreadCurrentUICulture = ui; // await 뒤 다른 스레드에서도 같은 언어
        CultureInfo.DefaultThreadCurrentCulture = format;
    }

    /// <summary>기기 언어가 한국어인지(한글 종목명 · 한국식 표기를 쓸지).</summary>
    public static bool IsKorean => _ui.TwoLetterISOLanguageName == "ko";

    /// <summary>키에 해당하는 문자열. 없는 키면 키를 그대로 돌려줘 화면에서 바로 보이게 한다.</summary>
    public static string Get(string key) => Resources.GetString(key, _ui) ?? key;

    public static string Format(string key, params object?[] args) =>
        string.Format(_format, Get(key), args);

    /// <summary>목록 · 상세의 판정: 매수 단계면 "매수"(강도는 신호 막대 · 색으로), 매입 대기는 "대기"에 합친다.</summary>
    public static string VerdictShort(BuyStatus s) => Get(s.Verdict() switch // 합치는 규칙은 Core(BuyStatusText.Verdict) 한 곳에 둔다
    {
        BuyStatus.Buy => "Verdict_Buy",
        BuyStatus.Wait => "Verdict_Wait",
        _ => "Verdict_None",
    });

    /// <summary>매수 단계 이름(매수 1~3단계 · 대기). 금액 칸 · 예약 주문표에 쓴다.</summary>
    public static string Verdict(BuyStatus s) => Get(s switch
    {
        BuyStatus.StrongBuy => "Verdict_StrongBuy",
        BuyStatus.MustBuy => "Verdict_MustBuy",
        BuyStatus.Buy => "Verdict_Buy1",
        BuyStatus.Near => "Verdict_Wait", // 매입 대기는 대기에 합친다
        BuyStatus.Wait => "Verdict_Wait",
        _ => "Verdict_None",
    });
}

/// <summary>XAML용: <c>Text="{loc:Tr Main_AddTarget}"</c>.</summary>
[ContentProperty(nameof(Key))]
public sealed class TrExtension : IMarkupExtension<string>
{
    public string Key { get; set; } = "";

    public string ProvideValue(IServiceProvider serviceProvider) => L.Get(Key);

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
