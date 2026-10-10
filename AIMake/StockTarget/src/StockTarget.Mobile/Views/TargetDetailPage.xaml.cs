using StockTarget.Mobile.ViewModels;

namespace StockTarget.Mobile.Views;

public partial class TargetDetailPage : ContentPage
{
    private readonly MobileTargetDetailViewModel _viewModel;

    public TargetDetailPage(MobileTargetDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>수정 화면에서 저장하고 돌아오면 새로 읽은 목표로 바꾼다.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshAsync();
    }
}
