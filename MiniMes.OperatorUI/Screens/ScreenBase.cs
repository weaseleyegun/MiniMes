using MiniMes.OperatorUI.Controls;

namespace MiniMes.OperatorUI.Screens;

// 화면 3개의 공통 뼈대: 위쪽 제목 영역과 아래쪽 내용 영역.
public abstract class ScreenBase : Panel
{
    private readonly Label _updated = new();
    private readonly Label _error = new();

    protected ApiClient Api { get; }

    // 화면 내용을 넣는 영역
    protected Panel Content { get; } = new() { Dock = DockStyle.Fill };

    protected ScreenBase(ApiClient api, string tag, string title, string subtitle)
    {
        Api = api;
        Dock = DockStyle.Fill;
        BackColor = Theme.Bg;
        Visible = false;

        var header = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Theme.Bg };
        header.Controls.Add(new Label
        {
            Text = tag,
            AutoSize = true,
            ForeColor = Theme.Teal,
            Font = Theme.Font(8, true),
            Location = new Point(20, 8),
        });
        header.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            ForeColor = Theme.Text,
            Font = Theme.Font(17, true),
            Location = new Point(18, 24),
        });
        header.Controls.Add(new Label
        {
            Text = subtitle,
            AutoSize = true,
            ForeColor = Theme.Muted,
            Font = Theme.Font(9),
            Location = new Point(20, 58),
        });

        _updated.AutoSize = true;
        _updated.ForeColor = Theme.Muted;
        _updated.Font = Theme.Font(9);
        _updated.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _updated.Location = new Point(header.Width - 150, 14);
        header.Controls.Add(_updated);

        _error.AutoSize = true;
        _error.ForeColor = Theme.Orange;
        _error.Font = Theme.Font(9);
        _error.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _error.Location = new Point(header.Width - 600, 40);
        header.Controls.Add(_error);

        Content.Padding = new Padding(14, 0, 14, 12);
        Content.BackColor = Theme.Bg;

        Controls.Add(Content);
        Controls.Add(header);
    }

    // 이 화면이 보일 때 한 번 호출: 데이터를 불러온다.
    public virtual Task OnShownAsync() => Task.CompletedTask;

    // 화면이 보이는 동안 주기적으로 호출 (실시간 갱신용)
    public virtual Task TickAsync() => Task.CompletedTask;

    // 문서용 번호 표시 위치: (번호, 대상 컨트롤)
    public abstract IReadOnlyList<(string Number, Control Target)> Callouts { get; }

    protected void MarkUpdated() => _updated.Text = "갱신 " + DateTime.Now.ToString("HH:mm:ss");

    protected void ShowError(string? message) => _error.Text = message is null ? "" : "⚠ " + message;

    // 예외를 화면 오른쪽 위 메시지로 바꿔서 보여준다.
    protected async Task RunAsync(Func<Task> action)
    {
        try
        {
            await action();
            ShowError(null);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    // 2행 N열 같은 격자 레이아웃 만들기: 비율 지정
    protected static TableLayoutPanel Grid(float[] colPercents, float[] rowPercents)
    {
        var t = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = colPercents.Length,
            RowCount = rowPercents.Length,
            BackColor = Theme.Bg,
            Margin = new Padding(0),
        };
        foreach (var c in colPercents) t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, c));
        foreach (var r in rowPercents)
            t.RowStyles.Add(r < 0 ? new RowStyle(SizeType.Absolute, -r) : new RowStyle(SizeType.Percent, r));
        return t;
    }
}
