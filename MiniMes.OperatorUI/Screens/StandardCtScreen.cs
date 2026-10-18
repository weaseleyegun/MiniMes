using MiniMes.OperatorUI.Controls;
using ScottPlot.WinForms;

namespace MiniMes.OperatorUI.Screens;

// 화면 1. 표준 C/T 체계 구축 및 예측
// 품번별 실측 C/T를 표준·예측과 비교하고, 설비 실시간 값을 보여주며, 신규 품번의 C/T를 예측한다.
public class StandardCtScreen : ScreenBase
{
    private List<ItemRow> _items = new();
    private List<string> _equipCodes = new();
    private bool _ticking;

    // 조회 조건
    private readonly ComboBox _cboEquip = Ui.Combo(130);
    private readonly ComboBox _cboItem = Ui.Combo(150);
    private readonly DateTimePicker _dtFrom = Ui.DatePicker(DateTime.Today.AddDays(-29));
    private readonly DateTimePicker _dtTo = Ui.DatePicker(DateTime.Today);
    private readonly Button _btnQuery = Ui.Primary("조회 및 분석", 330);
    private readonly Button _btnRecalc = Ui.Secondary("표준 C/T 재계산");
    private readonly Button _btnTrain = Ui.Secondary("예측 모델 재학습");
    private readonly Label _lblSummary = new();

    // 요약 카드
    private readonly KpiCard _kpiStd = new("표준 C/T", Theme.Green);
    private readonly KpiCard _kpiPred = new("예측 C/T", Theme.Purple);
    private readonly KpiCard _kpiAcc = new("예측 정확도", Theme.Orange);

    // 차트
    private readonly FormsPlot _live = Charts.Create();
    private readonly FormsPlot _ctPlot = Charts.Create();
    private readonly Label _liveInfo = new();

    // 분석 상세 + 신규 품번 예측
    private readonly Label _lblDetail = new();
    private readonly ComboBox _cboMaterial = Ui.Combo(110);
    private readonly NumericUpDown _numD = Ui.Number(14, 0.1m, 500, 0.5m, 1, 62);
    private readonly NumericUpDown _numL = Ui.Number(28, 0.1m, 1000, 0.5m, 1, 62);
    private readonly NumericUpDown _numF = Ui.Number(4, 1, 12, 1, 0, 48);
    private readonly Button _btnPredict = Ui.Primary("신규 품번 C/T 예측");
    private readonly Label _lblNew = new();

    private readonly CardPanel _card1 = new("조회 조건 · C/T 요약");
    private readonly CardPanel _card2 = new("설비 실시간 모니터링", "부하 · RPM · Feed 최근 값 (0~100 환산)");
    private readonly CardPanel _card3 = new("C/T 시계열 · 실측 / 표준 / 예측", "점을 드래그해 확대, 더블클릭으로 원래대로");
    private readonly CardPanel _card4 = new("C/T 분석 상세 · 신규 품번 예측");

    public StandardCtScreen(ApiClient api)
        : base(api, "01 / STANDARD CYCLE TIME", "표준 C/T 체계 구축 및 예측",
               "실측 이력에서 기준을 확인하고, 신규 품번의 가공시간을 예측합니다.")
    {
        _cboMaterial.Items.AddRange(new object[] { "Steel", "Stainless", "Aluminum", "Carbide" });
        _cboMaterial.SelectedIndex = 0;

        BuildCard1();
        BuildCard2();
        _card3.Body.Controls.Add(_ctPlot);
        BuildCard4();

        var grid = Grid(new float[] { 46, 54 }, new float[] { 54, 46 });
        grid.Controls.Add(_card1, 0, 0);
        grid.Controls.Add(_card2, 1, 0);
        grid.Controls.Add(_card3, 0, 1);
        grid.Controls.Add(_card4, 1, 1);
        Content.Controls.Add(grid);

        WireEvents();
    }

    public override IReadOnlyList<(string Number, Control Target)> Callouts => new (string, Control)[]
    {
        ("1", _card1), ("2", _card2), ("3", _card3), ("4", _card4),
    };

    // ---------------------------------------------------------------- 화면 구성

