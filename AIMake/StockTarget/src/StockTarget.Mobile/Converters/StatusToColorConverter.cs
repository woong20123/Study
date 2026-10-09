using System.Globalization;
using StockTarget.Core;
using StockTarget.Mobile.Localization;

namespace StockTarget.Mobile.Converters;

/// <summary>
/// 판정 색. 한국어는 한국 증권 앱 관례대로 매수 = 빨강, 영어는 서양 관례대로 매수 = 초록이며 강도(1~3단계)가 셀수록 진하다.
/// 대기(매입 대기 포함)는 한국어 연한 하늘색(매도 쪽 = 파랑 관례), 영어 호박색(보류 · 주의)이다.
/// </summary>
public static class StatusPalette
{
    /// <summary>색 묶음. 16진 문자열은 만들 때 한 번만 Color로 바꿔 둔다(그릴 때마다 다시 해석하지 않게).</summary>
    private sealed class Set(string[] BadgeBg, string[] BadgeFg, string[] Accent, string[] Zone)
    {
        public Color[] BadgeBg { get; } = Parse(BadgeBg);
        public Color[] BadgeFg { get; } = Parse(BadgeFg);
        public Color[] Accent { get; } = Parse(Accent);
        public Color[] Zone { get; } = Parse(Zone);

        private static Color[] Parse(string[] hex) => hex.Select(h => Color.FromArgb(h)).ToArray();
    }

    // 인덱스 = 단계: 0 대기 · 1 매수 · 2 필수매수 · 3 강력매수
    private static readonly Set Korean = new(
        BadgeBg: ["#EFF6FF", "#FEE2E2", "#FCA5A5", "#DC2626"],
        BadgeFg: ["#2563EB", "#DC2626", "#991B1B", "#FFFFFF"],
        Accent: ["#2563EB", "#EF4444", "#DC2626", "#B91C1C"],
        Zone: ["#BFDBFE", "#FECACA", "#F87171", "#DC2626"]);

    private static readonly Set English = new(
        BadgeBg: ["#FFFBEB", "#DCFCE7", "#86EFAC", "#16A34A"],
        BadgeFg: ["#B45309", "#15803D", "#14532D", "#FFFFFF"],
        Accent: ["#D97706", "#22C55E", "#16A34A", "#15803D"],
        Zone: ["#FDE68A", "#BBF7D0", "#4ADE80", "#16A34A"]);

    private static Set Current => L.IsKorean ? Korean : English;

    private static readonly Color None = Color.FromArgb("#64748B");
    private static readonly Color NoneBg = Color.FromArgb("#F1F5F9");

    /// <summary>판정 배지 배경.</summary>
    public static Color BadgeBg(BuyStatus s) => s == BuyStatus.None ? NoneBg : Current.BadgeBg[s.BuyLevel()];

    /// <summary>판정 배지 글자 · 신호 막대(배지 배경 위).</summary>
    public static Color BadgeFg(BuyStatus s) => s == BuyStatus.None ? None : Current.BadgeFg[s.BuyLevel()];

    /// <summary>흰 바탕 위 강조 글자(주식 수 등) · 게이지 동그라미 테두리.</summary>
    public static Color Accent(BuyStatus s) => s == BuyStatus.None ? None : Current.Accent[s.BuyLevel()];

    /// <summary>게이지 구간 색. level = 0 대기 구간 · 1~3 매수 단계 구간.</summary>
    public static Color Zone(int level) => Current.Zone[level];
}

public class StatusToBgColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        StatusPalette.BadgeBg(value is BuyStatus s ? s : BuyStatus.None);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>배지 배경 위 글자색.</summary>
public class StatusToBadgeTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        StatusPalette.BadgeFg(value is BuyStatus s ? s : BuyStatus.None);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>흰 바탕 위 강조 글자색.</summary>
public class StatusToTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        StatusPalette.Accent(value is BuyStatus s ? s : BuyStatus.None);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>신호 막대 한 칸의 불투명도. ConverterParameter = 이 칸이 켜지는 최소 단계(1 · 2 · 3).</summary>
public class StatusToBarOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is BuyStatus s && int.TryParse(parameter as string, out var n) && s.BuyLevel() >= n ? 1.0 : 0.3;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>매수 단계(1~3)면 true — 신호 막대를 보일지.</summary>
public class StatusIsBuyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is BuyStatus s && s.BuyLevel() > 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>XAML용 고정 단계 색: <c>TextColor="{conv:StageColor Stage=MustBuy}"</c>. 언어를 바꾸면 화면을 새로 만들므로 그때 다시 정해진다.</summary>
[AcceptEmptyServiceProvider]
public sealed class StageColorExtension : IMarkupExtension<Color>
{
    public BuyStatus Stage { get; set; }

    public Color ProvideValue(IServiceProvider serviceProvider) => StatusPalette.Accent(Stage);

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
