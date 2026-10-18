using MiniMes.OperatorUI.Controls;
using ScottPlot.WinForms;

namespace MiniMes.OperatorUI.Screens;

// 화면 2. 양품 구간 실측값(ACT) 분석
// 양품의 ACT 값이 어느 범위에서 안정적인지(조업범위) 찾고, 현재 조건과 불량 사이클이 그 범위를 벗어나는지 본다.
public class ActAnalysisScreen : ScreenBase
{
    private List<CycleRow> _cycles = new();
    private ActAnalysisResponse? _act;
    private bool _loaded;
    private bool _suppress;

    // 조회 조건
    private readonly ComboBox _cboEquip = Ui.Combo(120);
    private readonly ComboBox _cboItem = Ui.Combo(130);
    private readonly ComboBox _cboPeriod = Ui.Combo(110);
    private readonly ComboBox _cboVar = Ui.Combo(190);
    private readonly Button _btnQuery = Ui.Primary("조회", 80);
    private readonly Label _lblInfo = new();

    // 조업범위 + 가상 조건
    private readonly RangeBarsControl _ranges = new() { Dock = DockStyle.Fill };
    private readonly TrackBar _slider = new() { Minimum = 0, Maximum = 1000, TickStyle = TickStyle.None, Dock = DockStyle.Fill };
    private readonly Label _lblVirtual = new();
    private readonly Button _btnRestore = Ui.Secondary("기록값 복원");

    // 차트
    private readonly FormsPlot _hist = Charts.Create();
    private readonly FormsPlot _box = Charts.Create();
    private readonly FormsPlot _series = Charts.Create();
    private readonly FormsPlot _outliers = Charts.Create();

    private readonly CardPanel _cardFilter = new();
    private readonly CardPanel _cardRange = new("양품 조업범위 · 현재 조건", "청록 막대 = 양품 5~95% 범위 · 흰 눈금 = 평균 · 마름모 = 현재 값");
    private readonly CardPanel _cardHist = new("선택 변수 분포", "양품 / 불량의 구간별 비율(%)");
    private readonly CardPanel _cardBox = new("설비별 조업범위", "양품 5~95% 분위수");
    private readonly CardPanel _cardSeries = new("시간별 공정조건 · 관측 범위", "점선 = 양품 5~95% 범위");
    private readonly CardPanel _cardOut = new("범위 이탈 발생 이력", "범위 경계로부터의 차이");
    private Control _distGroup = new Panel();
    private Control _bottomRow = new Panel();

    public ActAnalysisScreen(ApiClient api)
        : base(api, "02 / INTERACTIVE OPERATING RANGE", "양품 구간 실측값(ACT) 분석",
               "양품 관측 범위와 현재 조건을 비교하고, 가상 조건의 범위 이탈을 확인합니다.")
    {
        _cboPeriod.Items.AddRange(new object[] { "최근 1일", "최근 3일", "최근 7일", "전체 기간" });
        _cboPeriod.SelectedIndex = 3;
        foreach (var v in ActVariable.All) _cboVar.Items.Add(v);
        _cboVar.SelectedIndex = 1;

        _lblInfo.AutoSize = true;
        _lblInfo.ForeColor = Theme.Muted;
        _lblInfo.Font = Theme.Font(9);
        _lblInfo.Margin = new Padding(8, 11, 0, 0);

        _cardFilter.Body.Padding = new Padding(10, 2, 10, 0);
        _cardFilter.Body.Controls.Add(Ui.Row(
            Ui.Caption("설비"), _cboEquip, Ui.Caption("품번"), _cboItem, Ui.Caption("기간"), _cboPeriod,
            Ui.Caption("변수"), _cboVar, _btnQuery, _lblInfo));

        BuildRangeCard();
        _cardHist.Body.Controls.Add(_hist);
        _cardBox.Body.Controls.Add(_box);
        _cardSeries.Body.Controls.Add(_series);
        _cardOut.Body.Controls.Add(_outliers);

        var dist = Grid(new float[] { 58, 42 }, new float[] { 100 });
        dist.Controls.Add(_cardHist, 0, 0);
        dist.Controls.Add(_cardBox, 1, 0);
        _distGroup = dist;

        var bottom = Grid(new float[] { 62, 38 }, new float[] { 100 });
        bottom.Controls.Add(_cardSeries, 0, 0);
        bottom.Controls.Add(_cardOut, 1, 0);
        _bottomRow = bottom;

        var middle = Grid(new float[] { 52, 48 }, new float[] { 100 });
        middle.Controls.Add(_cardRange, 0, 0);
        middle.Controls.Add(dist, 1, 0);

        var root = Grid(new float[] { 100 }, new float[] { -62, 46, 54 });
        root.Controls.Add(_cardFilter, 0, 0);
        root.Controls.Add(middle, 0, 1);
        root.Controls.Add(bottom, 0, 2);
        Content.Controls.Add(root);

        _btnQuery.Click += async (_, _) => await RunAsync(QueryAsync);
        _cboVar.SelectedIndexChanged += (_, _) => { if (_loaded) RedrawVariable(); };
        _slider.Scroll += (_, _) => UpdateVirtual();
        _btnRestore.Click += (_, _) => RestoreSlider();
    }

