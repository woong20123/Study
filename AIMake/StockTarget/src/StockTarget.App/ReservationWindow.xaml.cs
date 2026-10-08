using System.Windows;
using StockTarget.App.ViewModels;

namespace StockTarget.App;

public partial class ReservationWindow : Window
{
    private readonly ReservationViewModel _vm;

    public ReservationWindow(ReservationViewModel vm)
    {
        InitializeComponent();
        DataContext = _vm = vm;
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
