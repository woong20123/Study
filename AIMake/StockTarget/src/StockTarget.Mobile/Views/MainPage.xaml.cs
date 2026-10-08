using StockTarget.Mobile.ViewModels;

namespace StockTarget.Mobile.Views;

public partial class MainPage : ContentPage
{
    public MainPage(MobileMainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
