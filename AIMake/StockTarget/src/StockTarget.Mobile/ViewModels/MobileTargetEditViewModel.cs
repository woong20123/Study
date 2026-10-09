using StockTarget.Core;
using StockTarget.Mobile.Localization;

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

    /// <summary>
    /// 금액 칸에 미리 채운 글자와 그 원화 값. 영어 화면은 달러로 바꿔 보여주므로, 손대지 않은 칸은 원래 원화 값을 그대로 저장해
    /// 환율 왕복 반올림으로 기본 매수금액과 달라지지 않게 한다.
    /// </summary>
    private readonly Dictionary<BuyStatus, (string Text, double Krw)> _prefilled = [];

    public string Symbol
    {
        get => _symbol;
        set => Set(ref _symbol, value ?? "");
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
        _ = FillAmountsAsync(_db.GetDefaultAmounts());
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

        _ = FillAmountsAsync(plan.EffectiveAmounts(_db.GetDefaultAmounts()));
    }

    private async Task AutoFetchDividendAsync()
    {
        if (string.IsNullOrWhiteSpace(Symbol))
        {
            Message = L.Get("Edit_NeedSymbolFirst");
            return;
        }

        IsBusy = true;
        Message = L.Get("Edit_FetchingDividend");
        try
        {
            var div = await _stockService.GetDividendYieldAsync(Symbol, force: false);
            DividendYieldPct = div.Value.AvgYieldPct.ToString("F2");
            Message = L.Format("Edit_DividendDone", DividendYieldPct);
        }
        catch (Exception ex)
        {
            Message = L.Format("Edit_DividendFailed", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>원/달러 환율. 메인 화면이 받아 둔 값을 쓰고, 없으면 조회한다.</summary>
    private async Task<double> UsdKrwAsync() =>
        _mainVm.UsdKrw ?? (await _stockService.GetUsdKrwAsync(force: false)).Value.Price;

    /// <summary>
    /// 단계별 금액 칸 채우기. 금액은 원화로 저장하고, 한국어 화면은 원화("1,000,000"),
    /// 영어 화면은 지금 환율로 바꾼 달러("746")로 보여준다.
    /// </summary>
    private int _fillId;

    private async Task FillAmountsAsync(BuyAmounts amounts)
    {
        var id = ++_fillId; // 기본값 채우기와 목표 불러오기가 겹치면 나중 것만 쓴다
        double? rate = null;
        if (!L.IsKorean)
        {
            try { rate = await UsdKrwAsync(); }
            catch { return; } // 환율을 못 받으면 원화를 보이지 않도록 비워 둔다
        }
        if (id != _fillId)
            return;
        _prefilled.Clear();
        string Show(BuyStatus stage, double? krw)
        {
            if (krw is not { } k)
                return "";
            var text = rate is { } r ? $"{k / r:N0}" : $"{k:N0}";
            _prefilled[stage] = (text, k);
            return text;
        }
        BuyKrw = Show(BuyStatus.Buy, amounts.BuyKrw);
        MustBuyKrw = Show(BuyStatus.MustBuy, amounts.MustBuyKrw);
        StrongBuyKrw = Show(BuyStatus.StrongBuy, amounts.StrongBuyKrw);
    }

    /// <summary>
    /// 금액 칸 → 원화. 미리 채운 그대로면 원래 원화 값, 한국어 화면은 원화("100만" 등), 영어 화면은 달러를 환율로 바꾼다.
    /// 잘못된 값은 화면 언어로 알린다.
    /// </summary>
    private double? ParseAmount(string text, BuyStatus stage, double? usdKrw)
    {
        if (_prefilled.TryGetValue(stage, out var p) && p.Text == text.Trim())
            return p.Krw;
        try
        {
            if (usdKrw is not { } rate)
                return Money.ParseKrw(text, stage.ToText());
            if (string.IsNullOrWhiteSpace(text))
                return null;
            var s = text.Replace(",", "").Replace("$", "").Trim();
            if (!double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var usd)
                || usd < 0 || double.IsInfinity(usd))
                throw new ArgumentException();
            return Math.Round(usd * rate);
        }
        catch (ArgumentException)
        {
            throw new ArgumentException(L.Format("Edit_InvalidAmount", L.Verdict(stage), text));
        }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Symbol))
        {
            Message = L.Get("Edit_NeedSymbol");
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
                    throw new ArgumentException(L.Get("Edit_InvalidPrice"));
                directPrice = p;
            }
            else
            {
                if (!double.TryParse(Eps, out eps) || eps <= 0)
                    throw new ArgumentException(L.Get("Edit_InvalidEps"));
                if (!double.TryParse(Per, out per) || per <= 0)
                    throw new ArgumentException(L.Get("Edit_InvalidPer"));
            }

            if (!double.TryParse(ReturnPct, out var ret) || ret <= 0)
                throw new ArgumentException(L.Get("Edit_InvalidReturn"));

            double divYield;
            if (string.IsNullOrWhiteSpace(DividendYieldPct))
            {
                var div = await _stockService.GetDividendYieldAsync(Symbol, force: false);
                divYield = div.Value.AvgYieldPct;
            }
            else if (!double.TryParse(DividendYieldPct, out divYield))
            {
                throw new ArgumentException(L.Get("Edit_InvalidDividend"));
            }

            var defaults = _db.GetDefaultAmounts();
            double? usdKrw = L.IsKorean ? null : await UsdKrwAsync(); // 영어 화면은 달러로 입력받는다
            var inputAmounts = new BuyAmounts(
                ParseAmount(BuyKrw, BuyStatus.Buy, usdKrw),
                ParseAmount(MustBuyKrw, BuyStatus.MustBuy, usdKrw),
                ParseAmount(StrongBuyKrw, BuyStatus.StrongBuy, usdKrw));

            var amountsToSave = inputAmounts.ExceptDefaults(defaults);

            var plan = new TargetPlan(
                Symbol: StockService.Normalize(Symbol),
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
            Message = L.Format("Edit_SaveFailed", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
