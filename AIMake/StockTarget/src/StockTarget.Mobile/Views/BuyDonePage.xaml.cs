using StockTarget.Mobile.ViewModels;

namespace StockTarget.Mobile.Views;

public partial class BuyDonePage : ContentPage
{
    private readonly MobileBuyDoneViewModel _viewModel;

    public BuyDonePage(MobileBuyDoneViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Reload();
    }
}
