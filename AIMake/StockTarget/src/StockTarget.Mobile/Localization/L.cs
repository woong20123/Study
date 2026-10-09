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

    /// <summary>저장한 언어("ko" · "en")를 적용한다. 화면을 만들기 전에 부른다.</summary>
    public static void ApplySavedLanguage() => Apply(Preferences.Default.Get(LanguageKey, ""));

    /// <summary>한국어 ↔ 영어로 바꾸고 저장한다. 이미 만든 화면은 호출한 쪽에서 새로 만든다.</summary>
    public static void ToggleLanguage()
    {
        var code = IsKorean ? "en" : "ko";
        Preferences.Default.Set(LanguageKey, code);
        Apply(code);
    }

    private static void Apply(string code)
    {
        // 숫자 · 날짜 표기도 고른 언어에 맞춘다(독일어 기기에서 영어를 골라도 1,350.25)
        var ui = code == "" ? DeviceUICulture : CultureInfo.GetCultureInfo(code);
        var format = code == "" ? DeviceCulture : ui;
        CultureInfo.CurrentUICulture = ui;
        CultureInfo.CurrentCulture = format;
        CultureInfo.DefaultThreadCurrentUICulture = ui; // await 뒤 다른 스레드에서도 같은 언어
        CultureInfo.DefaultThreadCurrentCulture = format;
    }

    /// <summary>기기 언어가 한국어인지(한글 종목명 · 한국식 표기를 쓸지).</summary>
    public static bool IsKorean => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ko";

    /// <summary>키에 해당하는 문자열. 없는 키면 키를 그대로 돌려줘 화면에서 바로 보이게 한다.</summary>
    public static string Get(string key) => Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public static string Format(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), args);

    /// <summary>판정·매수 단계 이름.</summary>
    public static string Verdict(BuyStatus s) => Get(s switch
    {
        BuyStatus.StrongBuy => "Verdict_StrongBuy",
        BuyStatus.MustBuy => "Verdict_MustBuy",
        BuyStatus.Buy => "Verdict_Buy",
        BuyStatus.Near => "Verdict_Near",
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
