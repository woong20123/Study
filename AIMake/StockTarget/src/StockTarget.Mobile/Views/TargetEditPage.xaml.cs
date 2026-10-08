using StockTarget.Mobile.ViewModels;

namespace StockTarget.Mobile.Views;

public partial class TargetEditPage : ContentPage
{
    public TargetEditPage(MobileTargetEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
