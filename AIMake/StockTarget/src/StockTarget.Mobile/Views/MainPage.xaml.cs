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

    private void OnAdLoaded(object? sender, EventArgs e) => System.Diagnostics.Debug.WriteLine("[AdMob] banner loaded");

    /// <summary>광고를 못 받으면(오프라인 · 광고 없음) 사유만 남긴다. 배너는 비어 있으면 높이가 0이라 화면을 차지하지 않는다.</summary>
    private void OnAdFailed(object? sender, Plugin.MauiMtAdmob.Extra.MTEventArgs e) =>
        System.Diagnostics.Debug.WriteLine("[AdMob] banner failed: " +
            string.Join(", ", e.GetType().GetProperties().Select(p => $"{p.Name}={p.GetValue(e)}")));

    /// <summary>필터 문구를 눌러도 스위치가 바뀌게 한다.</summary>
    private void OnOnlyBuyLabelTapped(object? sender, TappedEventArgs e) => _viewModel.OnlyBuy = !_viewModel.OnlyBuy;

    /// <summary>
    /// 언어 고르기(각 언어 이름은 그 언어로, 지금 언어에 ✓). XAML 문자열은 만들 때 정해지므로 셸을 새로 만들고 목록도 다시 읽는다.
    /// </summary>
    private async void OnLanguageClicked(object? sender, EventArgs e)
    {
        const string check = "✓ ";
        var current = L.CurrentCode;
        var choices = L.Languages.Select(l => (l.Code == current ? check : "") + l.Name).ToArray();
        var picked = await DisplayActionSheet(L.Get("Main_Language"), L.Get("Main_DeleteCancel"), null, choices);
        var index = Array.IndexOf(choices, picked);
        if (index < 0 || L.Languages[index].Code == current)
            return;

        // await 뒤(async 메서드 안)에서 바꾼 CurrentCulture는 메서드가 끝나면 되돌아가므로, 따로 디스패치해 바꾼다
        var code = L.Languages[index].Code;
        var window = Window;
        Dispatcher.Dispatch(() =>
        {
            L.SetLanguage(code);
            if (window is not null)
                window.Page = new AppShell();
            _ = _viewModel.LoadInitialDataAsync();
        });
    }
}
