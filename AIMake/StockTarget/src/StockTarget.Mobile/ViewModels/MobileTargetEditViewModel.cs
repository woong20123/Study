using StockTarget.Core;

namespace StockTarget.Mobile.ViewModels;

[QueryProperty(nameof(TargetSymbol), "Symbol")]
public sealed class MobileTargetEditViewModel : ObservableObject
{
    private readonly StockDatabase _db;
    private readonly StockService _stockService;
    private readonly MobileMainViewModel _mainVm;

    private string _symbol = "";
    private int _targetYear = DateTime.Today.Year + 4;
    private bool _useDirectPrice;
    private string _targetPrice = "";
    private string _eps = "";
    private string _per = "";
    private string _returnPct = "10";
    private string _dividendYieldPct = "";
    private string _buyKrw = "";
    private string _mustBuyKrw = "";
    private string _strongBuyKrw = "";
    private string _memo = "";
    private string _message = "";
    private bool _isBusy;

    public string Symbol
    {
        get => _symbol;
        set => Set(ref _symbol, value?.ToUpperInvariant() ?? "");
    }

    public int TargetYear
    {
        get => _targetYear;
        set => Set(ref _targetYear, value);
    }

    public bool UseDirectPrice
    {
        get => _useDirectPrice;
        set
        {
            if (Set(ref _useDirectPrice, value))
                OnPropertyChanged(nameof(UseEpsPer));
        }
    }

    public bool UseEpsPer => !UseDirectPrice;

    public string TargetPrice
    {
        get => _targetPrice;
        set => Set(ref _targetPrice, value);
    }

    public string Eps
    {
        get => _eps;
        set => Set(ref _eps, value);
    }

    public string Per
    {
        get => _per;
        set => Set(ref _per, value);
    }

    public string ReturnPct
    {
        get => _returnPct;
        set => Set(ref _returnPct, value);
    }

    public string DividendYieldPct
    {
        get => _dividendYieldPct;
        set => Set(ref _dividendYieldPct, value);
    }

    public string BuyKrw
    {
        get => _buyKrw;
        set => Set(ref _buyKrw, value);
    }

    public string MustBuyKrw
    {
        get => _mustBuyKrw;
        set => Set(ref _mustBuyKrw, value);
    }

    public string StrongBuyKrw
    {
        get => _strongBuyKrw;
        set => Set(ref _strongBuyKrw, value);
    }

    public string Memo
    {
        get => _memo;
        set => Set(ref _memo, value);
    }

    public string Message
    {
        get => _message;
        set => Set(ref _message, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => Set(ref _isBusy, value);
    }

    public string TargetSymbol
    {
        set => LoadTarget(value);
    }

    public AsyncCommand AutoFetchDividendCommand { get; }
    public AsyncCommand SaveCommand { get; }

    public MobileTargetEditViewModel(StockDatabase db, StockService stockService, MobileMainViewModel mainVm)
    {
        _db = db;
        _stockService = stockService;
        _mainVm = mainVm;

        AutoFetchDividendCommand = new AsyncCommand(AutoFetchDividendAsync);
        SaveCommand = new AsyncCommand(SaveAsync);

        // 기본 매수금액으로 초기값 채우기
        var defaults = _db.GetDefaultAmounts();
        if (defaults.BuyKrw is { } b) BuyKrw = $"{b:N0}";
        if (defaults.MustBuyKrw is { } mb) MustBuyKrw = $"{mb:N0}";
        if (defaults.StrongBuyKrw is { } sb) StrongBuyKrw = $"{sb:N0}";
    }

    private void LoadTarget(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol)) return;
        var plan = _db.GetTargets().FirstOrDefault(t => t.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        if (plan is null) return;

        Symbol = plan.Symbol;
        TargetYear = plan.TargetYear;
        UseDirectPrice = plan.TargetPriceInput is not null;
        TargetPrice = plan.TargetPriceInput?.ToString() ?? "";
        Eps = plan.Eps.ToString();
        Per = plan.Per.ToString();
        ReturnPct = plan.ReturnPct.ToString();
        DividendYieldPct = plan.DividendYieldPct.ToString("F2");
        Memo = plan.Memo ?? "";

        var defaults = _db.GetDefaultAmounts();
        var amounts = plan.EffectiveAmounts(defaults);
        if (amounts.BuyKrw is { } b) BuyKrw = $"{b:N0}";
        if (amounts.MustBuyKrw is { } mb) MustBuyKrw = $"{mb:N0}";
        if (amounts.StrongBuyKrw is { } sb) StrongBuyKrw = $"{sb:N0}";
    }

    private async Task AutoFetchDividendAsync()
    {
        if (string.IsNullOrWhiteSpace(Symbol))
        {
            Message = "티커를 먼저 입력하세요.";
            return;
        }

        IsBusy = true;
        Message = "5년 배당수익률 조회 중...";
        try
        {
            var div = await _stockService.GetDividendYieldAsync(Symbol, force: false);
            DividendYieldPct = div.Value.AvgYieldPct.ToString("F2");
            Message = $"배당수익률 조회 완료: {DividendYieldPct}%";
        }
        catch (Exception ex)
        {
            Message = $"배당수익률 조회 실패: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Symbol))
        {
            Message = "티커를 입력하세요.";
            return;
        }

        IsBusy = true;
        try
        {
            double? directPrice = null;
            double eps = 0, per = 0;

            if (UseDirectPrice)
            {
                if (!double.TryParse(TargetPrice, out var p) || p <= 0)
                    throw new ArgumentException("유효한 목표 주가를 입력하세요.");
                directPrice = p;
            }
            else
            {
                if (!double.TryParse(Eps, out eps) || eps <= 0)
                    throw new ArgumentException("유효한 목표 EPS를 입력하세요.");
                if (!double.TryParse(Per, out per) || per <= 0)
                    throw new ArgumentException("유효한 목표 PER을 입력하세요.");
            }

            if (!double.TryParse(ReturnPct, out var ret) || ret <= 0)
                throw new ArgumentException("유효한 목표 수익률(%)을 입력하세요.");

            double divYield;
            if (string.IsNullOrWhiteSpace(DividendYieldPct))
            {
                var div = await _stockService.GetDividendYieldAsync(Symbol, force: false);
                divYield = div.Value.AvgYieldPct;
            }
            else if (!double.TryParse(DividendYieldPct, out divYield))
            {
                throw new ArgumentException("유효한 배당수익률(%)을 입력하세요.");
            }

            var defaults = _db.GetDefaultAmounts();
            var inputAmounts = new BuyAmounts(
                Money.ParseKrw(BuyKrw, "매수금액"),
                Money.ParseKrw(MustBuyKrw, "필수매수금액"),
                Money.ParseKrw(StrongBuyKrw, "강력매수금액"));

            var amountsToSave = inputAmounts.ExceptDefaults(defaults);

            var plan = new TargetPlan(
                Symbol: Symbol,
                InputDate: DateOnly.FromDateTime(DateTime.Today),
                TargetYear: TargetYear,
                Eps: eps,
                Per: per,
                ReturnPct: ret,
                DividendYieldPct: divYield,
                Memo: string.IsNullOrWhiteSpace(Memo) ? null : Memo.Trim(),
                Amounts: amountsToSave,
                TargetPriceInput: directPrice);

            _db.SaveTarget(plan);
            await _mainVm.LoadInitialDataAsync();
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            Message = $"저장 실패: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