    private void BuildCard1()
    {
        _lblSummary.Dock = DockStyle.Fill;
        _lblSummary.ForeColor = Theme.Muted;
        _lblSummary.Font = Theme.Font(8.5f);
        _lblSummary.Margin = new Padding(0);
        _lblSummary.TextAlign = ContentAlignment.MiddleLeft;

        var kpis = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0) };
        for (var i = 0; i < 3; i++) kpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        kpis.Controls.Add(_kpiStd, 0, 0);
        kpis.Controls.Add(_kpiPred, 1, 0);
        kpis.Controls.Add(_kpiAcc, 2, 0);

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Margin = new Padding(0) };
        foreach (var h in new[] { 38, 38, 42, 24 }) body.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        body.Controls.Add(Ui.Row(Ui.Caption("설비"), _cboEquip, Ui.Caption("품번"), _cboItem), 0, 0);
        body.Controls.Add(Ui.Row(Ui.Caption("시작일"), _dtFrom, Ui.Caption("종료일"), _dtTo,
            QuickButton("1일", 1), QuickButton("7일", 7), QuickButton("30일", 30)), 0, 1);
        body.Controls.Add(Ui.Row(_btnQuery), 0, 2);
        body.Controls.Add(_lblSummary, 0, 3);
        body.Controls.Add(kpis, 0, 4);
        body.Controls.Add(Ui.Row(_btnRecalc, _btnTrain), 0, 5);
        _card1.Body.Controls.Add(body);
    }

    private Button QuickButton(string text, int days)
    {
        var btn = Ui.Secondary(text, 50);
        btn.Click += async (_, _) =>
        {
            _dtTo.Value = DateTime.Today;
            _dtFrom.Value = DateTime.Today.AddDays(-(days - 1));
            await RunAsync(QueryAsync);
        };
        return btn;
    }

    private void BuildCard2()
    {
        _liveInfo.Dock = DockStyle.Bottom;
        _liveInfo.Height = 24;
        _liveInfo.ForeColor = Theme.Muted;
        _liveInfo.Font = Theme.Font(9);
        _liveInfo.TextAlign = ContentAlignment.MiddleLeft;

        _card2.Body.Controls.Add(_live);        // Fill 먼저
        _card2.Body.Controls.Add(_liveInfo);
    }

    private void BuildCard4()
    {
        _lblDetail.Dock = DockStyle.Fill;
        _lblDetail.ForeColor = Theme.Text;
        _lblDetail.Font = Theme.Font(9.5f);
        _lblDetail.Padding = new Padding(0, 4, 8, 0);
        _lblDetail.Text = "조회하면 이곳에 표본 통계가 표시됩니다.";

        _lblNew.Dock = DockStyle.Fill;
        _lblNew.ForeColor = Theme.Muted;
        _lblNew.Font = Theme.Font(9);
        _lblNew.Margin = new Padding(0, 6, 0, 0);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Margin = new Padding(0) };
        foreach (var h in new[] { 26, 38, 38, 40 }) right.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.Controls.Add(new Label
        {
            Text = "신규 품번 사양으로 C/T 예측",
            ForeColor = Theme.Teal,
            Font = Theme.Font(9, true),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft,
        }, 0, 0);
        right.Controls.Add(Ui.Row(Ui.Caption("재질"), _cboMaterial), 0, 1);
        right.Controls.Add(Ui.Row(Ui.Caption("직경"), _numD, Ui.Caption("길이"), _numL, Ui.Caption("날 수"), _numF), 0, 2);
        right.Controls.Add(Ui.Row(_btnPredict), 0, 3);
        right.Controls.Add(_lblNew, 0, 4);

        var split = Grid(new float[] { 52, 48 }, new float[] { 100 });
        split.Controls.Add(_lblDetail, 0, 0);
        split.Controls.Add(right, 1, 0);
        _card4.Body.Controls.Add(split);
    }

    private void WireEvents()
    {
        _btnQuery.Click += async (_, _) => await RunAsync(QueryAsync);

        _btnRecalc.Click += async (_, _) => await RunAsync(async () =>
        {
            var res = await Api.RecalculateStandardsAsync();
            _items = await Api.GetItemsAsync();
            await QueryAsync();
            _lblSummary.Text = $"표준 C/T 재계산 완료: {res.Updated.Count}개 품번 갱신, {res.SkippedItemCodes.Count}개 표본 부족";
        });

        _btnTrain.Click += async (_, _) => await RunAsync(async () =>
        {
            var info = await Api.TrainModelAsync();
            await QueryAsync();
            _lblSummary.Text = $"예측 모델 재학습 완료: 품번 {info.NSamples}개, 교차검증 평균 오차 {info.LooMapePct:0.00}%";
        });

        _btnPredict.Click += async (_, _) =>
        {
            try
            {
                var r = await Api.PredictAsync(new PredictRequest(
                    _cboMaterial.SelectedItem?.ToString() ?? "Steel",
                    (double)_numD.Value, (double)_numL.Value, (int)_numF.Value));
                _lblNew.ForeColor = r.Warning is null ? Theme.Text : Theme.Yellow;
                _lblNew.Text = $"예측 C/T  {r.PredictedCtSec:0.0}초\r\n범위  {r.LowCtSec:0.0} ~ {r.HighCtSec:0.0}초 (±{r.ExpectedErrorPct:0.00}%)"
                               + (r.Warning is null ? "" : "\r\n" + r.Warning);
            }
            catch (Exception ex)
            {
                _lblNew.ForeColor = Theme.Yellow;
                _lblNew.Text = "예측 실패: " + ex.Message;
            }
        };
    }

    // ---------------------------------------------------------------- 데이터

    public override async Task OnShownAsync() => await RunAsync(async () =>
    {
        _items = await Api.GetItemsAsync();
        var board = await Api.GetEquipmentBoardAsync();
        _equipCodes = board.Select(b => b.EquipmentCode).ToList();

        if (_cboEquip.Items.Count == 0)
        {
            _cboEquip.Items.Add("전체 설비");
            foreach (var code in _equipCodes) _cboEquip.Items.Add(code);
            _cboEquip.SelectedIndex = 0;

            foreach (var item in _items) _cboItem.Items.Add(item.ItemCode);
            var first = _items.FirstOrDefault(i => i.StandardCycleTimeSec is not null) ?? _items.FirstOrDefault();
            if (first is not null) _cboItem.SelectedItem = first.ItemCode;
        }

        await QueryAsync();
        await TickAsync();
    });

    private async Task QueryAsync()
    {
        if (_cboItem.SelectedItem is not string code) return;

        var item = _items.First(i => i.ItemCode == code);
        var equip = _cboEquip.SelectedIndex > 0 ? _cboEquip.SelectedItem as string : null;
        var from = _dtFrom.Value.Date;
        var to = _dtTo.Value.Date.AddDays(1).AddSeconds(-1);

        var cycles = await Api.GetCyclesAsync(code, equip, from, to);

        PredictionResult? pred = null;
        string? predError = null;
        try
        {
            pred = await Api.PredictAsync(new PredictRequest(item.Material, item.Diameter, item.CutLength, item.FluteCount));
        }
        catch (Exception ex)
        {
            predError = ex.Message;
        }

        ShowKpis(item.StandardCycleTimeSec, pred, predError);
        _lblSummary.Text = $"조회 완료 · {code} · {from:yyyy-MM-dd} ~ {to:yyyy-MM-dd} · {cycles.Count}건 " +
                           $"(양품 {cycles.Count(c => c.IsGood)} / 불량 {cycles.Count(c => !c.IsGood)})";

        DrawCycleChart(cycles, item.StandardCycleTimeSec, pred);
        ShowDetail(cycles, item.StandardCycleTimeSec, pred);
        MarkUpdated();
    }

    private void ShowKpis(double? std, PredictionResult? pred, string? predError)
    {
        _kpiStd.Set(std is { } s ? $"{s:0.00}" : "-",
            std is null ? "표준 미산정 · 재계산 필요" : "양품 사이클 중앙값 · 초");

        if (pred is null)
        {
            _kpiPred.Set("-", Shorten(predError ?? "예측 불가", 22));
            _kpiAcc.Set("-", "예측값 없음");
            return;
        }

        _kpiPred.Set($"{pred.PredictedCtSec:0.00}", $"사양 기반 모델 · ±{pred.ExpectedErrorPct:0.0}%");
        if (std is { } sv)
            _kpiAcc.Set($"{100 - Math.Abs(pred.PredictedCtSec - sv) / sv * 100:0.00}%", "표준 C/T 대비 일치율");
        else
            _kpiAcc.Set("-", "표준 C/T 없음");
    }

    private void DrawCycleChart(List<CycleRow> cycles, double? std, PredictionResult? pred)
    {
        Charts.Reset(_ctPlot, dateAxisBottom: true);
        if (cycles.Count == 0)
        {
            Charts.Finish(_ctPlot, "조회된 사이클이 없습니다");
            return;
        }

        var good = cycles.Where(c => c.IsGood).ToList();
        var bad = cycles.Where(c => !c.IsGood).ToList();

        var sg = _ctPlot.Plot.Add.ScatterPoints(
            good.Select(c => c.StartedAt.ToOADate()).ToArray(), good.Select(c => c.CycleTimeSec).ToArray());
        sg.Color = Charts.C(Theme.Accent);
        sg.MarkerSize = 4;
        sg.LegendText = "실측 C/T (양품)";

        if (bad.Count > 0)
        {
            var sb = _ctPlot.Plot.Add.ScatterPoints(
                bad.Select(c => c.StartedAt.ToOADate()).ToArray(), bad.Select(c => c.CycleTimeSec).ToArray());
            sb.Color = Charts.C(Theme.Red);
            sb.MarkerSize = 5;
            sb.LegendText = "실측 C/T (불량)";
        }

        if (std is { } s) Charts.Line(_ctPlot, s, Theme.Green, "표준 C/T");
        if (pred is not null) Charts.Line(_ctPlot, pred.PredictedCtSec, Theme.Purple, "예측 C/T");
        Charts.Finish(_ctPlot);
    }

    private void ShowDetail(List<CycleRow> cycles, double? std, PredictionResult? pred)
    {
        var good = cycles.Where(c => c.IsGood).Select(c => c.CycleTimeSec).ToList();
        if (good.Count == 0)
        {
            _lblDetail.Text = "조회된 양품 사이클이 없습니다.";
            return;
        }

        var q1 = Stats.Percentile(good, 0.25);
        var q3 = Stats.Percentile(good, 0.75);
        var iqr = q3 - q1;
        var outliers = good.Count(v => v < q1 - 1.5 * iqr || v > q3 + 1.5 * iqr);
        var mean = Stats.Mean(good);

        var lines = new List<string>
        {
            $"조회 사이클   {cycles.Count:N0}건 (양품 {good.Count:N0} / 불량 {cycles.Count - good.Count:N0})",
            $"평균 실측 C/T   {mean:0.0}초",
            $"표준편차   {Stats.StdDev(good):0.0}초",
            $"이상치(IQR)   {outliers}건 ({outliers * 100.0 / good.Count:0.0}%)",
        };
        if (std is { } s)
        {
            lines.Add($"표준 C/T   {s:0.0}초");
            lines.Add($"표준 대비 평균 편차   {(mean - s) / s * 100:+0.0;-0.0;0.0}%");
        }
        if (pred is not null)
            lines.Add($"예측 C/T   {pred.PredictedCtSec:0.0}초 (범위 {pred.LowCtSec:0.0} ~ {pred.HighCtSec:0.0})");

        _lblDetail.Text = string.Join("\r\n\r\n", lines);
    }

    // 2초마다: 선택한 설비(전체이면 첫 설비)의 최근 값을 그린다.
    public override async Task TickAsync()
    {
        if (_ticking) return;
        _ticking = true;
        try
        {
            var code = _cboEquip.SelectedIndex > 0 ? _cboEquip.SelectedItem as string : _equipCodes.FirstOrDefault();
            if (code is null) return;

            var logs = await Api.GetEquipmentLogsAsync(code, 60);
            Charts.Reset(_live, dateAxisBottom: true);
            if (logs.Count == 0)
            {
                Charts.Finish(_live, "수집된 데이터가 없습니다");
                return;
            }

            var xs = logs.Select(l => l.CollectedAt.ToOADate()).ToArray();
            AddLiveLine(xs, logs.Select(l => l.SpindleLoadPct).ToArray(), Theme.Orange, "주축부하 (%)");
            AddLiveLine(xs, logs.Select(l => l.SpindleRpm / 100.0).ToArray(), Theme.Teal, "RPM (÷100)");
            AddLiveLine(xs, logs.Select(l => l.FeedRate / 4.0).ToArray(), Theme.Yellow, "Feed (÷4)");
            Charts.Finish(_live);
            _live.Plot.Axes.SetLimitsY(0, 105);
            _live.Refresh();

            var last = logs[^1];
            _liveInfo.Text = $"{code} · {Theme.StatusText(last.Status)} · 부하 {last.SpindleLoadPct:0}% · RPM {last.SpindleRpm:N0} · Feed {last.FeedRate:0} · 수신 {last.CollectedAt:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            _liveInfo.Text = "실시간 데이터 없음: " + ex.Message;
        }
        finally
        {
            _ticking = false;
        }
    }

    private void AddLiveLine(double[] xs, double[] ys, System.Drawing.Color color, string legend)
    {
        var line = _live.Plot.Add.Scatter(xs, ys);
        line.Color = Charts.C(color);
        line.LineWidth = 2;
        line.MarkerSize = 0;
        line.LegendText = legend;
    }

    private static string Shorten(string text, int max) => text.Length <= max ? text : text[..max] + "…";
}
