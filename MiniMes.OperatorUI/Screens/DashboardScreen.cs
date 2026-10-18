using MiniMes.OperatorUI.Controls;
using ScottPlot.WinForms;

namespace MiniMes.OperatorUI.Screens;

// 화면 3. 통합 대시보드
// 설비별 생산 C/T 현황과 표준 대비 편차를 한 화면에서 모니터링한다.
public class DashboardScreen : ScreenBase
{
    private const int MaxCycles = 5000;
    private const int MaxBins = 36;

    private List<ItemRow> _items = new();
    private bool _loaded;
    private bool _ticking;

    // 히트맵 셀별 요약 (행 = 설비, 열 = 시간 구간)
    private string[] _rows = Array.Empty<string>();
    private DateTime _binStart;
    private TimeSpan _binSpan = TimeSpan.FromHours(1);
    private (int Count, int Defect, double? AvgDev)[,] _cells = new (int, int, double?)[0, 0];

    // 조회 조건
    private readonly ComboBox _cboEquip = Ui.Combo(130);
    private readonly DateTimePicker _dtFrom = Ui.DatePicker(DateTime.Today.AddDays(-29));
    private readonly DateTimePicker _dtTo = Ui.DatePicker(DateTime.Today);
    private readonly Button _btnQuery = Ui.Primary("데이터 조회", 110);
    private readonly Label _lblInfo = new();

    private readonly KpiCard _kpiEquip = new("조회 설비", Theme.Text);
    private readonly KpiCard _kpiCycles = new("사이클 이력", Theme.Accent);
    private readonly KpiCard _kpiStd = new("표준 C/T 보유 품번", Theme.Green);
    private readonly KpiCard _kpiDefect = new("불량률", Theme.Orange);

    private readonly HeatmapControl _heatmap = new() { Dock = DockStyle.Fill };
    private readonly Label _lblDetail = new();
    private readonly FormsPlot _trend = Charts.Create();
    private readonly FormsPlot _counts = Charts.Create();
    private readonly DataGridView _gridStatus = new();

    private readonly CardPanel _cardFilter = new();
    private readonly CardPanel _cardHeat = new("설비 × 시간 · C/T 편차", "표준 C/T 대비 평균 편차(%) · 주황 = 느림, 청록 = 빠름");
    private readonly CardPanel _cardDetail = new("분석 상세", "셀을 클릭하면 상세가 표시됩니다");
    private readonly CardPanel _cardTrend = new("실측 C/T 편차 추이", "설비별 시간 구간 평균 편차(%)");
    private readonly CardPanel _cardCounts = new("설비별 기록 건수", "양품 / 불량 사이클 수");
    private readonly CardPanel _cardStatus = new("설비 실시간 상태", "2초마다 갱신");

    public DashboardScreen(ApiClient api)
        : base(api, "03 / MANUFACTURING INTELLIGENCE", "통합 대시보드",
               "수집된 C/T 이력과 설비 상태를 하나의 분석 화면으로 확인합니다.")
    {
        BuildFilterCard();
        BuildHeatCard();
        BuildStatusGrid();

        _cardTrend.Body.Controls.Add(_trend);
        _cardCounts.Body.Controls.Add(_counts);
        _cardStatus.Body.Controls.Add(_gridStatus);

        var heatRow = Grid(new float[] { 72, 28 }, new float[] { 100 });
        heatRow.Controls.Add(_cardHeat, 0, 0);
        heatRow.Controls.Add(_cardDetail, 1, 0);

        var bottom = Grid(new float[] { 36, 28, 36 }, new float[] { 100 });
        bottom.Controls.Add(_cardTrend, 0, 0);
        bottom.Controls.Add(_cardCounts, 1, 0);
        bottom.Controls.Add(_cardStatus, 2, 0);

        var root = Grid(new float[] { 100 }, new float[] { -150, 43, 57 });
        root.Controls.Add(_cardFilter, 0, 0);
        root.Controls.Add(heatRow, 0, 1);
        root.Controls.Add(bottom, 0, 2);
        Content.Controls.Add(root);

        _btnQuery.Click += async (_, _) => await RunAsync(QueryAsync);
        _heatmap.CellClicked += ShowCell;
    }

