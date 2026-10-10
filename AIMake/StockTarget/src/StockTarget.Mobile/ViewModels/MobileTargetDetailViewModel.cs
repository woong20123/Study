using System.Collections.ObjectModel;
using StockTarget.Core;
using StockTarget.Mobile.Localization;

namespace StockTarget.Mobile.ViewModels;

[QueryProperty(nameof(TargetRow), "TargetRow")]
public sealed class MobileTargetDetailViewModel : ObservableObject
{
    private readonly StockDatabase _db;
    private readonly StockService _stockService;
    private readonly MobileMainViewModel _main;
    private MobileTargetRowViewModel? _targetRow;
    private bool _isBusy;
    private string _status = "";

    public ObservableCollection<ScheduleRow> Schedule { get; } = [];
    public ObservableCollection<DividendPeriod> DividendPeriods { get; } = [];
    public ObservableCollection<PriceCheck> PriceChecks { get; } = [];

    public MobileTargetRowViewModel? TargetRow
    {
        get => _targetRow;
        set
        {
            if (Set(ref _targetRow, value) && value != null)
            {
                OnPropertyChanged(nameof(IsBuyDone));
                OnPropertyChanged(nameof(ShowBuyDone));
                _ = LoadDetailsAsync(value);
            }
        }
    }

    /// <summary>매수 완료 스위치. 켜고 끄면 바로 저장한다.</summary>
    public bool IsBuyDone
    {
        get => TargetRow?.IsBuyDone ?? false;
        set
        {
            if (TargetRow is null || TargetRow.IsBuyDone == value)
                return;
            _main.SetBuyDone(TargetRow, value);
            OnPropertyChanged();
        }
    }

    /// <summary>매수 단계이거나 이미 완료로 표시한 종목에만 스위치를 보인다.</summary>
    public bool ShowBuyDone => TargetRow is { } r && (r.NeedsBuy || r.IsBuyDone);

    public AsyncCommand EditCommand { get; }
    public AsyncCommand DeleteCommand { get; }

    /// <summary>
    /// 화면에 다시 들어올 때(수정하고 돌아올 때) 부른다. 저장하면 목록을 새로 읽어 행 객체가 바뀌므로 같은 티커의 새 행으로 바꾸고,
    /// 그 사이 목록에서 없어졌으면 목록으로 돌아간다.
    /// </summary>
    public async Task RefreshAsync()
    {
        if (TargetRow is not { } row)
            return;
        var fresh = _main.Targets.FirstOrDefault(r => r.Symbol == row.Symbol);
        if (fresh is null)
            await Shell.Current.GoToAsync("..");
        else if (!ReferenceEquals(fresh, row))
            TargetRow = fresh;
    }

    private Task EditAsync() =>
        TargetRow is { } row
            ? Shell.Current.GoToAsync($"{nameof(Views.TargetEditPage)}?Symbol={Uri.EscapeDataString(row.Symbol)}")
            : Task.CompletedTask;

    private async Task DeleteAsync()
    {
        if (await _main.DeleteAsync(TargetRow))
            await Shell.Current.GoToAsync("..");
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => Set(ref _isBusy, value);
    }

    public string Status
    {
        get => _status;
        set => Set(ref _status, value);
    }

    public MobileTargetDetailViewModel(StockDatabase db, StockService stockService, MobileMainViewModel main)
    {
        _db = db;
        _stockService = stockService;
        _main = main;
        EditCommand = new AsyncCommand(EditAsync);
        DeleteCommand = new AsyncCommand(DeleteAsync);
    }

    private async Task LoadDetailsAsync(MobileTargetRowViewModel row)
    {
        IsBusy = true;
        try
        {
            // 1. 분기별 매입 목표가 일정
            var schedule = TargetCalculator.Schedule(row.Plan);
            Schedule.Clear();
            foreach (var s in schedule) Schedule.Add(s);

            // 2. 과거 확인 이력
            var history = _db.GetChecks(row.Symbol);
            PriceChecks.Clear();
            foreach (var h in history) PriceChecks.Add(h);

            // 3. 5년 배당 내역 조회
            var divYield = await _stockService.GetDividendYieldAsync(row.Symbol, force: false);
            DividendPeriods.Clear();
            foreach (var p in divYield.Value.Periods) DividendPeriods.Add(p);

            Status = L.Format("Detail_Loaded", row.Title);
        }
        catch (Exception ex)
        {
            Status = L.Format("Detail_Failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
