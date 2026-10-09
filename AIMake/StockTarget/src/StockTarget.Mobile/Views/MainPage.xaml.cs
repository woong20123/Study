using StockTarget.Mobile.Localization;
using StockTarget.Mobile.ViewModels;

namespace StockTarget.Mobile.Views;

public partial class MainPage : ContentPage
{
    private readonly MobileMainViewModel _viewModel;

    public MainPage(MobileMainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>상세 · 편집 화면에서 돌아오면 바뀐 판정으로 필터를 다시 적용한다.</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.ApplyFilter();
    }

    /// <summary>필터 문구를 눌러도 스위치가 바뀌게 한다.</summary>
    private void OnOnlyBuyLabelTapped(object? sender, TappedEventArgs e) => _viewModel.OnlyBuy = !_viewModel.OnlyBuy;

    /// <summary>한국어 ↔ 영어 전환. XAML 문자열은 만들 때 정해지므로 셸을 새로 만들고 목록도 다시 읽는다.</summary>
    private void OnLanguageClicked(object? sender, EventArgs e)
    {
        L.ToggleLanguage();
        if (Window is { } window)
            window.Page = new AppShell();
        _ = _viewModel.LoadInitialDataAsync();
    }
}
