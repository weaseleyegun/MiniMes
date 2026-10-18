using MiniMes.OperatorUI.Controls;
using MiniMes.OperatorUI.Screens;

namespace MiniMes.OperatorUI;

// 프로그램 틀: 위쪽 제목 줄과 화면 전환 버튼, 가운데에 화면 3개 중 하나를 보여준다.
public class MainForm : Form
{
    private readonly ApiClient _api;
    private readonly ScreenBase[] _screens;
    private readonly Button[] _navButtons;
    private readonly Label _lblConn = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 2000 };
    private int _current = -1;

    public MainForm(string apiUrl)
    {
        _api = new ApiClient(apiUrl);
        _screens = new ScreenBase[]
        {
            new StandardCtScreen(_api),
            new ActAnalysisScreen(_api),
            new DashboardScreen(_api),
        };
        _navButtons = new Button[_screens.Length];

        Text = "MiniMes 작업자 화면";
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = Theme.Font(10);
        ClientSize = new Size(1360, 860);
        MinimumSize = new Size(1100, 700);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;

        BuildUi();

        KeyDown += (_, e) => { if (e.KeyCode == Keys.F11) ToggleFullScreen(); };
        _timer.Tick += async (_, _) => await TickAsync();
        Shown += async (_, _) =>
        {
            _timer.Start();
            await ShowScreenAsync(0);
        };
        FormClosed += (_, _) => _timer.Stop();
    }

    public IReadOnlyList<ScreenBase> Screens => _screens;

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Bg,
            Margin = new Padding(0),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildNav(), 0, 1);

        var host = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg };
        foreach (var s in _screens) host.Controls.Add(s);
        root.Controls.Add(host, 0, 2);
    }

    private Panel BuildHeader()
    {
        var header = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Panel };

        var title = new Label
        {
            Text = "MiniMes  작업자 화면",
            Dock = DockStyle.Left,
            Width = 340,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 0, 0, 0),
            Font = Theme.Font(14, true),
            ForeColor = Theme.Text,
        };

        _lblConn.Dock = DockStyle.Fill;
        _lblConn.TextAlign = ContentAlignment.MiddleRight;
        _lblConn.Padding = new Padding(0, 0, 20, 0);
        _lblConn.ForeColor = Theme.Muted;
        _lblConn.Text = "연결 중...";

        header.Controls.Add(_lblConn);
        header.Controls.Add(title);
        return header;
    }

    private Panel BuildNav()
    {
        var nav = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Bg,
            Padding = new Padding(16, 6, 0, 0),
        };

        var names = new[] { "01  표준 C/T 체계 구축 및 예측", "02  양품 구간 실측값(ACT) 분석", "03  통합 대시보드" };
        for (var i = 0; i < names.Length; i++)
        {
            var index = i;
            var btn = new Button
            {
                Text = names[i],
                AutoSize = true,
                MinimumSize = new Size(230, 36),
                FlatStyle = FlatStyle.Flat,
                Font = Theme.Font(10),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0),
                TabStop = false,
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += async (_, _) => await ShowScreenAsync(index);
            _navButtons[i] = btn;
            nav.Controls.Add(btn);
        }
        return nav;
    }

    // 화면을 바꾸고, 그 화면의 데이터를 불러올 때까지 기다린다.
    public async Task ShowScreenAsync(int index)
    {
        _current = index;
        for (var i = 0; i < _screens.Length; i++)
        {
            _screens[i].Visible = i == index;
            _navButtons[i].BackColor = i == index ? Theme.Panel : Theme.Bg;
            _navButtons[i].ForeColor = i == index ? Theme.Text : Theme.Muted;
        }

        await _screens[index].OnShownAsync();
        SetConn(true);
    }

    // 2초마다 현재 화면의 실시간 부분을 갱신한다.
    private async Task TickAsync()
    {
        if (_current < 0) return;
        try
        {
            await _screens[_current].TickAsync();
            SetConn(true);
        }
        catch (Exception ex)
        {
            SetConn(false, ex.Message);
        }
    }

    private void SetConn(bool ok, string? error = null)
    {
        _lblConn.ForeColor = ok ? Color.FromArgb(165, 214, 167) : Color.FromArgb(239, 154, 154);
        _lblConn.Text = ok
            ? $"● API 연결됨  ·  {_api.BaseUrl.TrimEnd('/')}"
            : $"● API 연결 실패 ({error})";
    }

    private void ToggleFullScreen()
    {
        var full = FormBorderStyle == FormBorderStyle.None;
        FormBorderStyle = full ? FormBorderStyle.Sizable : FormBorderStyle.None;
        WindowState = full ? FormWindowState.Normal : FormWindowState.Maximized;
    }
}
