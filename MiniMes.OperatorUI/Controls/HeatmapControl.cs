namespace MiniMes.OperatorUI.Controls;

// 행(설비) x 열(시간) 격자에 값의 크기를 색으로 보여주는 히트맵.
// 값이 0보다 크면 주황(표준보다 느림), 0보다 작으면 청록(표준보다 빠름)으로 칠한다.
public class HeatmapControl : Control
{
    private const int LeftMargin = 84;
    private const int BottomMargin = 26;
    private const int TopMargin = 6;
    private const int RightMargin = 10;

    public string[] RowLabels { get; private set; } = Array.Empty<string>();
    public string[] ColLabels { get; private set; } = Array.Empty<string>();
    public double?[,] Values { get; private set; } = new double?[0, 0];
    public double ColorScale { get; set; } = 8;
    public (int Row, int Col)? Selected { get; private set; }

    public event Action<int, int>? CellClicked;

    public HeatmapControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Theme.Card;
        Font = Theme.Font(8.5f);
    }

    public void SetData(string[] rows, string[] cols, double?[,] values)
    {
        RowLabels = rows;
        ColLabels = cols;
        Values = values;
        Selected = null;
        Invalidate();
    }

    public void Select(int row, int col)
    {
        Selected = (row, col);
        Invalidate();
    }

    private RectangleF Plot => new(LeftMargin, TopMargin,
        Math.Max(1, Width - LeftMargin - RightMargin), Math.Max(1, Height - TopMargin - BottomMargin));

    private static Color ColorFor(double value, double scale)
    {
        var t = Math.Clamp(value / scale, -1, 1);
        var zero = Color.FromArgb(28, 42, 62);
        var hot = Color.FromArgb(240, 110, 60);
        var cold = Color.FromArgb(40, 175, 195);
        var target = t >= 0 ? hot : cold;
        var k = Math.Abs(t);
        return Color.FromArgb(
            (int)(zero.R + (target.R - zero.R) * k),
            (int)(zero.G + (target.G - zero.G) * k),
            (int)(zero.B + (target.B - zero.B) * k));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.Clear(BackColor);

        var rows = RowLabels.Length;
        var cols = ColLabels.Length;
        if (rows == 0 || cols == 0)
        {
            TextRenderer.DrawText(g, "조회된 데이터가 없습니다", Font, ClientRectangle, Theme.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        var plot = Plot;
        var cw = plot.Width / cols;
        var ch = plot.Height / rows;

        for (var r = 0; r < rows; r++)
        {
            var rowRect = new Rectangle(0, (int)(plot.Top + r * ch), LeftMargin - 6, (int)ch);
            TextRenderer.DrawText(g, RowLabels[r], Font, rowRect, Theme.Muted,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter);

            for (var c = 0; c < cols; c++)
            {
                var cell = new RectangleF(plot.Left + c * cw + 1, plot.Top + r * ch + 1, cw - 2, ch - 2);
                var v = Values[r, c];
                using var brush = new SolidBrush(v is { } val ? ColorFor(val, ColorScale) : Color.FromArgb(20, 30, 48));
                g.FillRectangle(brush, cell);
            }
        }

        // 열 라벨은 겹치지 않게 일정 간격으로만 그린다.
        var step = Math.Max(1, (int)Math.Ceiling(64 / Math.Max(1f, cw)));
        for (var c = 0; c < cols; c += step)
        {
            var rect = new Rectangle((int)(plot.Left + c * cw - 10), (int)plot.Bottom + 3, (int)(cw * step) + 20, BottomMargin - 3);
            TextRenderer.DrawText(g, ColLabels[c], Font, rect, Theme.Muted, TextFormatFlags.Left | TextFormatFlags.NoClipping);
        }

        if (Selected is { } s && s.Row < rows && s.Col < cols)
        {
            using var pen = new Pen(Color.White, 2);
            g.DrawRectangle(pen, plot.Left + s.Col * cw, plot.Top + s.Row * ch, cw, ch);
        }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (RowLabels.Length == 0 || ColLabels.Length == 0) return;

        var plot = Plot;
        if (!plot.Contains(e.Location)) return;

        var c = (int)((e.X - plot.Left) / (plot.Width / ColLabels.Length));
        var r = (int)((e.Y - plot.Top) / (plot.Height / RowLabels.Length));
        if (r < 0 || r >= RowLabels.Length || c < 0 || c >= ColLabels.Length) return;

        Selected = (r, c);
        Invalidate();
        CellClicked?.Invoke(r, c);
    }
}
