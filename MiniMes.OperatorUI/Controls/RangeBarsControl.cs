namespace MiniMes.OperatorUI.Controls;

// 변수별 양품 조업범위(막대)와 현재 값(마커)을 한눈에 비교하는 컨트롤.
// - 가는 선: 양품의 최소~최대
// - 청록 막대: 양품 5~95% 범위(조업범위)
// - 흰 눈금: 양품 평균
// - 마름모: 현재 값 (범위 안이면 흰색, 밖이면 주황)
// - 보라 마름모: 사용자가 슬라이더로 지정한 가상 조건
public record RangeRow(string Label, string Unit, double GoodMin, double GoodMax, double P05, double P95, double Mean, double? Current);

public class RangeBarsControl : Control
{
    private const int LabelWidth = 150;
    private const int ValueWidth = 170;

    public IReadOnlyList<RangeRow> Rows { get; private set; } = Array.Empty<RangeRow>();
    public int VirtualRow { get; private set; } = -1;
    public double VirtualValue { get; private set; }

    public RangeBarsControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Theme.Card;
        Font = Theme.Font(9);
    }

    public void SetRows(IReadOnlyList<RangeRow> rows)
    {
        Rows = rows;
        VirtualRow = -1;
        Invalidate();
    }

    public void SetVirtual(int row, double value)
    {
        VirtualRow = row;
        VirtualValue = value;
        Invalidate();
    }

    // 막대 축의 표시 범위: 양품 범위 양옆에 25% 여유를 둔다.
    public static (double Lo, double Hi) AxisOf(RangeRow r)
    {
        var span = Math.Max(r.GoodMax - r.GoodMin, Math.Abs(r.Mean) * 0.02 + 1e-6);
        return (r.GoodMin - span * 0.35, r.GoodMax + span * 0.35);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        if (Rows.Count == 0)
        {
            TextRenderer.DrawText(g, "조회된 양품 데이터가 없습니다", Font, ClientRectangle, Theme.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        var rowH = (float)Height / Rows.Count;
        var barLeft = LabelWidth;
        var barRight = Width - ValueWidth;
        var barW = Math.Max(10, barRight - barLeft);

        for (var i = 0; i < Rows.Count; i++)
        {
            var r = Rows[i];
            var top = i * rowH;
            var cy = top + rowH / 2;
            var (lo, hi) = AxisOf(r);
            float X(double v) => barLeft + (float)((v - lo) / (hi - lo) * barW);

            // 라벨
            TextRenderer.DrawText(g, r.Label + (r.Unit.Length > 0 ? $" ({r.Unit})" : ""), Font,
                new Rectangle(0, (int)top, LabelWidth - 8, (int)rowH), Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            // 트랙
            using (var track = new SolidBrush(Color.FromArgb(22, 34, 54)))
                g.FillRectangle(track, barLeft, cy - 11, barW, 22);

            // 양품 최소~최대 (가는 선)
            using (var thin = new Pen(Color.FromArgb(110, 140, 170), 2))
                g.DrawLine(thin, X(r.GoodMin), cy, X(r.GoodMax), cy);

            // 조업범위 막대
            using (var band = new SolidBrush(Color.FromArgb(200, 40, 160, 160)))
                g.FillRectangle(band, X(r.P05), cy - 8, Math.Max(2, X(r.P95) - X(r.P05)), 16);

            // 평균 눈금
            using (var mean = new Pen(Color.White, 2))
                g.DrawLine(mean, X(r.Mean), cy - 11, X(r.Mean), cy + 11);

            // 현재 값
            string valueText = "현재 값 없음";
            if (r.Current is { } cur)
            {
                var outside = cur < r.P05 || cur > r.P95;
                DrawDiamond(g, X(Math.Clamp(cur, lo, hi)), cy, 8, outside ? Theme.Orange : Color.White);
                valueText = $"현재 {cur:0.##}  {(outside ? "범위 이탈" : "범위 내")}";
            }

            // 가상 조건
            if (i == VirtualRow)
                DrawDiamond(g, X(Math.Clamp(VirtualValue, lo, hi)), cy, 8, Theme.Purple);

            // 오른쪽: 위 줄은 현재 값, 아래 줄은 조업범위 (각 줄이 겹치지 않게 행 높이의 절반씩 쓴다)
            var half = (int)(rowH / 2);
            const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;
            TextRenderer.DrawText(g, valueText, Theme.Font(8.5f),
                new Rectangle(barRight + 10, (int)top, ValueWidth - 10, half),
                r.Current is { } c2 && (c2 < r.P05 || c2 > r.P95) ? Theme.Orange : Theme.Text, flags);
            TextRenderer.DrawText(g, $"범위 {r.P05:0.##} ~ {r.P95:0.##}", Theme.Font(8),
                new Rectangle(barRight + 10, (int)top + half, ValueWidth - 10, half), Theme.Muted, flags);
        }
    }

    private static void DrawDiamond(Graphics g, float x, float y, float r, Color color)
    {
        var pts = new[] { new PointF(x, y - r), new PointF(x + r, y), new PointF(x, y + r), new PointF(x - r, y) };
        using var brush = new SolidBrush(color);
        using var pen = new Pen(Color.FromArgb(10, 17, 30), 1.5f);
        g.FillPolygon(brush, pts);
        g.DrawPolygon(pen, pts);
    }
}
