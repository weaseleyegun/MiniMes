namespace MiniMes.OperatorUI;

static class Program
{
    // 사용법: MiniMes.OperatorUI.exe [API 주소] [--screenshots <폴더>]
    // 주소 우선순위: 실행 인수 > 환경변수 MINIMES_API_URL > 기본값
    [STAThread]
    static void Main(string[] args)
    {
        var list = args.ToList();

        string? shotDir = null;
        var shotIndex = list.IndexOf("--screenshots");
        if (shotIndex >= 0)
        {
            shotDir = shotIndex + 1 < list.Count ? list[shotIndex + 1] : "docs/images";
            list.RemoveRange(shotIndex, Math.Min(2, list.Count - shotIndex));
        }

        var apiUrl = list.Count > 0
            ? list[0]
            : Environment.GetEnvironmentVariable("MINIMES_API_URL") ?? "http://localhost:5168";

        ApplicationConfiguration.Initialize();
        Controls.Charts.RegisterKoreanFont();

        var form = new MainForm(apiUrl);
        if (shotDir is not null)
            form.Shown += async (_, _) => await ScreenshotMode.RunAsync(form, shotDir);

        Application.Run(form);
    }
}
