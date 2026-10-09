using System.Windows;
using StockTarget.Core;

namespace StockTarget.App.ViewModels;

/// <summary>
/// 기본 매수금액: 목표에서 매수금액을 비운 단계에 공통으로 쓰는 금액(원).
/// 목표에 직접 입력한 단계는 그 값이 우선하고, 0을 입력하면 그 단계는 사지 않는다.
/// </summary>
public sealed partial class MainViewModel
{
    private BuyAmounts _defaults = BuyAmounts.Empty;
    private string _defaultBuyKrw = "";
    private string _defaultMustBuyKrw = "";
    private string _defaultStrongBuyKrw = "";

    public RelayCommand SaveDefaultsCommand { get; private set; } = null!;
    public RelayCommand RevertDefaultsCommand { get; private set; } = null!;

    /// <summary>저장된 기본 매수금액.</summary>
    public BuyAmounts Defaults
    {
        get => _defaults;
        private set
        {
            if (!Set(ref _defaults, value))
                return;
            foreach (var row in Targets)
                row.Defaults = value;
            OnPropertyChanged(nameof(DefaultsSummary));
            RaiseFormAmountPreviews();
        }
    }

    public string DefaultBuyKrw
    {
        get => _defaultBuyKrw;
        set
        {
            if (Set(ref _defaultBuyKrw, value))
                RaiseDefaultsEdited(nameof(DefaultBuyUsd));
        }
    }

    public string DefaultMustBuyKrw
    {
        get => _defaultMustBuyKrw;
        set
        {
            if (Set(ref _defaultMustBuyKrw, value))
                RaiseDefaultsEdited(nameof(DefaultMustBuyUsd));
        }
    }

    public string DefaultStrongBuyKrw
    {
        get => _defaultStrongBuyKrw;
        set
        {
            if (Set(ref _defaultStrongBuyKrw, value))
                RaiseDefaultsEdited(nameof(DefaultStrongBuyUsd));
        }
    }

    public string DefaultBuyUsd => AmountPreview(DefaultBuyKrw, null);
    public string DefaultMustBuyUsd => AmountPreview(DefaultMustBuyKrw, null);
    public string DefaultStrongBuyUsd => AmountPreview(DefaultStrongBuyKrw, null);

    /// <summary>입력칸 값이 저장된 기본 매수금액과 다른지(저장 안 된 변경).</summary>
    public bool DefaultsDirty => TryParseDefaults() is not { } a || a != Defaults;

    public string DefaultsSummary
    {
        get
        {
            if (Defaults.IsEmpty)
                return "설정 안 함 — 목표마다 입력한 금액만 사용";
            var custom = Targets.Count(r => !r.Plan.BuyAmounts.IsEmpty);
            return $"목표 {Targets.Count}개 중 {Targets.Count - custom}개는 기본값만 사용, {custom}개는 일부 단계를 개별 금액으로 저장";
        }
    }

    private void InitDefaults()
    {
        SaveDefaultsCommand = new RelayCommand(SaveDefaults, () => !IsBusy && DefaultsDirty);
        RevertDefaultsCommand = new RelayCommand(LoadDefaults, () => DefaultsDirty);
        LoadDefaults();
    }

    /// <summary>DB의 기본 매수금액을 읽어 입력칸과 목록에 반영한다(시작 · 복원 후 · 되돌리기).</summary>
    private void LoadDefaults()
    {
        Defaults = _db.GetDefaultAmounts();
        DefaultBuyKrw = KrwInput(Defaults.BuyKrw);
        DefaultMustBuyKrw = KrwInput(Defaults.MustBuyKrw);
        DefaultStrongBuyKrw = KrwInput(Defaults.StrongBuyKrw);
        OnPropertyChanged(nameof(DefaultsDirty));
    }

    private void SaveDefaults()
    {
        try
        {
            var amounts = ParseDefaults();
            var old = Defaults;
            _db.SaveDefaultAmounts(amounts);
            RefillFormAmounts(old, amounts);
            LoadDefaults(); // 입력칸을 "1,000,000" 형식으로 정리
            OnPropertyChanged(nameof(DefaultsSummary));
            if (Selected is { } row)
                _ = LoadDetailAsync(row, force: false);
            Status = amounts.IsEmpty
                ? "기본 매수금액 해제"
                : $"기본 매수금액 저장: {DefaultsText(amounts)}";
        }
        catch (ArgumentException e)
        {
            Status = "기본 매수금액 저장 실패: " + e.Message;
            MessageBox.Show(e.Message, "기본 매수금액 저장 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>
    /// 기본 매수금액이 바뀌면 목표 입력칸에서 이전 기본값을 그대로 두고 있던(또는 비운) 단계를 새 기본값으로 바꾼다.
    /// 사용자가 다른 값으로 고쳐 둔 단계는 건드리지 않는다.
    /// </summary>
    private void RefillFormAmounts(BuyAmounts old, BuyAmounts updated)
    {
        string Refill(string text, BuyStatus s)
        {
            double? v;
            try
            {
                v = Money.ParseKrw(text, "");
            }
            catch (ArgumentException)
            {
                return text;
            }
            return v is null || v == old.For(s) ? KrwInput(updated.For(s)) : text;
        }
        FormBuyKrw = Refill(FormBuyKrw, BuyStatus.Buy);
        FormMustBuyKrw = Refill(FormMustBuyKrw, BuyStatus.MustBuy);
        FormStrongBuyKrw = Refill(FormStrongBuyKrw, BuyStatus.StrongBuy);
    }

    private BuyAmounts ParseDefaults() => new(
        Money.ParseKrw(DefaultBuyKrw, "기본 매수 1단계"),
        Money.ParseKrw(DefaultMustBuyKrw, "기본 매수 2단계"),
        Money.ParseKrw(DefaultStrongBuyKrw, "기본 매수 3단계"));

    private BuyAmounts? TryParseDefaults()
    {
        try
        {
            return ParseDefaults();
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private void RaiseDefaultsEdited(string previewName)
    {
        OnPropertyChanged(previewName);
        OnPropertyChanged(nameof(DefaultsDirty));
    }

    private void RaiseFormAmountPreviews()
    {
        OnPropertyChanged(nameof(FormBuyUsd));
        OnPropertyChanged(nameof(FormMustBuyUsd));
        OnPropertyChanged(nameof(FormStrongBuyUsd));
        OnPropertyChanged(nameof(DefaultBuyUsd));
        OnPropertyChanged(nameof(DefaultMustBuyUsd));
        OnPropertyChanged(nameof(DefaultStrongBuyUsd));
    }

    private static string DefaultsText(BuyAmounts a) => string.Join(" / ",
        new[] { BuyStatus.Buy, BuyStatus.MustBuy, BuyStatus.StrongBuy }
            .Where(s => a.For(s) is not null)
            .Select(s => $"{s.ToText()} {Money.KrwText(a.For(s)!.Value)}"));
}
