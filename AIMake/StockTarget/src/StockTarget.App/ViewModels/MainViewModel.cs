using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.Windows;
using StockTarget.Core;

namespace StockTarget.App.ViewModels;

public sealed record ScheduleItem(string Quarter, DateOnly BaseDate, double BuyPrice, bool IsCurrent);

public sealed class MainViewModel : ObservableObject
{
    private readonly StockDatabase _db;
    private readonly StockService _service;

    private TargetRowViewModel? _selected;
    private string _status = "";
    private bool _busy;
    private string _summary = "목표를 선택하거나 새로 입력하세요.";
    private string _dividendSummary = "";
    private string _formSymbol = "";
    private string _formYear = "";
    private string _formEps = "";
    private string _formPer = "";
    private string _formReturn = "10";
    private string _formDividend = "";
    private string _formMemo = "";

    public MainViewModel(StockDatabase db, StockService service)
    {
        _db = db;
        _service = service;
        SaveCommand = new AsyncCommand(SaveAsync, () => !IsBusy);
        DeleteCommand = new AsyncCommand(DeleteAsync, () => !IsBusy && Selected is not null);
        RefreshCommand = new AsyncCommand(() => RefreshAllAsync(force: true), () => !IsBusy);
        NewCommand = new RelayCommand(ClearForm);
        DbPath = db.Path;
    }

    // ------------------------------------------------------------- 바인딩 속성
    public ObservableCollection<TargetRowViewModel> Targets { get; } = [];
    public ObservableCollection<ScheduleItem> Schedule { get; } = [];
    public ObservableCollection<DividendPeriod> DividendPeriods { get; } = [];
    public ObservableCollection<PriceCheck> Checks { get; } = [];

    public AsyncCommand SaveCommand { get; }
    public AsyncCommand DeleteCommand { get; }
    public AsyncCommand RefreshCommand { get; }
    public RelayCommand NewCommand { get; }

    public string DbPath { get; }

    public TargetRowViewModel? Selected
    {
        get => _selected;
        set
        {
            if (Set(ref _selected, value))
                _ = OnSelectedChangedAsync();
        }
    }

    public string Status { get => _status; set => Set(ref _status, value); }
    public bool IsBusy { get => _busy; set => Set(ref _busy, value); }
    public string Summary { get => _summary; set => Set(ref _summary, value); }
    public string DividendSummary { get => _dividendSummary; set => Set(ref _dividendSummary, value); }

    public string FormSymbol { get => _formSymbol; set => Set(ref _formSymbol, value); }
    public string FormYear { get => _formYear; set => Set(ref _formYear, value); }
    public string FormEps { get => _formEps; set => Set(ref _formEps, value); }
    public string FormPer { get => _formPer; set => Set(ref _formPer, value); }
    public string FormReturn { get => _formReturn; set => Set(ref _formReturn, value); }
    public string FormDividend { get => _formDividend; set => Set(ref _formDividend, value); }
    public string FormMemo { get => _formMemo; set => Set(ref _formMemo, value); }

    // ------------------------------------------------------------------ 동작
    public async Task LoadAsync()
    {
        Targets.Clear();
        foreach (var t in _db.GetTargets())
            Targets.Add(new TargetRowViewModel(t));
        Status = $"목표 {Targets.Count}개 로드";
        await RefreshAllAsync(force: false);
        if (Selected is null && Targets.Count > 0)
            Selected = Targets[0];
    }

