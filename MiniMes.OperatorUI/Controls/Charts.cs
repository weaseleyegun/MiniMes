using ScottPlot;
using ScottPlot.WinForms;
using SPColor = ScottPlot.Color;

namespace MiniMes.OperatorUI.Controls;

// ScottPlot 차트를 화면 테마(어두운 배경)에 맞게 만들고 다루는 도우미
public static class Charts
{
    public static SPColor C(System.Drawing.Color c) => new(c.R, c.G, c.B, c.A);

    public static FormsPlot Create()
    {
        var fp = new FormsPlot { Dock = DockStyle.Fill };
        var p = fp.Plot;

        p.FigureBackground.Color = C(Theme.Card);
        p.DataBackground.Color = C(System.Drawing.Color.FromArgb(10, 17, 30));
        p.Axes.Color(C(Theme.Muted));
        p.Grid.MajorLineColor = C(Theme.Line);
        p.Legend.BackgroundColor = C(Theme.Panel);
        p.Legend.FontColor = C(Theme.Text);
        p.Legend.OutlineColor = C(Theme.Line);
        p.Legend.Orientation = ScottPlot.Orientation.Horizontal;
        ApplyFonts(p);
        return fp;
    }

    // 차트 엔진(SkiaSharp)은 시스템 글꼴 이름만으로는 한글을 못 그리는 경우가 있어서,
    // Windows 글꼴 파일(맑은 고딕)을 직접 등록해 둔다. 프로그램 시작 때 한 번 호출한다.
    public static void RegisterKoreanFont()
    {
        var dir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        var regular = Path.Combine(dir, "malgun.ttf");
        var bold = Path.Combine(dir, "malgunbd.ttf");

        if (File.Exists(regular)) Fonts.AddFontFile(Theme.FontName, regular);
        if (File.Exists(bold)) Fonts.AddFontFile(Theme.FontName, bold, bold: true);
        Fonts.Default = Theme.FontName;
    }

    // 날짜 축처럼 축 객체가 새로 만들어지는 경우가 있어서, 그릴 때마다 모든 글자에 한글 글꼴을 다시 지정한다.
    private static void ApplyFonts(Plot p)
    {
        var name = Theme.FontName;
        p.Legend.FontName = name;
        p.Axes.Title.Label.FontName = name;
        foreach (var axis in new IAxis[] { p.Axes.Bottom, p.Axes.Left, p.Axes.Right, p.Axes.Top })
        {
            axis.TickLabelStyle.FontName = name;
            axis.Label.FontName = name;
        }
    }

    // 새로 그리기 전에 이전 도형을 지운다.
    public static void Reset(FormsPlot fp, bool dateAxisBottom = false)
    {
        fp.Plot.Clear();
        fp.Plot.Title("");
        if (dateAxisBottom) fp.Plot.Axes.DateTimeTicksBottom();
        ApplyFonts(fp.Plot);
    }

    // 도형을 모두 추가한 뒤 호출: 축 범위를 맞추고 화면을 갱신한다.
    public static void Finish(FormsPlot fp, string? emptyMessage = null)
    {
        if (emptyMessage is not null) fp.Plot.Title(emptyMessage);
        ApplyFonts(fp.Plot);
        fp.Plot.ShowLegend(Alignment.UpperRight);
        fp.Plot.Axes.AutoScale();
        fp.Refresh();
    }

    public static void Line(FormsPlot fp, double y, System.Drawing.Color color, string? legend = null, bool dashed = true)
    {
        var line = fp.Plot.Add.HorizontalLine(y);
        line.Color = C(color);
        line.LineWidth = 1.5f;
        if (dashed) line.LinePattern = LinePattern.Dashed;
        if (legend is not null) line.LegendText = legend;
    }
}
