using StockTarget.Mobile.ViewModels;

namespace StockTarget.Mobile.Views;

public partial class ReservationPage : ContentPage
{
    public ReservationPage(MobileReservationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
