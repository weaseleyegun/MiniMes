using System.Drawing.Imaging;
using ScottPlot.WinForms;
using MiniMes.OperatorUI.Screens;

namespace MiniMes.OperatorUI;

// 문서(README)용 화면 캡처. 실행: MiniMes.OperatorUI.exe [API 주소] --screenshots <폴더>
// 화면 3개를 차례로 열어 PNG로 저장하고, 각 영역에 빨간 번호 표시를 그린다.
//
// 모니터 화면을 찍지 않고 프로그램 창이 스스로 그리는 방식(DrawToBitmap)을 쓴다.
// 그래서 다른 창이 앞을 가리고 있어도 그 내용이 찍히지 않는다.
public static class ScreenshotMode
{
    private static readonly string[] FileNames =
    {
        "screen-01-standard-ct.png",
        "screen-02-act-analysis.png",
        "screen-03-dashboard.png",
    };

    public static async Task RunAsync(MainForm form, string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        await Task.Delay(1500);

        for (var i = 0; i < form.Screens.Count; i++)
        {
            await form.ShowScreenAsync(i);
            await Task.Delay(2500);   // 차트가 다 그려질 때까지 잠시 기다린다.
            Capture(form, form.Screens[i], Path.Combine(outputDir, FileNames[i]));
        }

        form.Close();
    }

    private static void Capture(MainForm form, ScreenBase screen, string path)
    {
        using var bmp = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));

        // 창 왼쪽 위(테두리 포함)를 기준으로 한 컨트롤 위치
        Rectangle Relative(Control c)
        {
            var p = c.PointToScreen(Point.Empty);
            return new Rectangle(p.X - form.Location.X, p.Y - form.Location.Y, c.Width, c.Height);
        }

        using var g = Graphics.FromImage(bmp);

        // ScottPlot 차트는 DrawToBitmap으로 안 찍히므로 차트 엔진이 그린 이미지를 덧붙인다.
        foreach (var plot in FindPlots(screen))
        {
            var r = Relative(plot);
            if (r.Width < 10 || r.Height < 10) continue;

            using var ms = new MemoryStream(plot.Plot.GetImageBytes(r.Width, r.Height));
            using var chart = new Bitmap(ms);
            g.DrawImage(chart, r.Location);
        }

        // 번호 표시: 대상 영역에 빨간 테두리와 번호 배지
        using var pen = new Pen(Color.FromArgb(230, 60, 50), 3);
        using var badge = new SolidBrush(Color.FromArgb(230, 60, 50));
        using var font = new Font(Theme.FontName, 12, FontStyle.Bold);

        foreach (var (number, target) in screen.Callouts)
        {
            var r = Relative(target);
            g.DrawRectangle(pen, r);

            var box = new Rectangle(r.Right - 30, r.Top - 4, 26, 26);
            g.FillRectangle(badge, box);
            TextRenderer.DrawText(g, number, font, box, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        bmp.Save(path, ImageFormat.Png);
    }

    private static IEnumerable<FormsPlot> FindPlots(Control root)
    {
        foreach (Control child in root.Controls)
        {
            if (child is FormsPlot fp) yield return fp;
            foreach (var nested in FindPlots(child)) yield return nested;
        }
    }
}