    public override IReadOnlyList<(string Number, Control Target)> Callouts => new (string, Control)[]
    {
        ("1", _cardFilter), ("2", _cardHeat), ("3", _cardTrend), ("4", _cardCounts), ("5", _cardStatus),
    };

    // ---------------------------------------------------------------- 화면 구성

    private void BuildFilterCard()
    {
        _lblInfo.AutoSize = true;
        _lblInfo.ForeColor = Theme.Muted;
        _lblInfo.Font = Theme.Font(9);
        _lblInfo.Margin = new Padding(6, 11, 0, 0);

        var kpis = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = new Padding(0) };
        for (var i = 0; i < 4; i++) kpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        kpis.Controls.Add(_kpiEquip, 0, 0);
        kpis.Controls.Add(_kpiCycles, 1, 0);
        kpis.Controls.Add(_kpiStd, 2, 0);
        kpis.Controls.Add(_kpiDefect, 3, 0);

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.Controls.Add(Ui.Row(
            Ui.Caption("설비"), _cboEquip, Ui.Caption("시작일"), _dtFrom, Ui.Caption("종료일"), _dtTo, _btnQuery, _lblInfo), 0, 0);
        body.Controls.Add(kpis, 0, 1);

        _cardFilter.Body.Padding = new Padding(10, 4, 10, 6);
        _cardFilter.Body.Controls.Add(body);
    }

    private void BuildHeatCard()
    {
        _cardHeat.Body.Controls.Add(_heatmap);

        _lblDetail.Dock = DockStyle.Fill;
        _lblDetail.ForeColor = Theme.Text;
        _lblDetail.Font = Theme.Font(9.5f);
        _lblDetail.Padding = new Padding(2, 6, 4, 0);
        _lblDetail.Text = "조회하면 설비별·시간대별 C/T 편차가 표시됩니다.";
        _cardDetail.Body.Controls.Add(_lblDetail);
    }

    private void BuildStatusGrid()
    {
        Ui.StyleGrid(_gridStatus);
        _gridStatus.Columns.Add("eq", "설비");
        _gridStatus.Columns.Add("status", "상태");
        _gridStatus.Columns.Add("item", "최근 품번");
        _gridStatus.Columns.Add("judge", "CT 판정");
        _gridStatus.Columns.Add("defect", "불량률(%)");
        _gridStatus.Columns["defect"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        _gridStatus.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.CellStyle is not { } style) return;
            var name = _gridStatus.Columns[e.ColumnIndex].Name;
            if (name == "status" && e.Value is string s)
            {
                style.ForeColor = s switch { "가동 중" => Theme.Green, "대기" => Theme.Yellow, "알람" => Theme.Red, _ => Theme.Muted };
                style.Font = Theme.Font(9.5f, true);
            }
            else if (name == "judge")
            {
                var (back, fore) = Theme.JudgeColors(e.Value as string);
                style.BackColor = back;
                style.ForeColor = fore;
                style.SelectionBackColor = back;
                style.SelectionForeColor = fore;
            }
        };
    }

    // ---------------------------------------------------------------- 데이터

    public override async Task OnShownAsync()
    {
        await RunAsync(async () =>
        {
            _items = await Api.GetItemsAsync();
            var board = await Api.GetEquipmentBoardAsync();
            FillStatus(board);

            if (!_loaded)
            {
                _cboEquip.Items.Add("전체 설비");
                foreach (var b in board) _cboEquip.Items.Add(b.EquipmentCode);
                _cboEquip.SelectedIndex = 0;
                _loaded = true;
            }
            await QueryAsync();
        });
    }

    // 2초마다 설비 상태 목록만 갱신한다.
    public override async Task TickAsync()
    {
        if (_ticking) return;
        _ticking = true;
        try
        {
            FillStatus(await Api.GetEquipmentBoardAsync());
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            _ticking = false;
        }
    }

    private void FillStatus(List<EquipmentBoardRow> board)
    {
        _gridStatus.Rows.Clear();
        foreach (var b in board)
        {
            var stale = b.LastCollectedAt is null || (DateTime.Now - b.LastCollectedAt.Value).TotalSeconds > 15;
            _gridStatus.Rows.Add(b.EquipmentCode, stale ? "수집 중단" : Theme.StatusText(b.Status),
                b.LastItemCode ?? "-", b.Judgement, $"{b.DefectRatePct:0.0}");
        }
        _gridStatus.ClearSelection();
    }

    private async Task QueryAsync()
    {
        var equip = _cboEquip.SelectedIndex > 0 ? _cboEquip.SelectedItem as string : null;
        var from = _dtFrom.Value.Date;
        var to = _dtTo.Value.Date.AddDays(1).AddSeconds(-1);

        _items = await Api.GetItemsAsync();
        var cycles = await Api.GetCyclesAsync(null, equip, from, to, MaxCycles);

        var std = _items.Where(i => i.StandardCycleTimeSec is not null)
            .ToDictionary(i => i.ItemCode, i => i.StandardCycleTimeSec!.Value);

        var equipCodes = cycles.Select(c => c.EquipmentCode).Distinct().OrderBy(x => x).ToList();
        var defects = cycles.Count(c => !c.IsGood);

        _kpiEquip.Set($"{equipCodes.Count}", "조회 기준");
        _kpiCycles.Set($"{cycles.Count:N0}", cycles.Count >= MaxCycles ? $"최근 {MaxCycles:N0}건 기준" : "조회 기준");
        _kpiStd.Set($"{std.Count} / {_items.Count}", std.Count < _items.Count ? "일부 품번은 표준 미산정" : "전 품번 보유");
        _kpiDefect.Set(cycles.Count == 0 ? "-" : $"{defects * 100.0 / cycles.Count:0.0}%", $"불량 {defects:N0}건");
        _lblInfo.Text = $"{from:yyyy-MM-dd} ~ {to:yyyy-MM-dd}";

        BuildHeatmap(cycles, std, equipCodes);
        DrawTrend(cycles, std, equipCodes);
        DrawCounts(cycles, equipCodes);
        MarkUpdated();
    }

    // ---------------------------------------------------------------- 히트맵 / 차트

    private void BuildHeatmap(List<CycleRow> cycles, Dictionary<string, double> std, List<string> equipCodes)
    {
        if (cycles.Count == 0 || equipCodes.Count == 0)
        {
            _rows = Array.Empty<string>();
            _cells = new (int, int, double?)[0, 0];
            _heatmap.SetData(_rows, Array.Empty<string>(), new double?[0, 0]);
            _lblDetail.Text = "조회된 사이클이 없습니다.";
            return;
        }

        var tMin = cycles.Min(c => c.StartedAt);
        var tMax = cycles.Max(c => c.StartedAt);
        var span = tMax - tMin;
        _binSpan = TimeSpan.FromHours(Math.Max(1, Math.Ceiling(span.TotalHours / MaxBins)));
        _binStart = new DateTime(tMin.Year, tMin.Month, tMin.Day, tMin.Hour, 0, 0);
        var cols = (int)Math.Ceiling((tMax - _binStart).TotalHours / _binSpan.TotalHours) + 1;

        _rows = equipCodes.ToArray();
        _cells = new (int, int, double?)[_rows.Length, cols];

        var devSum = new double[_rows.Length, cols];
        var devCnt = new int[_rows.Length, cols];
        var count = new int[_rows.Length, cols];
        var defect = new int[_rows.Length, cols];

        foreach (var c in cycles)
        {
            var r = Array.IndexOf(_rows, c.EquipmentCode);
            var k = (int)((c.StartedAt - _binStart).TotalHours / _binSpan.TotalHours);
            if (r < 0 || k < 0 || k >= cols) continue;

            count[r, k]++;
            if (!c.IsGood) defect[r, k]++;
            if (c.IsGood && std.TryGetValue(c.ItemCode, out var s))
            {
                devSum[r, k] += (c.CycleTimeSec - s) / s * 100;
                devCnt[r, k]++;
            }
        }

        var values = new double?[_rows.Length, cols];
        for (var r = 0; r < _rows.Length; r++)
            for (var k = 0; k < cols; k++)
            {
                double? avg = devCnt[r, k] > 0 ? devSum[r, k] / devCnt[r, k] : null;
                values[r, k] = avg;
                _cells[r, k] = (count[r, k], defect[r, k], avg);
            }

        var labels = Enumerable.Range(0, cols)
            .Select(k => (_binStart + k * _binSpan).ToString("MM-dd HH"))
            .ToArray();
        _heatmap.SetData(_rows, labels, values);

        var all = Enumerable.Range(0, _rows.Length).SelectMany(r => Enumerable.Range(0, cols).Select(k => values[r, k]))
            .Where(v => v is not null).Select(v => Math.Abs(v!.Value)).ToList();
        _heatmap.ColorScale = all.Count == 0 ? 5 : Math.Max(3, Stats.Percentile(all, 0.95));
        _heatmap.Invalidate();

        _lblDetail.Text = $"시간 구간 {_binSpan.TotalHours:0}시간 단위\r\n\r\n색 범위: ±{_heatmap.ColorScale:0.0}%\r\n주황 = 표준보다 느림\r\n청록 = 표준보다 빠름\r\n\r\n셀을 클릭하세요.";
    }

    private void ShowCell(int row, int col)
    {
        if (row >= _cells.GetLength(0) || col >= _cells.GetLength(1)) return;

        var (count, defect, avg) = _cells[row, col];
        var start = _binStart + col * _binSpan;
        _lblDetail.Text =
            $"설비  {_rows[row]}\r\n\r\n" +
            $"구간  {start:MM-dd HH:mm} ~ {start + _binSpan:HH:mm}\r\n\r\n" +
            $"사이클  {count:N0}건\r\n불량  {defect:N0}건\r\n\r\n" +
            (avg is { } a ? $"표준 대비 평균 편차\r\n{a:+0.0;-0.0;0.0}%" : "표준 비교 가능한 양품 없음");
    }

    private void DrawTrend(List<CycleRow> cycles, Dictionary<string, double> std, List<string> equipCodes)
    {
        Charts.Reset(_trend, dateAxisBottom: true);
        if (_cells.GetLength(1) == 0)
        {
            Charts.Finish(_trend, "데이터 없음");
            return;
        }

        var palette = new[] { Theme.Teal, Theme.Orange, Theme.Accent, Theme.Purple, Theme.Yellow };
        for (var r = 0; r < _rows.Length; r++)
        {
            var xs = new List<double>();
            var ys = new List<double>();
            for (var k = 0; k < _cells.GetLength(1); k++)
            {
                if (_cells[r, k].AvgDev is not { } dev) continue;
                xs.Add((_binStart + k * _binSpan + _binSpan / 2).ToOADate());
                ys.Add(dev);
            }
            if (xs.Count == 0) continue;

            var line = _trend.Plot.Add.Scatter(xs.ToArray(), ys.ToArray());
            line.Color = Charts.C(palette[r % palette.Length]);
            line.LineWidth = 2;
            line.MarkerSize = 4;
            line.LegendText = _rows[r];
        }
        Charts.Line(_trend, 0, Theme.Muted, null, dashed: false);
        Charts.Finish(_trend);
    }

    private void DrawCounts(List<CycleRow> cycles, List<string> equipCodes)
    {
        Charts.Reset(_counts);
        if (equipCodes.Count == 0)
        {
            Charts.Finish(_counts, "데이터 없음");
            return;
        }

        var good = equipCodes.Select(e => (double)cycles.Count(c => c.EquipmentCode == e && c.IsGood)).ToArray();
        var bad = equipCodes.Select(e => (double)cycles.Count(c => c.EquipmentCode == e && !c.IsGood)).ToArray();
        var pos = Enumerable.Range(0, equipCodes.Count).Select(i => (double)i).ToArray();

        var gBars = _counts.Plot.Add.Bars(pos.Select(p => p - 0.18).ToArray(), good);
        foreach (var b in gBars.Bars) { b.Size = 0.34; b.FillColor = Charts.C(Theme.Teal); }
        gBars.LegendText = "양품";

        var bBars = _counts.Plot.Add.Bars(pos.Select(p => p + 0.18).ToArray(), bad);
        foreach (var b in bBars.Bars) { b.Size = 0.34; b.FillColor = Charts.C(Theme.Red); }
        bBars.LegendText = "불량";

        _counts.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(pos, equipCodes.ToArray());
        Charts.Finish(_counts);
        _counts.Plot.Axes.SetLimitsY(0, _counts.Plot.Axes.GetLimits().Top);
        _counts.Refresh();
    }
}
