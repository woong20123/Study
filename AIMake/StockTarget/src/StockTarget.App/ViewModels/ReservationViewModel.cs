using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows;
using StockTarget.Core;

namespace StockTarget.App.ViewModels;

/// <summary>주문표 한 행.</summary>
public sealed class ReservationRowViewModel(ReservationOrder order, double usdKrw, string name = "")
{
    public ReservationOrder Order { get; } = order;
    public string Symbol => Order.Symbol;
    public string Name { get; } = name;
    public BuyStatus Status => Order.Stage; // 판정 색 스타일(StatusRow) 재사용
    public string StageText => Order.StageText;
    public string Period => $"{Order.Start:MM-dd} ~ {Order.End:MM-dd}";
    public double BuyPrice => Order.BuyPrice;
    public double Price => Order.Price;
    public int Quantity => Order.Quantity;
    public string AmountText => Money.Text(Order.AmountKrw, usdKrw);
    public double OrderUsd => Order.OrderUsd;
}

/// <summary>제외 목록 한 행(종목명 표시용).</summary>
public sealed record ReservationSkipRow(string Symbol, string Name, string Reason, bool BuyDone);

/// <summary>
/// 다음 주(월~금) LOC 예약 매수 주문표. 등록된 목표 전체를 보고 계단식 주문을 만든다.
/// 증권사에 접수하지는 않는다 — 주문표를 복사해 증권사 앱에서 직접 넣을 때 참고한다.
/// </summary>
public sealed class ReservationViewModel : ObservableObject
{
    private readonly StockDatabase _db;
    private readonly StockService _service;
    private readonly IReadOnlyList<TargetPlan> _targets;
    private readonly BuyAmounts _defaults;
    private readonly IReadOnlyDictionary<string, double> _currentPrices;
    private ReservationPlan? _plan;
    private double? _usdKrw;
    private string _summary = "";
    private string _status = "";
    private bool _busy;

    public ReservationViewModel(
        StockDatabase db, StockService service, IReadOnlyList<TargetPlan> targets, double? usdKrw, BuyAmounts defaults,
        IReadOnlyDictionary<string, double> currentPrices)
    {
        _currentPrices = currentPrices;
        _defaults = defaults;
        _db = db;
        _service = service;
        _targets = targets;
        _usdKrw = usdKrw;
        (Start, End) = ReservationPlanner.NextWeek(DateOnly.FromDateTime(DateTime.Today));
        RefreshCommand = new AsyncCommand(() => BuildAsync(force: true), () => !IsBusy);
        CopyCommand = new RelayCommand(Copy, () => !IsBusy && Rows.Count > 0);
        MarkBuyDoneCommand = new RelayCommand<string>(s => SetBuyDone(s, true), _ => !IsBusy);
        UnmarkBuyDoneCommand = new RelayCommand<string>(s => SetBuyDone(s, false), _ => !IsBusy);
    }

    public ObservableCollection<ReservationRowViewModel> Rows { get; } = [];
    public ObservableCollection<ReservationSkipRow> Skips { get; } = [];

    /// <summary>예전 버전에서 키움에 접수했던 이력(읽기 전용).</summary>
    public ObservableCollection<ReservationRecord> History { get; } = [];

    public AsyncCommand RefreshCommand { get; }

    /// <summary>주문표를 텍스트로 클립보드에 복사한다.</summary>
    public RelayCommand CopyCommand { get; }

    /// <summary>티커(CommandParameter)를 '매수 완료'로 표시해 해제할 때까지 주문표에서 뺀다.</summary>
    public RelayCommand<string> MarkBuyDoneCommand { get; }

    /// <summary>'매수 완료' 표시를 해제해 다시 주문표에 넣는다.</summary>
    public RelayCommand<string> UnmarkBuyDoneCommand { get; }

    public DateOnly Start { get; }
    public DateOnly End { get; }

    public string PeriodText =>
        $"예약 기간 {Start:yyyy-MM-dd}(월) ~ {End:yyyy-MM-dd}(금) · 기간예약(잔량주문) · LOC · 계단식(총액 맞춤)";

    public string Summary { get => _summary; set => Set(ref _summary, value); }
    public string Status { get => _status; set => Set(ref _status, value); }
    public bool IsBusy { get => _busy; set => Set(ref _busy, value); }

    public Task LoadAsync() => BuildAsync(force: false);

    private async Task BuildAsync(bool force)
    {
        IsBusy = true;
        try
        {
            if (_usdKrw is null || force)
            {
                Status = "환율 조회 중...";
                _usdKrw = (await _service.GetUsdKrwAsync(force)).Value.Price;
            }
            var rate = _usdKrw.Value;
            _plan = ReservationPlanner.Plan(_targets, Start, End, rate, _defaults, _currentPrices, _db.GetBuyDone());

            var names = _db.GetStockNames();
            Rows.Clear();
            foreach (var o in _plan.Orders)
                Rows.Add(new ReservationRowViewModel(o, rate, names.GetValueOrDefault(o.Symbol, "")));
            Skips.Clear();
            foreach (var s in _plan.Skips)
                Skips.Add(new ReservationSkipRow(s.Symbol, names.GetValueOrDefault(s.Symbol, ""), s.Reason, s.BuyDone));
            LoadHistory();

            var totalKrw = _plan.Orders.Sum(o => o.AmountKrw);
            var totalUsd = _plan.Orders.Sum(o => o.OrderUsd);
            Summary = $"목표 {_targets.Count}개 → 현재가가 도달한 단계 주문 {_plan.Orders.Count}건 (제외 {_plan.Skips.Count}건) · " +
                      $"최대 체결 {Money.KrwText(totalKrw)} / 주문가 기준 {Money.UsdText(totalUsd)} · 환율 {rate:#,0.00}원";
            Status = $"{DateTime.Now:HH:mm:ss} 주문표 계산";
        }
        catch (Exception e) when (e is StockDataException or HttpRequestException or TaskCanceledException or ArgumentException)
        {
            Status = "주문표 계산 실패: " + e.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Copy()
    {
        var lines = Rows.Select(r => $"{r.Symbol}\t{r.StageText}\tLOC {r.Price:0.00}\t{r.Quantity}주\t{r.Period}");
        var text = $"LOC 예약 매수 {Start:yyyy-MM-dd} ~ {End:yyyy-MM-dd} (기간예약 · 잔량주문)\n" +
                   string.Join("\n", lines) +
                   $"\n주문가 기준 합계 {Money.UsdText(Rows.Sum(r => r.OrderUsd))}";
        Clipboard.SetText(text);
        Status = $"주문표 {Rows.Count}건을 클립보드에 복사했습니다";
    }

    private async void SetBuyDone(string symbol, bool done)
    {
        _db.SetBuyDone(symbol, done);
        await BuildAsync(force: false); // 환율은 그대로 두고 주문표만 다시 계산
        Status = done
            ? $"{symbol} 매수 완료 — 해제할 때까지 주문표에서 제외"
            : $"{symbol} 매수 완료 해제 — 다시 주문표에 넣음";
    }

    private void LoadHistory()
    {
        History.Clear();
        foreach (var r in _db.GetReservations())
            History.Add(r);
    }
}