    public override IReadOnlyList<(string Number, Control Target)> Callouts => new (string, Control)[]
    {
        ("1", _cardFilter), ("2", _cardRange), ("3", _distGroup), ("4", _bottomRow),
    };

    private ActVariable SelectedVar => (ActVariable)_cboVar.SelectedItem!;

    // ---------------------------------------------------------------- 화면 구성

    private void BuildRangeCard()
    {
        _lblVirtual.AutoSize = false;
        _lblVirtual.Dock = DockStyle.Fill;
        _lblVirtual.ForeColor = Theme.Text;
        _lblVirtual.Font = Theme.Font(9.5f, true);
        _lblVirtual.TextAlign = ContentAlignment.MiddleLeft;

        var caption = new Label
        {
            Text = "가상 조건",
            ForeColor = Theme.Purple,
            Font = Theme.Font(9, true),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        var virtualRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = new Padding(0) };
        virtualRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        virtualRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        virtualRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        virtualRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        virtualRow.Controls.Add(caption, 0, 0);
        virtualRow.Controls.Add(_slider, 1, 0);
        virtualRow.Controls.Add(_lblVirtual, 2, 0);
        virtualRow.Controls.Add(_btnRestore, 3, 0);

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        body.Controls.Add(_ranges, 0, 0);
        body.Controls.Add(virtualRow, 0, 1);
        _cardRange.Body.Controls.Add(body);
    }

    // ---------------------------------------------------------------- 데이터

    public override async Task OnShownAsync()
    {
        if (_loaded) return;
        await RunAsync(async () =>
        {
            var items = await Api.GetItemsAsync();
            var board = await Api.GetEquipmentBoardAsync();

            _cboEquip.Items.Add("전체 설비");
            foreach (var b in board) _cboEquip.Items.Add(b.EquipmentCode);
            _cboEquip.SelectedIndex = 0;

            foreach (var i in items) _cboItem.Items.Add(i.ItemCode);
            var first = items.FirstOrDefault(i => i.StandardCycleTimeSec is not null) ?? items.FirstOrDefault();
            if (first is not null) _cboItem.SelectedItem = first.ItemCode;

            await QueryAsync();
        });
    }

    private async Task QueryAsync()
    {
        if (_cboItem.SelectedItem is not string code) return;

        var equip = _cboEquip.SelectedIndex > 0 ? _cboEquip.SelectedItem as string : null;
        DateTime? from = _cboPeriod.SelectedIndex switch
        {
            0 => DateTime.Now.AddDays(-1),
            1 => DateTime.Now.AddDays(-3),
            2 => DateTime.Now.AddDays(-7),
            _ => null,
        };

        _cycles = await Api.GetCyclesAsync(code, equip, from, null);
        try { _act = await Api.GetActAnalysisAsync(code); } catch { _act = null; }

        var good = _cycles.Count(c => c.IsGood);
        _lblInfo.Text = $"{code} · 양품 {good:N0}건 / 불량 {_cycles.Count - good:N0}건";
        _loaded = true;
        RedrawVariable();
        MarkUpdated();
    }

    // ---------------------------------------------------------------- 그리기

    private void RedrawVariable()
    {
        var good = _cycles.Where(c => c.IsGood).ToList();
        DrawRanges(good);
        RestoreSlider();

        var v = SelectedVar;
        var rel = _act?.Variables.FirstOrDefault(x => x.Variable == v.Key);
        _cardHist.Subtitle = rel is null
            ? "양품 / 불량의 구간별 비율(%)"
            : $"양품 / 불량 구간별 비율(%) · 불량 관련도 d={rel.CohensD:0.00} ({rel.Judgement})";

        DrawHistogram(v);
        DrawBox(v);
        DrawSeries(v, good);
    }

