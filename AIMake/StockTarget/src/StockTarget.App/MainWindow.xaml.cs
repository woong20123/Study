using System.Windows;
using StockTarget.App.ViewModels;

namespace StockTarget.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = _vm = vm;
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
