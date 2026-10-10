using StockTarget.Mobile.Views;

namespace StockTarget.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(TargetDetailPage), typeof(TargetDetailPage));
        Routing.RegisterRoute(nameof(TargetEditPage), typeof(TargetEditPage));
        Routing.RegisterRoute(nameof(BuyDonePage), typeof(BuyDonePage));
        Routing.RegisterRoute(nameof(AlertSettingsPage), typeof(AlertSettingsPage));
    }
}