    private void DrawRanges(List<CycleRow> good)
    {
        if (good.Count < 5)
        {
            _ranges.SetRows(Array.Empty<RangeRow>());
            return;
        }

        var last = _cycles.Count > 0 ? _cycles[^1] : null;
        var rows = ActVariable.All.Select(v =>
        {
            var vals = good.Select(v.Select).ToList();
            return new RangeRow(v.Label, v.Unit, vals.Min(), vals.Max(),
                Stats.Percentile(vals, 0.05), Stats.Percentile(vals, 0.95), Stats.Mean(vals),
                last is null ? null : v.Select(last));
        }).ToList();
        _ranges.SetRows(rows);
    }

    private RangeRow? SelectedRow()
    {
        var idx = _cboVar.SelectedIndex;
        return idx >= 0 && idx < _ranges.Rows.Count ? _ranges.Rows[idx] : null;
    }

    private void RestoreSlider()
    {
        var row = SelectedRow();
        if (row is null)
        {
            _lblVirtual.Text = "";
            return;
        }

        var (lo, hi) = RangeBarsControl.AxisOf(row);
        var value = row.Current ?? row.Mean;
        _suppress = true;
        _slider.Value = (int)Math.Clamp((value - lo) / (hi - lo) * 1000, 0, 1000);
        _suppress = false;
        UpdateVirtual();
    }

    private void UpdateVirtual()
    {
        if (_suppress) return;
        var row = SelectedRow();
        if (row is null) return;

        var (lo, hi) = RangeBarsControl.AxisOf(row);
        var value = lo + _slider.Value / 1000.0 * (hi - lo);
        var outside = value < row.P05 || value > row.P95;

        _ranges.SetVirtual(_cboVar.SelectedIndex, value);
        _lblVirtual.ForeColor = outside ? Theme.Orange : Theme.Green;
        _lblVirtual.Text = $"{value:0.##} {row.Unit} · {(outside ? "범위 이탈" : "범위 내")}";
    }

    private void DrawHistogram(ActVariable v)
    {
        Charts.Reset(_hist);
        var good = _cycles.Where(c => c.IsGood).Select(v.Select).ToList();
        var bad = _cycles.Where(c => !c.IsGood).Select(v.Select).ToList();
        if (good.Count == 0)
        {
            Charts.Finish(_hist, "데이터 없음");
            return;
        }

        var all = good.Concat(bad).ToList();
        var lo = all.Min();
        var hi = all.Max();
        if (hi - lo < 1e-9) hi = lo + 1;

        const int bins = 16;
        var width = (hi - lo) / bins;

        double[] Percent(List<double> values)
        {
            var counts = new double[bins];
            foreach (var x in values) counts[Math.Min(bins - 1, (int)((x - lo) / width))]++;
            return values.Count == 0 ? counts : counts.Select(c => c * 100.0 / values.Count).ToArray();
        }

        var centers = Enumerable.Range(0, bins).Select(i => lo + (i + 0.5) * width).ToArray();
        var barW = width * 0.4;

        var gBars = _hist.Plot.Add.Bars(centers.Select(c => c - barW / 2).ToArray(), Percent(good));
        foreach (var b in gBars.Bars) { b.Size = barW; b.FillColor = Charts.C(Theme.Teal); }
        gBars.LegendText = "양품";

        if (bad.Count > 0)
        {
            var bBars = _hist.Plot.Add.Bars(centers.Select(c => c + barW / 2).ToArray(), Percent(bad));
            foreach (var b in bBars.Bars) { b.Size = barW; b.FillColor = Charts.C(Theme.Red); }
            bBars.LegendText = "불량";
        }

        Charts.Finish(_hist);
        _hist.Plot.Axes.SetLimitsY(0, _hist.Plot.Axes.GetLimits().Top);
        _hist.Refresh();
    }

