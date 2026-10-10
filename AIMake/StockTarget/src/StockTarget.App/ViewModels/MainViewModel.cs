using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.Windows;
using StockTarget.Core;

namespace StockTarget.App.ViewModels;

public sealed record ScheduleItem(string Quarter, DateOnly BaseDate, double BuyPrice, bool IsCurrent);

public sealed partial class MainViewModel : ObservableObject
{
    private readonly StockDatabase _db;
    private readonly StockService _service;
    private readonly StockNameService? _names;

    private TargetRowViewModel? _selected;
    private string _status = "";
    private bool _busy;
    private string _summary = "목표를 선택하거나 새로 입력하세요.";
    private string _dividendSummary = "";
    private string _formSymbol = "";
    private string _formYear = "";
    private string _formEps = "";
    private string _formPer = "";
    private bool _formDirectPrice;
    private string _formTargetPrice = "";
    private string _formReturn = "10";
    private string _formDividend = "";
    private string _formMemo = "";
    private string _formBuyKrw = "";
    private string _formMustBuyKrw = "";
    private string _formStrongBuyKrw = "";
    private double? _usdKrw;

    public MainViewModel(StockDatabase db, StockService service, StockNameService? names = null)
    {
        _db = db;
        _service = service;
        _names = names;
        SaveCommand = new AsyncCommand(SaveAsync, () => !IsBusy);
        DeleteCommand = new AsyncCommand(DeleteAsync, () => !IsBusy && Selected is not null);
        RefreshCommand = new AsyncCommand(() => RefreshAllAsync(force: true), () => !IsBusy);
        NewCommand = new RelayCommand(ClearForm);
        ReserveCommand = new RelayCommand(OpenReservation, () => !IsBusy && Targets.Count > 0);
        BuyDoneCommand = new RelayCommand(OpenBuyDone, () => !IsBusy && Targets.Count > 0);
        DbPath = db.Path;
        InitBackupCommands();
        InitDefaults();
        ClearForm();
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
    public RelayCommand ReserveCommand { get; }
    public RelayCommand BuyDoneCommand { get; }

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
    public string FormEps
    {
        get => _formEps;
        set
        {
            if (Set(ref _formEps, value))
                OnPropertyChanged(nameof(FormEpsPerPreview));
        }
    }

    public string FormPer
    {
        get => _formPer;
        set
        {
            if (Set(ref _formPer, value))
                OnPropertyChanged(nameof(FormEpsPerPreview));
        }
    }

    /// <summary>목표 주가 입력 방식. false = EPS × PER, true = 목표 주가 직접 입력.</summary>
    public bool FormDirectPrice
    {
        get => _formDirectPrice;
        set
        {
            if (!Set(ref _formDirectPrice, value))
                return;
            OnPropertyChanged(nameof(FormEpsPerMode));
            OnPropertyChanged(nameof(FormEpsPerPreview));
            // 직접 입력으로 바꿀 때 비어 있으면 지금까지 입력한 EPS × PER 값을 채워 준다
            if (value && string.IsNullOrWhiteSpace(FormTargetPrice) && EpsPerPrice() is { } price)
                FormTargetPrice = price.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>EPS × PER 라디오 버튼용(FormDirectPrice의 반대).</summary>
    public bool FormEpsPerMode
    {
        get => !FormDirectPrice;
        set => FormDirectPrice = !value;
    }

    public string FormTargetPrice { get => _formTargetPrice; set => Set(ref _formTargetPrice, value); }

    /// <summary>EPS × PER 방식일 때 계산된 목표 주가 미리보기.</summary>
    public string FormEpsPerPreview =>
        !FormDirectPrice && EpsPerPrice() is { } price ? $"목표 주가 = {price:N2}" : "";
    public string FormReturn { get => _formReturn; set => Set(ref _formReturn, value); }
    public string FormDividend { get => _formDividend; set => Set(ref _formDividend, value); }
    public string FormMemo { get => _formMemo; set => Set(ref _formMemo, value); }

    // 매수 단계별 매수금액(원). 입력하면 바로 아래에 달러 환산을 보여 준다.
    public string FormBuyKrw
    {
        get => _formBuyKrw;
        set
        {
            if (Set(ref _formBuyKrw, value))
                OnPropertyChanged(nameof(FormBuyUsd));
        }
    }

    public string FormMustBuyKrw
    {
        get => _formMustBuyKrw;
        set
        {
            if (Set(ref _formMustBuyKrw, value))
                OnPropertyChanged(nameof(FormMustBuyUsd));
        }
    }

    public string FormStrongBuyKrw
    {
        get => _formStrongBuyKrw;
        set
        {
            if (Set(ref _formStrongBuyKrw, value))
                OnPropertyChanged(nameof(FormStrongBuyUsd));
        }
    }

    public string FormBuyUsd => AmountPreview(FormBuyKrw, BuyStatus.Buy);
    public string FormMustBuyUsd => AmountPreview(FormMustBuyKrw, BuyStatus.MustBuy);
    public string FormStrongBuyUsd => AmountPreview(FormStrongBuyKrw, BuyStatus.StrongBuy);

    /// <summary>USD/KRW 환율(1달러당 원). 조회 전이거나 실패하면 null.</summary>
    public double? UsdKrw
    {
        get => _usdKrw;
        private set
        {
            if (!Set(ref _usdKrw, value))
                return;
            foreach (var row in Targets)
                row.UsdKrw = value;
            OnPropertyChanged(nameof(UsdKrwText));
            RaiseFormAmountPreviews();
        }
    }

    public string UsdKrwText => UsdKrw is { } r ? $"USD/KRW {r:#,0.00}" : "USD/KRW -";

    // ------------------------------------------------------------------ 동작
    public async Task LoadAsync()
    {
        Targets.Clear();
        LoadDefaults();
        var names = _db.GetStockNames(); // 캐시한 한글명을 먼저 보여주고, 시세 갱신 때 7일 지난 것만 다시 받는다
        _db.ClearStaleBuyDone(); // 지난 분기의 매수 완료는 풀린다(다음 분기엔 다시 살 차례)
        var buyDone = _db.GetBuyDone();
        foreach (var t in _db.GetTargets())
            Targets.Add(new TargetRowViewModel(t)
            {
                UsdKrw = UsdKrw, Defaults = Defaults, KoreanName = names.GetValueOrDefault(t.Symbol), IsBuyDone = buyDone.Contains(t.Symbol)
            });
        OnPropertyChanged(nameof(DefaultsSummary));
        Status = $"목표 {Targets.Count}개 로드";
        await RefreshAllAsync(force: false);
        if (Selected is null && Targets.Count > 0)
            Selected = Targets[0];
    }

    /// <summary>모든 목표의 현재가를 조회하고 이번 분기 확인 이력을 남긴다. force면 캐시를 무시한다.</summary>
    private async Task RefreshAllAsync(bool force)
    {
        await RefreshUsdKrwAsync(force);
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
                await LoadNameAsync(row);
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

    /// <summary>매수금액 달러 환산용 환율 조회. 실패하면 이전 값을 유지하고 원화만 표시한다.</summary>
    private async Task RefreshUsdKrwAsync(bool force)
    {
        try
        {
            UsdKrw = (await _service.GetUsdKrwAsync(force)).Value.Price;
        }
        catch (Exception e) when (e is StockDataException or HttpRequestException or TaskCanceledException)
        {
            Status = "환율 조회 실패(매수금액은 원화만 표시): " + e.Message;
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
            // 직접 입력이면 EPS·PER은 쓰지 않으므로 0으로 저장한다
            double eps = 0, per = 0;
            double? targetPrice = null;
            if (FormDirectPrice)
            {
                targetPrice = ParseDouble(FormTargetPrice, "목표 주가");
            }
            else
            {
                eps = ParseDouble(FormEps, "EPS");
                per = ParseDouble(FormPer, "PER");
            }
            var ret = ParseDouble(FormReturn, "목표 수익률");
            // 기본 매수금액과 같거나 비운 단계는 저장하지 않는다(기본값을 따라가 기본값을 바꾸면 함께 바뀐다)
            var amounts = new BuyAmounts(
                Money.ParseKrw(FormBuyKrw, "매수 1단계"),
                Money.ParseKrw(FormMustBuyKrw, "매수 2단계"),
                Money.ParseKrw(FormStrongBuyKrw, "매수 3단계")).ExceptDefaults(Defaults);

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
                string.IsNullOrWhiteSpace(FormMemo) ? null : FormMemo.Trim(),
                amounts.IsEmpty ? null : amounts, targetPrice);
            TargetCalculator.Validate(plan);
            _db.SaveTarget(plan);

            var row = Targets.FirstOrDefault(r => r.Symbol == symbol);
            if (row is null)
            {
                row = new TargetRowViewModel(plan) { UsdKrw = UsdKrw, Defaults = Defaults };
                Targets.Add(row);
            }
            else
            {
                row.Update(plan);
            }
            IsBusy = false;
            Selected = row;
            OnPropertyChanged(nameof(DefaultsSummary));
            await RefreshOneAsync(row);
            Status = $"{symbol} 목표 저장 (배당수익률 {div:0.00}% 고정, 필요 주가 상승률 연 {plan.GrowthPct:0.00}%, " +
                     $"매수금액 {(amounts.IsEmpty ? "기본값 사용" : "개별 " + DefaultsText(amounts))})";
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
        await LoadNameAsync(row);
        await LoadDetailAsync(row, force: false);
    }

    /// <summary>한글 종목명(7일 캐시). 못 받으면 영문명이 그대로 보인다.</summary>
    private async Task LoadNameAsync(TargetRowViewModel row)
    {
        if (_names is not null)
            row.KoreanName = await _names.GetNameAsync(row.Symbol);
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
        OnPropertyChanged(nameof(DefaultsSummary));
        Selected = Targets.FirstOrDefault();
        if (Selected is null)
            ClearDetail();
        Status = $"{row.Symbol} 목표 삭제";
        return Task.CompletedTask;
    }

    /// <summary>
    /// 다음 주 LOC 예약 매수 주문표 창. 지금 화면의 현재가로 이미 도달한 단계만 주문표에 넣는다.
    /// 증권사에 접수하지는 않고, 주문표를 복사해 직접 주문할 때 참고한다.
    /// </summary>
    private void OpenReservation()
    {
        var vm = new ReservationViewModel(_db, _service, Targets.Select(r => r.Plan).ToList(), UsdKrw, Defaults,
            Targets.Where(r => r.Error is null && r.Price is > 0).ToDictionary(r => r.Symbol, r => r.Price!.Value));
        new ReservationWindow(vm) { Owner = Application.Current.MainWindow }.ShowDialog();
        SyncBuyDone(); // 주문표 창에서 매수 완료를 표시 · 해제했을 수 있다
    }

    private void OpenBuyDone()
    {
        var vm = new BuyDoneViewModel(this);
        new BuyDoneWindow(vm) { Owner = Application.Current.MainWindow }.ShowDialog();
    }

    /// <summary>종목을 '매수 완료'로 표시하거나 해제해 저장한다.</summary>
    public void SetBuyDone(TargetRowViewModel row, bool done)
    {
        if (row.IsBuyDone == done)
            return;
        _db.SetBuyDone(row.Symbol, done);
        row.IsBuyDone = done;
        Status = done ? $"{row.Symbol} 매수 완료 표시" : $"{row.Symbol} 매수 완료 해제";
    }

    private void SyncBuyDone()
    {
        var done = _db.GetBuyDone();
        foreach (var row in Targets)
            row.IsBuyDone = done.Contains(row.Symbol);
    }

    private void ClearForm()
    {
        Selected = null;
        FormSymbol = FormYear = FormEps = FormPer = FormDividend = FormMemo = FormTargetPrice = "";
        FillFormAmounts(BuyAmounts.Empty); // 새 목표는 기본 매수금액으로 시작
        FormDirectPrice = false;
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
        FormTargetPrice = p.TargetPriceInput?.ToString(CultureInfo.InvariantCulture) ?? "";
        FormDirectPrice = p.IsDirectTargetPrice;
        FormEps = p.IsDirectTargetPrice ? "" : p.Eps.ToString(CultureInfo.InvariantCulture);
        FormPer = p.IsDirectTargetPrice ? "" : p.Per.ToString(CultureInfo.InvariantCulture);
        FormReturn = p.ReturnPct.ToString(CultureInfo.InvariantCulture);
        FormDividend = p.DividendYieldPct.ToString("0.####", CultureInfo.InvariantCulture);
        FormMemo = p.Memo ?? "";
        FillFormAmounts(p.BuyAmounts);
        await LoadDetailAsync(row, force: false);
    }

    /// <summary>마지막으로 시작한 오른쪽 패널 갱신 번호. 겹친 갱신이 배당 내역을 중복으로 채우지 않게 한다.</summary>
    private int _detailLoadId;

    /// <summary>오른쪽 패널(요약·분기 일정·배당·이력) 갱신.</summary>
    private async Task LoadDetailAsync(TargetRowViewModel row, bool force)
    {
        var id = ++_detailLoadId;
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
            $"{row.Name}  {p.Symbol}".Trim(),
            $"목표      {p.TargetYear}년 말 (입력일 {p.InputDate:yyyy-MM-dd})",
            p.IsDirectTargetPrice
                ? $"목표 주가 {p.TargetPrice:N2} (직접 입력)"
                : $"목표 주가 EPS {p.Eps:0.###} × PER {p.Per:0.###} = {p.TargetPrice:N2}",
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
        AddAmountLines(lines, row);
        if (row.Quote?.SplitNote is { } note)
            lines.Add($"※ {note}");
        if (!string.IsNullOrEmpty(p.Memo))
            lines.Add($"메모: {p.Memo}");
        Summary = string.Join(Environment.NewLine, lines);
        RaiseFormAmountPreviews(); // 입력칸 아래 주식 수는 선택한 목표의 현재 매입 목표가를 쓴다

        DividendPeriods.Clear();
        DividendSummary = "배당 이력 조회 중...";
        try
        {
            var d = await _service.GetDividendYieldAsync(p.Symbol, force);
            if (id != _detailLoadId)
                return; // 조회 중 선택이 바뀌었거나 같은 목표를 다시 불러오기 시작했다(저장 직후 등)
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
            if (id == _detailLoadId)
                DividendSummary = "배당 이력 조회 실패: " + e.Message;
        }
    }

    /// <summary>단계별 매수금액(원 · 달러)과 그 단계 매수단가에 살 주식 수(내림). 현재 판정 단계에 ◀ 표시.</summary>
    private void AddAmountLines(List<string> lines, TargetRowViewModel row)
    {
        var a = row.Amounts;
        if (a.IsEmpty)
            return;
        lines.Add(UsdKrw is { } r ? $"매수금액 (환율 {r:#,0.00}원)" : "매수금액 (환율 조회 전 — 원화만 표시)");
        var stages = new[] { (BuyStatus.Buy, a.BuyKrw), (BuyStatus.MustBuy, a.MustBuyKrw), (BuyStatus.StrongBuy, a.StrongBuyKrw) };
        foreach (var (stage, krw) in stages)
        {
            if (krw is not { } k || k <= 0)
                continue;
            var label = stage.ToText();
            var text = $"  {label}{new string(' ', 9 - 2 * label.Length)}{Money.Text(k, UsdKrw)}"; // 한글은 2칸 폭
            if (row.SharesText(stage, k) is { } shares)
                text += $" → {shares}";
            if (row.Plan.BuyAmounts.IsDefault(stage, Defaults))
                text += " (기본)";
            if (row.Error is null && row.Status == stage)
                text += "  ◀";
            lines.Add(text);
        }
    }

    /// <summary>
    /// 매수금액 입력칸 아래 달러 환산 미리보기. stage를 주면(목표 입력칸) 기본값을 따르는지 개별 저장인지도 표시한다.
    /// </summary>
    private string AmountPreview(string text, BuyStatus? stage)
    {
        try
        {
            var defaultKrw = stage is { } s ? Defaults.For(s) : null;
            if (Money.ParseKrw(text, "") is not { } krw)
                return defaultKrw is { } d ? $"비우면 기본 {Money.KrwText(d)}" : "";
            if (krw == 0)
                return stage is null ? "0 = 이 단계는 사지 않음" : "0 = 사지 않음 · 개별 저장";
            var preview = Money.KrwToUsd(krw, UsdKrw) is { } usd
                ? $"{Money.KrwText(krw)} ≈ {Money.UsdText(usd)}"
                : $"{Money.KrwText(krw)} (환율 조회 전)";
            if (stage is not { } st)
                return preview;
            // 선택한 목표가 있으면 그 단계 매수단가에 살 주식 수(내림)
            // 선택한 목표가 있으면 둘째 줄에 그 단계 매수단가에 살 주식 수(내림)
            var tag = krw == defaultKrw ? "기본" : "개별 저장";
            return Selected?.SharesText(st, krw) is { } shares
                ? $"{preview}{Environment.NewLine}{shares} · {tag}"
                : $"{preview} · {tag}";
        }
        catch (ArgumentException)
        {
            return "금액 형식 오류";
        }
    }

    /// <summary>목표 입력칸의 매수금액 = 목표에 저장한 값, 없는 단계는 기본 매수금액.</summary>
    private void FillFormAmounts(BuyAmounts saved)
    {
        var a = saved.WithDefaults(Defaults);
        FormBuyKrw = KrwInput(a.BuyKrw);
        FormMustBuyKrw = KrwInput(a.MustBuyKrw);
        FormStrongBuyKrw = KrwInput(a.StrongBuyKrw);
    }

    private double? EpsPerPrice() =>
        double.TryParse(FormEps?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var eps) &&
        double.TryParse(FormPer?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var per) &&
        eps > 0 && per > 0
            ? eps * per
            : null;

    private static string KrwInput(double? krw) => krw?.ToString("#,0", CultureInfo.InvariantCulture) ?? "";

    private static double ParseDouble(string text, string name) =>
        double.TryParse(text?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
            ? v
            : throw new ArgumentException($"{name} 값이 올바르지 않습니다: '{text}'");

    private static int ParseInt(string text, string name) =>
        int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)
            ? v
            : throw new ArgumentException($"{name} 값이 올바르지 않습니다: '{text}'");
}
