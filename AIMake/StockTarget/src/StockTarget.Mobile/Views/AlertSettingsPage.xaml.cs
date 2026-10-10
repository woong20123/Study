using StockTarget.Mobile.ViewModels;

namespace StockTarget.Mobile.Views;

public partial class AlertSettingsPage : ContentPage
{
    public AlertSettingsPage(AlertSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
