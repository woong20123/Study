using StockTarget.Mobile.ViewModels;

namespace StockTarget.Mobile.Views;

public partial class TargetDetailPage : ContentPage
{
    public TargetDetailPage(MobileTargetDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
