using StockTarget.Mobile.Views;

namespace StockTarget.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(TargetDetailPage), typeof(TargetDetailPage));
        Routing.RegisterRoute(nameof(TargetEditPage), typeof(TargetEditPage));
    }
}