    private void DrawBox(ActVariable v)
    {
        Charts.Reset(_box);
        var groups = _cycles.Where(c => c.IsGood)
            .GroupBy(c => c.EquipmentCode)
            .OrderBy(g => g.Key)
            .Where(g => g.Count() >= 5)
            .ToList();
        if (groups.Count == 0)
        {
            Charts.Finish(_box, "데이터 없음");
            return;
        }

        var positions = new List<double>();
        var labels = new List<string>();
        for (var i = 0; i < groups.Count; i++)
        {
            var vals = groups[i].Select(v.Select).ToList();
            var box = new ScottPlot.Box
            {
                Position = i + 1,
                BoxMin = Stats.Percentile(vals, 0.25),
                BoxMax = Stats.Percentile(vals, 0.75),
                BoxMiddle = Stats.Median(vals),
                WhiskerMin = Stats.Percentile(vals, 0.05),
                WhiskerMax = Stats.Percentile(vals, 0.95),
            };
            var bp = _box.Plot.Add.Box(box);
            bp.FillColor = Charts.C(Theme.Teal).WithAlpha(0.45);
            bp.LineColor = Charts.C(Theme.Teal);
            positions.Add(i + 1);
            labels.Add(groups[i].Key);
        }

        _box.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(positions.ToArray(), labels.ToArray());
        Charts.Finish(_box);
    }

    private void DrawSeries(ActVariable v, List<CycleRow> good)
    {
        Charts.Reset(_series, dateAxisBottom: true);
        Charts.Reset(_outliers, dateAxisBottom: true);
        if (_cycles.Count == 0 || good.Count < 5)
        {
            Charts.Finish(_series, "데이터 없음");
            Charts.Finish(_outliers, "데이터 없음");
            return;
        }

        var goodVals = good.Select(v.Select).ToList();
        var p05 = Stats.Percentile(goodVals, 0.05);
        var p95 = Stats.Percentile(goodVals, 0.95);

        // 시간별 값: 양품과 불량을 다른 색으로
        var g = _cycles.Where(c => c.IsGood).ToList();
        var b = _cycles.Where(c => !c.IsGood).ToList();
        var sg = _series.Plot.Add.ScatterPoints(g.Select(c => c.StartedAt.ToOADate()).ToArray(), g.Select(v.Select).ToArray());
        sg.Color = Charts.C(Theme.Teal);
        sg.MarkerSize = 3.5f;
        sg.LegendText = "양품";
        if (b.Count > 0)
        {
            var sb = _series.Plot.Add.ScatterPoints(b.Select(c => c.StartedAt.ToOADate()).ToArray(), b.Select(v.Select).ToArray());
            sb.Color = Charts.C(Theme.Red);
            sb.MarkerSize = 6;
            sb.LegendText = "불량";
        }
        Charts.Line(_series, p05, Theme.Yellow, "범위 하한");
        Charts.Line(_series, p95, Theme.Yellow, "범위 상한");
        Charts.Finish(_series);

        // 범위 이탈: 경계로부터의 차이
        var outside = _cycles.Where(c => v.Select(c) < p05 || v.Select(c) > p95).ToList();
        double Diff(CycleRow c) => v.Select(c) < p05 ? v.Select(c) - p05 : v.Select(c) - p95;

        var og = outside.Where(c => c.IsGood).ToList();
        var ob = outside.Where(c => !c.IsGood).ToList();
        if (og.Count > 0)
        {
            var s1 = _outliers.Plot.Add.ScatterPoints(og.Select(c => c.StartedAt.ToOADate()).ToArray(), og.Select(Diff).ToArray());
            s1.Color = Charts.C(Theme.Orange);
            s1.MarkerSize = 4;
            s1.LegendText = "양품 이탈";
        }
        if (ob.Count > 0)
        {
            var s2 = _outliers.Plot.Add.ScatterPoints(ob.Select(c => c.StartedAt.ToOADate()).ToArray(), ob.Select(Diff).ToArray());
            s2.Color = Charts.C(Theme.Red);
            s2.MarkerSize = 6;
            s2.LegendText = "불량 이탈";
        }
        Charts.Line(_outliers, 0, Theme.Muted, null, dashed: false);

        var badTotal = _cycles.Count(c => !c.IsGood);
        _cardOut.Subtitle = $"이탈 {outside.Count:N0}건 · 불량 {badTotal}건 중 {ob.Count}건({(badTotal == 0 ? 0 : ob.Count * 100.0 / badTotal):0}%) 이탈";
        Charts.Finish(_outliers, outside.Count == 0 ? "범위 이탈 없음" : null);
    }
}
