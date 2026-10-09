using System.Globalization;
using System.Windows;
using System.Windows.Data;
using StockTarget.Core;

namespace StockTarget.App.ViewModels;

/// <summary>
/// 판정 칸 글자 · 신호 막대 색. 매수 1~3단계는 BuyFg · MustBuyFg · StrongBuyFg, 매입 대기 · 대기는 WaitFg(App.xaml).
/// 판정이 없으면(조회 실패 등) 값을 정하지 않아 기본 글자색을 그대로 쓴다.
/// </summary>
public sealed class StatusToVerdictBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        BuyStatus.StrongBuy => Application.Current.FindResource("StrongBuyFg"),
        BuyStatus.MustBuy => Application.Current.FindResource("MustBuyFg"),
        BuyStatus.Buy => Application.Current.FindResource("BuyFg"),
        BuyStatus.Near or BuyStatus.Wait => Application.Current.FindResource("WaitFg"),
        _ => DependencyProperty.UnsetValue,
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>매수 단계(1~3)면 신호 막대를 보인다.</summary>
public sealed class StatusToBarsVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is BuyStatus s && s.BuyLevel() > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>신호 막대 한 칸의 불투명도. ConverterParameter = 이 칸이 켜지는 최소 단계(1 · 2 · 3). 꺼진 칸은 흐리게.</summary>
public sealed class StatusToBarOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is BuyStatus s && int.TryParse(parameter as string, out var n) && s.BuyLevel() >= n ? 1.0 : 0.25;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
