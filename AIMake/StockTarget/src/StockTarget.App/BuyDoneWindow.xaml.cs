using System.Windows;
using StockTarget.App.ViewModels;

namespace StockTarget.App;

public partial class BuyDoneWindow : Window
{
    public BuyDoneWindow(BuyDoneViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
