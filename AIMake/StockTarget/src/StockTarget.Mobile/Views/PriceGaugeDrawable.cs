using StockTarget.Core;
using StockTarget.Mobile.Converters;

namespace StockTarget.Mobile.Views;

/// <summary>
/// 목록 카드의 가격 게이지. 이번 분기 매입 목표가(세로선) 대비 −30% ~ +30%를 매수 단계 구간 색(진할수록 강한 매수)과
/// 대기 구간 색으로 칠하고, 현재가 위치에 동그라미를 찍는다. 범위를 벗어나면 양 끝에 붙인다.
/// 괴리가 없으면(시세 전 · 조회 실패) 막대만 그린다. 색은 언어별 판정 색(<see cref="StatusPalette"/>)을 따른다.
/// </summary>
public sealed class PriceGaugeDrawable(double? gapPct, BuyStatus status) : IDrawable
{
    public double? GapPct => gapPct;
    public BuyStatus Status => status;

    private const double Min = -30, Max = 30;
    private const float TrackHeight = 6, DotRadius = 6;

    // (시작 %, 끝 %, 구간 단계) — 3 강력매수 · 2 필수매수 · 1 매수 · 0 대기(매입 대기 포함)
    private static readonly (double From, double To, int Level)[] Zones =
    [
        (Min, -TargetCalculator.StrongBuyPct, 3),
        (-TargetCalculator.StrongBuyPct, -TargetCalculator.MustBuyPct, 2),
        (-TargetCalculator.MustBuyPct, 0, 1),
        (0, Max, 0),
    ];

    public void Draw(ICanvas canvas, RectF rect)
    {
        // 동그라미가 잘리지 않게 양옆을 반지름만큼 비운다
        var left = rect.Left + DotRadius;
        var width = rect.Width - DotRadius * 2;
        var y = rect.Center.Y - TrackHeight / 2;
        float X(double g) => left + (float)((Math.Clamp(g, Min, Max) - Min) / (Max - Min)) * width;

        canvas.SaveState();
        var clip = new PathF();
        clip.AppendRoundedRectangle(left, y, width, TrackHeight, TrackHeight / 2);
        canvas.ClipPath(clip);
        foreach (var (from, to, level) in Zones)
        {
            canvas.FillColor = StatusPalette.Zone(level);
            canvas.FillRectangle(X(from), y, X(to) - X(from), TrackHeight);
        }
        canvas.RestoreState();

        // 매입 목표가 기준선
        canvas.StrokeColor = Color.FromArgb("#0F172A");
        canvas.StrokeSize = 2;
        canvas.DrawLine(X(0), rect.Top + 1, X(0), rect.Bottom - 1);

        if (gapPct is not { } gap)
            return;
        var cx = X(gap);
        canvas.FillColor = Colors.White;
        canvas.FillCircle(cx, rect.Center.Y, DotRadius);
        canvas.StrokeColor = StatusPalette.Accent(status);
        canvas.StrokeSize = 3;
        canvas.DrawCircle(cx, rect.Center.Y, DotRadius - 1.5f);
    }
}
