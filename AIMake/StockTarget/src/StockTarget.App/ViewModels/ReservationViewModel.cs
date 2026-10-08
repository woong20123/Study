using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows;
using StockTarget.Core;

namespace StockTarget.App.ViewModels;

/// <summary>주문표 한 행.</summary>
public sealed class ReservationRowViewModel(ReservationOrder order, double usdKrw, string result) : ObservableObject
{
    private string _result = result;

    public ReservationOrder Order { get; } = order;
    public string Symbol => Order.Symbol;
    public BuyStatus Status => Order.Stage; // 판정 색 스타일(StatusRow) 재사용
    public string StageText => Order.StageText;
    public string Period => $"{Order.Start:MM-dd} ~ {Order.End:MM-dd}";
    public double BuyPrice => Order.BuyPrice;
    public double Price => Order.Price;
    public int Quantity => Order.Quantity;
    public string AmountText => Money.Text(Order.AmountKrw, usdKrw);
    public double OrderUsd => Order.OrderUsd;

    public string Result { get => _result; set => Set(ref _result, value); }
}

/// <summary>
/// 다음 주(월~금) LOC 예약 매수 주문표. 등록된 목표 전체를 보고 계단식 주문을 만들고,
/// 사용자가 확인 버튼을 누를 때만 키움에 접수한다.
/// </summary>
public sealed class ReservationViewModel : ObservableObject
{
    private readonly StockDatabase _db;
    private readonly StockService _service;
    private readonly IReadOnlyList<TargetPlan> _targets;
    private readonly BuyAmounts _defaults;
    private readonly IReadOnlyDictionary<string, double> _currentPrices;
    private readonly KiwoomMode _mode;
    private readonly KiwoomOptions? _options;
    private ReservationPlan? _plan;
    private double? _usdKrw;
    private string _summary = "";
    private string _status = "";
    private bool _busy;

    public ReservationViewModel(
        StockDatabase db, StockService service, IReadOnlyList<TargetPlan> targets, double? usdKrw, BuyAmounts defaults,
        KiwoomMode mode, IReadOnlyDictionary<string, double> currentPrices)
    {
        _currentPrices = currentPrices;
        _mode = mode;
        _defaults = defaults;
        _db = db;
        _service = service;
        _targets = targets;
        _usdKrw = usdKrw;
        _options = KiwoomOptions.FromEnvironment(mode);
        (Start, End) = ReservationPlanner.NextWeek(DateOnly.FromDateTime(DateTime.Today));
        RefreshCommand = new AsyncCommand(() => BuildAsync(force: true), () => !IsBusy);
        SubmitCommand = new AsyncCommand(SubmitAsync, () => !IsBusy && _options is not null && Rows.Any(r => CanSubmit(r)));
    }

    public ObservableCollection<ReservationRowViewModel> Rows { get; } = [];
    public ObservableCollection<ReservationSkip> Skips { get; } = [];
    public ObservableCollection<ReservationRecord> History { get; } = [];

    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand SubmitCommand { get; }

    public DateOnly Start { get; }
    public DateOnly End { get; }

    public string PeriodText =>
        $"예약 기간 {Start:yyyy-MM-dd}(월) ~ {End:yyyy-MM-dd}(금) · 기간예약(잔량주문) · LOC · 계단식(총액 맞춤)";

    public string EnvText => _mode == KiwoomMode.Off
        ? "키움 연동 꺼짐(구동 옵션 --kiwoom off) — 주문표 계산만 가능"
        : _options is null
        ? $"키움 API 키 미설정 — 환경변수 {KiwoomOptions.AppKeyVariable}, {KiwoomOptions.SecretKeyVariable} 필요 (주문표 계산만 가능)"
        : _mode == KiwoomMode.Auto
        ? $"접수 대상: {_options.EnvText} ({_options.BaseUrl})   ·   {KiwoomOptions.EnvVariable}=real 이면 실전 (구동 옵션 --kiwoom 이 우선)"
        : $"접수 대상: {_options.EnvText} ({_options.BaseUrl})   ·   구동 옵션 --kiwoom {(_options.IsMock ? "mock" : "real")}";

    public bool IsRealEnv => _options is { IsMock: false };

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
            _plan = ReservationPlanner.Plan(_targets, Start, End, rate, _defaults, _currentPrices);

            Rows.Clear();
            foreach (var o in _plan.Orders)
                Rows.Add(new ReservationRowViewModel(o, rate, InitialResult(o)));
            Skips.Clear();
            foreach (var s in _plan.Skips)
                Skips.Add(s);
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

    private async Task SubmitAsync()
    {
        if (_options is null || _plan is null)
            return;
        var pending = Rows.Where(CanSubmit).ToList();
        if (pending.Count == 0)
            return;

        var lines = pending.Select(r => $"  {r.Symbol} {r.StageText} LOC {r.Price:0.00} × {r.Quantity}주");
        var message = $"키움 {_options.EnvText} 계좌에 미국주식 LOC 예약 매수 {pending.Count}건을 접수합니다.\n\n" +
                      $"기간 {Start:yyyy-MM-dd} ~ {End:yyyy-MM-dd} (기간예약 · 잔량주문)\n" +
                      string.Join("\n", lines) +
                      $"\n\n주문가 기준 합계 {Money.UsdText(pending.Sum(r => r.OrderUsd))}" +
                      (IsRealEnv ? "\n\n※ 실전 계좌입니다. 종가가 주문가 이하이면 실제로 매수됩니다." : "") +
                      "\n\n접수할까요?";
        if (MessageBox.Show(message, "LOC 예약 매수 확인", MessageBoxButton.YesNo,
                IsRealEnv ? MessageBoxImage.Warning : MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
        {
            Status = "접수 취소";
            return;
        }

        IsBusy = true;
        try
        {
            using var client = new KiwoomClient(_options);
            var service = new ReservationService(_db, client);
            var rowsByOrder = pending.ToDictionary(r => r.Order);
            var progress = new Progress<ReservationResult>(r => rowsByOrder[r.Order].Result = r.Message);
            var results = await service.SubmitAsync(pending.Select(r => r.Order), progress);
            LoadHistory();
            Status = $"{DateTime.Now:HH:mm:ss} {_options.EnvText} 접수: 성공 {results.Count(r => r.Submitted)}, " +
                     $"실패·건너뜀 {results.Count(r => !r.Submitted)}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSubmit(ReservationRowViewModel r) => r.Result is "미접수" || r.Result.StartsWith("실패", StringComparison.Ordinal);

    private string InitialResult(ReservationOrder o) =>
        _options is not null && _db.FindReservation(o.Symbol, o.StageText, o.Start, o.End, _options.EnvText) is { } prev
            ? $"이미 접수됨 (예약번호 {prev.ReservationNo})"
            : "미접수";

    private void LoadHistory()
    {
        History.Clear();
        foreach (var r in _db.GetReservations())
            History.Add(r);
    }
}
