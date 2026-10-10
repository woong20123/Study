using System.Collections.ObjectModel;

namespace StockTarget.App.ViewModels;

/// <summary>매수 완료 관리 창 한 행. 체크하면 바로 저장한다.</summary>
public sealed class BuyDoneItem(TargetRowViewModel row, MainViewModel main) : ObservableObject
{
    public TargetRowViewModel Row { get; } = row;

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
/// 켠 종목은 해제할 때까지 예약 주문표에서 빠진다(모바일과 같은 buy_done 표).
/// 창을 열 때 한 번 모으므로, 체크를 풀어도 그 행이 바로 사라지지 않는다.
/// </summary>
public sealed class BuyDoneViewModel : ObservableObject
{
    public ObservableCollection<BuyDoneItem> Items { get; } = [];

    public BuyDoneViewModel(MainViewModel main)
    {
        // 매수 단계를 먼저, 그다음 매수 단계가 아닌데 완료로 남아 있는 종목
        foreach (var row in main.Targets.Where(r => r.NeedsBuy).Concat(main.Targets.Where(r => !r.NeedsBuy && r.IsBuyDone)))
        {
            var item = new BuyDoneItem(row, main);
            item.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Summary));
            Items.Add(item);
        }
    }

    public string Summary => Items.Count == 0
        ? "지금 매수가 필요한 종목이 없습니다."
        : $"매수 완료 {Items.Count(i => i.IsDone)} / {Items.Count}개 — 체크한 종목은 예약 주문표에서 빠지고, 다음 분기가 되면 자동으로 풀립니다";
}
