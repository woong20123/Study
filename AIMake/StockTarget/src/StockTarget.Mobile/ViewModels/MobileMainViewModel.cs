using System.Collections.ObjectModel;
using StockTarget.Core;
using StockTarget.Mobile.Localization;
using StockTarget.Mobile.Views;

namespace StockTarget.Mobile.ViewModels;

public sealed class MobileMainViewModel : ObservableObject
{
    private readonly StockDatabase _db;
    private readonly StockService _stockService;
    private readonly StockNameService _names;

    private bool _isBusy;
    private bool _isRefreshing;
    private string _status = L.Get("Status_Ready");
    private double? _usdKrw;
    private BuyAmounts _defaults = BuyAmounts.Empty;

    public ObservableCollection<MobileTargetRowViewModel> Targets { get; } = [];

    /// <summary>화면에 보이는 목표(필터 적용). 시세를 받은 뒤 판정이 바뀌면 <see cref="ApplyFilter"/>로 다시 고른다.</summary>
    public ObservableCollection<MobileTargetRowViewModel> VisibleTargets { get; } = [];

    private const string OnlyBuyKey = "filter_only_buy";
    private bool _onlyBuy = Preferences.Default.Get(OnlyBuyKey, false);

    /// <summary>매수 · 필수매수 · 강력매수 단계인 목표만 보기. 다음 실행에도 유지한다.</summary>
    public bool OnlyBuy
    {
        get => _onlyBuy;
        set
        {
            if (!Set(ref _onlyBuy, value))
                return;
            Preferences.Default.Set(OnlyBuyKey, value);
            ApplyFilter();
        }
    }

    public string OnlyBuyText => L.Format("Main_OnlyBuy", Targets.Count(r => NeedsBuy(r.Status)));

    /// <summary>목록이 비었을 때 문구. 필터 때문에 비었으면 그렇게 알린다.</summary>
    public string EmptyText => OnlyBuy && Targets.Count > 0 ? L.Get("Main_EmptyFiltered") : L.Get("Main_Empty");

    public bool ShowAddFirst => Targets.Count == 0;

    private static bool NeedsBuy(BuyStatus s) => s is BuyStatus.Buy or BuyStatus.MustBuy or BuyStatus.StrongBuy;

    public void ApplyFilter()
    {
        VisibleTargets.Clear();
        foreach (var row in Targets)
            if (!OnlyBuy || NeedsBuy(row.Status))
                VisibleTargets.Add(row);
        OnPropertyChanged(nameof(OnlyBuyText));
        OnPropertyChanged(nameof(EmptyText));
        OnPropertyChanged(nameof(ShowAddFirst));
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => Set(ref _isBusy, value);
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => Set(ref _isRefreshing, value);
    }

    public string Status
    {
        get => _status;
        set => Set(ref _status, value);
    }

    public double? UsdKrw
    {
        get => _usdKrw;
        private set
        {
            if (Set(ref _usdKrw, value))
            {
                OnPropertyChanged(nameof(UsdKrwText));
                foreach (var row in Targets)
                    row.UsdKrw = value;
            }
        }
    }

    public string UsdKrwText => UsdKrw is { } r ? L.Format("Main_UsdKrw", r) : L.Get("Main_UsdKrwNone");

    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand AddTargetCommand { get; }
    public AsyncCommand<MobileTargetRowViewModel> SelectTargetCommand { get; }
    public AsyncCommand<MobileTargetRowViewModel> DeleteTargetCommand { get; }
    public AsyncCommand ImportJsonCommand { get; }

    public MobileMainViewModel(StockDatabase db, StockService stockService, StockNameService names)
    {
        _db = db;
        _stockService = stockService;
        _names = names;

        RefreshCommand = new AsyncCommand(RefreshAsync);
        AddTargetCommand = new AsyncCommand(NavigateToAddAsync);
        SelectTargetCommand = new AsyncCommand<MobileTargetRowViewModel>(NavigateToDetailAsync);
        DeleteTargetCommand = new AsyncCommand<MobileTargetRowViewModel>(DeleteAsync);
        ImportJsonCommand = new AsyncCommand(ImportJsonAsync);

        _ = LoadInitialDataAsync();
    }

    /// <summary>마지막으로 시작한 불러오기 번호. 언어 전환 등으로 겹치면 마지막 것만 상태를 바꾼다.</summary>
    private int _loadId;