    /// <summary>모든 목표의 현재가를 조회하고 이번 분기 확인 이력을 남긴다. force면 캐시를 무시한다.</summary>
    private async Task RefreshAllAsync(bool force)
    {
        if (Targets.Count == 0)
            return;
        IsBusy = true;
        int ok = 0, cached = 0, failed = 0;
        try
        {
            foreach (var row in Targets)
            {
                try
                {
                    var q = await _service.GetQuoteAsync(row.Symbol, force);
                    row.Quote = q.Value;
                    row.CacheAge = q.CacheAgeSeconds;
                    row.Error = null;
                    _service.RecordCheck(row.Plan, q.Value);
                    ok++;
                    if (q.FromCache) cached++;
                }
                catch (Exception e) when (e is StockDataException or HttpRequestException or TaskCanceledException)
                {
                    row.Error = e.Message;
                    failed++;
                }
            }
            Status = $"{DateTime.Now:HH:mm:ss} 시세 갱신: 성공 {ok}(캐시 {cached}), 실패 {failed}" + (force ? " — 캐시 무시" : "");
            if (Selected is not null)
                await LoadDetailAsync(Selected, force);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            var symbol = StockService.Normalize(FormSymbol);
            if (symbol.Length == 0)
                throw new ArgumentException("티커를 입력하세요");
            var year = ParseInt(FormYear, "목표 연도");
            var eps = ParseDouble(FormEps, "EPS");
            var per = ParseDouble(FormPer, "PER");
            var ret = ParseDouble(FormReturn, "목표 수익률");

            IsBusy = true;
            double div;
            if (string.IsNullOrWhiteSpace(FormDividend))
            {
                Status = $"{symbol} 최근 5년 평균 배당수익률 조회 중...";
                div = (await _service.GetDividendYieldAsync(symbol)).Value.AvgYieldPct;
                FormDividend = div.ToString("0.####", CultureInfo.InvariantCulture);
            }
            else
            {
                div = ParseDouble(FormDividend, "배당수익률");
            }

            var plan = new TargetPlan(symbol, DateOnly.FromDateTime(DateTime.Today), year, eps, per, ret, div,
                string.IsNullOrWhiteSpace(FormMemo) ? null : FormMemo.Trim());
            TargetCalculator.Validate(plan);
            _db.SaveTarget(plan);

            var row = Targets.FirstOrDefault(r => r.Symbol == symbol);
            if (row is null)
            {
                row = new TargetRowViewModel(plan);
                Targets.Add(row);
            }
            else
            {
                row.Update(plan);
            }
            IsBusy = false;
            Selected = row;
            await RefreshOneAsync(row);
            Status = $"{symbol} 목표 저장 (배당수익률 {div:0.00}% 고정, 필요 주가 상승률 연 {plan.GrowthPct:0.00}%)";
        }
        catch (Exception e) when (e is ArgumentException or StockDataException or HttpRequestException)
        {
            Status = "저장 실패: " + e.Message;
            MessageBox.Show(e.Message, "저장 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshOneAsync(TargetRowViewModel row)
    {
        try
        {
            var q = await _service.GetQuoteAsync(row.Symbol);
            row.Quote = q.Value;
            row.CacheAge = q.CacheAgeSeconds;
            row.Error = null;
            _service.RecordCheck(row.Plan, q.Value);
        }
        catch (Exception e) when (e is StockDataException or HttpRequestException or TaskCanceledException)
        {
            row.Error = e.Message;
        }
        await LoadDetailAsync(row, force: false);
    }

    private Task DeleteAsync()
    {
        if (Selected is not { } row)
            return Task.CompletedTask;
        var answer = MessageBox.Show($"{row.Symbol} 목표와 확인 이력을 삭제할까요?", "삭제 확인",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes)
            return Task.CompletedTask;

        _db.DeleteTarget(row.Symbol);
        Targets.Remove(row);
        Selected = Targets.FirstOrDefault();
        if (Selected is null)
            ClearDetail();
        Status = $"{row.Symbol} 목표 삭제";
        return Task.CompletedTask;
    }

    private void ClearForm()
    {
        Selected = null;
        FormSymbol = FormYear = FormEps = FormPer = FormDividend = FormMemo = "";
        FormReturn = "10";
        ClearDetail();
    }

    private void ClearDetail()
    {
        Schedule.Clear();
        DividendPeriods.Clear();
        Checks.Clear();
        Summary = "목표를 선택하거나 새로 입력하세요.";
        DividendSummary = "";
    }

    private async Task OnSelectedChangedAsync()
    {
        if (Selected is not { } row)
            return;
        var p = row.Plan;
        FormSymbol = p.Symbol;
        FormYear = p.TargetYear.ToString(CultureInfo.InvariantCulture);
        FormEps = p.Eps.ToString(CultureInfo.InvariantCulture);
        FormPer = p.Per.ToString(CultureInfo.InvariantCulture);
        FormReturn = p.ReturnPct.ToString(CultureInfo.InvariantCulture);
        FormDividend = p.DividendYieldPct.ToString("0.####", CultureInfo.InvariantCulture);
        FormMemo = p.Memo ?? "";
        await LoadDetailAsync(row, force: false);
    }

    /// <summary>오른쪽 패널(요약·분기 일정·배당·이력) 갱신.</summary>
    private async Task LoadDetailAsync(TargetRowViewModel row, bool force)
    {
        var p = row.Plan;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var current = TargetCalculator.QuarterLabel(today);

        Schedule.Clear();
        foreach (var r in TargetCalculator.Schedule(p))
            Schedule.Add(new ScheduleItem(r.Quarter, r.BaseDate, r.BuyPrice, r.Quarter == current));

        Checks.Clear();
        foreach (var c in _db.GetChecks(p.Symbol))
            Checks.Add(c);

        // 오른쪽 패널 폭(약 440px)에 맞춰 한 줄을 짧게 유지한다
        var lines = new List<string>
        {
            $"{p.Symbol}  {row.Quote?.Name}",
            $"목표      {p.TargetYear}년 말 (입력일 {p.InputDate:yyyy-MM-dd})",
            $"목표 주가 EPS {p.Eps:0.###} × PER {p.Per:0.###} = {p.TargetPrice:N2}",
            $"필요 상승 {p.ReturnPct:0.##}% − 배당 {p.DividendYieldPct:0.00}% = 연 {p.GrowthPct:0.00}%",
        };
        if (p.GrowthPct <= 0)
            lines.Add("※ 배당만으로 목표 수익률을 넘어 매입 목표가가 목표 주가보다 높음");
        if (row.Price is { } price && row.CurrentBuyPrice is { } buy)
        {
            lines.Add($"현재가    {price:N2} / {row.CurrentQuarter} 매입 목표가 {buy:N2}");
            lines.Add($"판정      {row.Verdict} ({row.GapPct:+0.0;-0.0}%)");
        }
        else if (row.Error is not null)
            lines.Add($"현재가 조회 실패: {row.Error}");
        if (row.Quote?.SplitNote is { } note)
            lines.Add($"※ {note}");
        if (!string.IsNullOrEmpty(p.Memo))
            lines.Add($"메모: {p.Memo}");
        Summary = string.Join(Environment.NewLine, lines);

        DividendPeriods.Clear();
        DividendSummary = "배당 이력 조회 중...";
        try
        {
            var d = await _service.GetDividendYieldAsync(p.Symbol, force);
            if (Selected != row)
                return; // 조회 중 선택이 바뀌었다
            foreach (var x in d.Value.Periods)
                DividendPeriods.Add(x);
            var text = $"{d.Value.Years}년 평균: 배당금 {d.Value.AvgDividend:0.0000} / 평균 주가 {d.Value.AvgPrice:N2} / 수익률 {d.Value.AvgYieldPct:0.00}%";
            if (Math.Abs(d.Value.AvgYieldPct - p.DividendYieldPct) >= 0.01)
                text += $"   (목표에 고정된 값 {p.DividendYieldPct:0.00}%)";
            if (d.Value.Splits.Count > 0)
                text += Environment.NewLine + "기간 내 주식 분할: " + string.Join(", ", d.Value.Splits.Select(s => $"{s.Date:yyyy-MM-dd} {s.RatioText}"))
                        + " → 배당금·주가는 현재 주식 수 기준으로 환산된 값";
            if (d.Value.AvgDividend == 0)
                text += Environment.NewLine + "이 기간 배당 지급 기록이 없습니다(무배당 종목)";
            if (d.CacheAgeSeconds is { } age)
                text += $"   [캐시 {Format.Age(age)}]";
            DividendSummary = text;
        }
        catch (Exception e) when (e is StockDataException or HttpRequestException or TaskCanceledException)
        {
            DividendSummary = "배당 이력 조회 실패: " + e.Message;
        }
    }

    private static double ParseDouble(string text, string name) =>
        double.TryParse(text?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
            ? v
            : throw new ArgumentException($"{name} 값이 올바르지 않습니다: '{text}'");

    private static int ParseInt(string text, string name) =>
        int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)
            ? v
            : throw new ArgumentException($"{name} 값이 올바르지 않습니다: '{text}'");
}
