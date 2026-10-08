using System.Globalization;
using StockTarget.Core;

namespace StockTarget.Mobile.Converters;

public class StatusToBgColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not BuyStatus status)
            return Color.FromArgb("#F1F5F9");

        return status switch
        {
            BuyStatus.StrongBuy => Color.FromArgb("#DBEAFE"), // 파란색 연한 배경
            BuyStatus.MustBuy => Color.FromArgb("#E0F2FE"),   // 하늘색 연한 배경
            BuyStatus.Buy => Color.FromArgb("#DCFCE7"),       // 녹색 연한 배경
            BuyStatus.Near => Color.FromArgb("#ECFCCB"),      // 연녹색 연한 배경
            _ => Color.FromArgb("#F1F5F9"),                  // 회색 배경
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class StatusToTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not BuyStatus status)
            return Color.FromArgb("#64748B");

        return status switch
        {
            BuyStatus.StrongBuy => Color.FromArgb("#1D4ED8"),
            BuyStatus.MustBuy => Color.FromArgb("#0284C7"),
            BuyStatus.Buy => Color.FromArgb("#16A34A"),
            BuyStatus.Near => Color.FromArgb("#65A30D"),
            _ => Color.FromArgb("#64748B"),
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