    public async Task LoadInitialDataAsync()
    {
        var id = ++_loadId;
        IsBusy = true;
        Status = L.Get("Main_Loading");
        try
        {
            _defaults = _db.GetDefaultAmounts();
            var plans = _db.GetTargets();
            var names = _names.CachedNames(); // 캐시한 한글명을 먼저 보여주고, 시세 갱신 때 7일 지난 것만 다시 받는다
            Targets.Clear();
            foreach (var plan in plans)
            {
                Targets.Add(new MobileTargetRowViewModel(plan)
                {
                    Defaults = _defaults,
                    UsdKrw = UsdKrw,
                    KoreanName = names.GetValueOrDefault(plan.Symbol),
                    SelectCommand = SelectTargetCommand
                });
            }
            ApplyFilter(); // 시세 전에도 목록을 먼저 보여준다(필터 중이면 시세를 받은 뒤 채워진다)

            await RefreshQuotesAsync(force: false);
            if (id == _loadId)
                Status = L.Format("Main_TargetCount", Targets.Count);
        }
        catch (Exception ex)
        {
            if (id == _loadId)
                Status = L.Format("Main_LoadFailed", ex.Message);
        }
        finally
        {
            if (id == _loadId)
                IsBusy = false;
        }
    }

    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        try
        {
            await RefreshQuotesAsync(force: true);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    public async Task RefreshQuotesAsync(bool force)
    {
        try
        {
            var usdQuote = await _stockService.GetUsdKrwAsync(force);
            UsdKrw = usdQuote.Value.Price;
        }
        catch
        {
            // 환율 조회 실패 시 기존 값 유지
        }

        var tasks = Targets.Select(async row =>
        {
            try
            {
                var cached = await _stockService.GetQuoteAsync(row.Symbol, force);
                row.Quote = cached.Value;
                row.CacheAge = cached.CacheAgeSeconds;
                row.Error = null;

                _stockService.RecordCheck(row.Plan, cached.Value);
            }
            catch (Exception ex)
            {
                row.Error = ex.Message;
            }
            if (L.IsKorean) // 영어 기기는 시세의 영문명을 쓰므로 네이버를 조회하지 않는다
                row.KoreanName = await _names.GetNameAsync(row.Symbol);
        });

        await Task.WhenAll(tasks);
        ApplyFilter(); // 새 시세로 판정이 바뀌었을 수 있다
    }

    private async Task NavigateToAddAsync()
    {
        await Shell.Current.GoToAsync(nameof(TargetEditPage));
    }

    private async Task NavigateToDetailAsync(MobileTargetRowViewModel? row)
    {
        if (row is null) return;
        var navigationParameter = new Dictionary<string, object>
        {
            { "TargetRow", row }
        };
        await Shell.Current.GoToAsync(nameof(TargetDetailPage), navigationParameter);
    }

    /// <summary>
    /// 백업 JSON 파일(데스크톱 'JSON으로 내보내기'와 같은 형식)을 골라 현재 데이터를 통째로 바꾼다.
    /// 바꾸기 전에 현재 데이터를 앱 폴더 backups\ 에 자동 백업한다.
    /// </summary>
    private async Task ImportJsonAsync()
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = L.Get("Main_ImportJson") });
            if (file is null)
                return;

            BackupData backup;
            await using (var stream = await file.OpenReadAsync())
            using (var reader = new StreamReader(stream))
                backup = BackupSerializer.FromJson(await reader.ReadToEndAsync());
            var targets = backup.ToTargets(); // 값 검사(잘못된 백업이면 여기서 예외, DB는 그대로)
            backup.ToDefaultAmounts();

            var ok = await Shell.Current.DisplayAlert(L.Get("Main_ImportTitle"),
                L.Format("Main_ImportMessage", file.FileName, backup.ExportedAt.ToLocalTime(), targets.Count, Targets.Count),
                L.Get("Main_ImportOk"), L.Get("Main_DeleteCancel"));
            if (!ok)
                return;

            var folder = Path.Combine(FileSystem.AppDataDirectory, "backups");
            Directory.CreateDirectory(folder);
            var autoPath = Path.Combine(folder, $"before-import-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            await File.WriteAllTextAsync(autoPath, BackupSerializer.ToJson(_db.ExportBackup()));

            _db.ReplaceWithBackup(backup);
            await LoadInitialDataAsync();
            Status = L.Format("Main_Imported", targets.Count);
        }
        catch (Exception e) when (e is BackupFormatException or IOException or ArgumentException)
        {
            await Shell.Current.DisplayAlert(L.Get("Main_ImportFailed"), e.Message, "OK");
        }
    }

    private async Task DeleteAsync(MobileTargetRowViewModel? row)
    {
        if (row is null) return;
        bool confirm = await Shell.Current.DisplayAlert(L.Get("Main_DeleteTitle"), L.Format("Main_DeleteMessage", row.Title), L.Get("Main_DeleteOk"), L.Get("Main_DeleteCancel"));
        if (!confirm) return;

        _db.DeleteTarget(row.Symbol);
        Targets.Remove(row);
        ApplyFilter();
        Status = L.Format("Main_Deleted", row.Title);
    }
}
