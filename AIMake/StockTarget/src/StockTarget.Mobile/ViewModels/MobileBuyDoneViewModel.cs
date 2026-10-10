using System.Collections.ObjectModel;
using StockTarget.Mobile.Localization;

namespace StockTarget.Mobile.ViewModels;

/// <summary>매수 완료 관리 화면 한 행. 스위치를 켜고 끄면 바로 저장한다.</summary>
public sealed class BuyDoneItem(MobileTargetRowViewModel row, MobileMainViewModel main) : ObservableObject
{
    public MobileTargetRowViewModel Row { get; } = row;

    public string Title => Row.Title;
    public string Subtitle => Row.BuyDoneSubtitle;

    public bool IsDone
    {
        get => Row.IsBuyDone;
        set
        {
            if (Row.IsBuyDone == value)
                return;
            main.SetBuyDone(Row, value);
            OnPropertyChanged();
        }
    }
}

/// <summary>
/// 매수 완료 관리: 지금 매수 단계인 종목과 이미 매수 완료로 표시한 종목을 모아 켜고 끈다.
/// 켠 종목은 메인 화면 '매수 필요만' 필터에서 빠진다(예약 주문표에서도 빠진다 — 같은 buy_done 표).
/// </summary>
public sealed class MobileBuyDoneViewModel(MobileMainViewModel main) : ObservableObject
{
    public ObservableCollection<BuyDoneItem> Items { get; } = [];

    public string Summary => L.Format("BuyDone_Summary", Items.Count(i => i.IsDone), Items.Count);

    /// <summary>
    /// 목록을 다시 모은다. 화면에 들어올 때만 부르므로, 스위치를 꺼도(매수 단계가 아니어도) 그 행이 바로 사라지지 않는다.
    /// 매수 단계를 먼저, 그다음 매수 단계가 아닌데 완료로 남아 있는 종목.
    /// </summary>
    public void Reload()
    {
        foreach (var item in Items)
            item.PropertyChanged -= OnItemChanged;
        Items.Clear();
        foreach (var row in main.Targets.Where(r => r.NeedsBuy).Concat(main.Targets.Where(r => !r.NeedsBuy && r.IsBuyDone)))
        {
            var item = new BuyDoneItem(row, main);
            item.PropertyChanged += OnItemChanged;
            Items.Add(item);
        }
        OnPropertyChanged(nameof(Summary));
    }

    private void OnItemChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) =>
        OnPropertyChanged(nameof(Summary));
}
