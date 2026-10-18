using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMes.Api.Analysis;
using MiniMes.Api.Data;
using MiniMes.Api.Models;

namespace MiniMes.Api.Controllers;

[ApiController]
[Route("api/analysis")]
public class AnalysisController : ControllerBase
{
    private readonly MiniMesDbContext _db;

    public AnalysisController(MiniMesDbContext db)
    {
        _db = db;
    }

    // 분석 대상 ACT 변수 목록
    private static readonly (string Name, Func<ProductionCycle, double> Selector)[] Variables =
    {
        ("SpindleLoadAvg", c => c.SpindleLoadAvg),
        ("SpindleLoadMax", c => c.SpindleLoadMax),
        ("SpindleRpmAvg",  c => c.SpindleRpmAvg),
        ("FeedRateAvg",    c => c.FeedRateAvg),
        ("VibrationMax",   c => c.VibrationMax),
    };

    // 양품/불량 사이클의 ACT 값을 비교한다.
    // 품목마다 정상 범위가 다르므로, 품목별 양품 평균/표준편차로 z-score를 만든 뒤 비교한다.
    [HttpGet("act")]
    public async Task<ActionResult<ActAnalysisResponse>> Act(string? itemCode, int minGoodPerItem = 20)
    {
        var q = _db.ProductionCycles.AsQueryable();
        if (!string.IsNullOrEmpty(itemCode)) q = q.Where(c => c.ItemCode == itemCode);
        var cycles = await q.ToListAsync();

        var goodZ = Variables.ToDictionary(v => v.Name, _ => new List<double>());
        var defectZ = Variables.ToDictionary(v => v.Name, _ => new List<double>());
        int itemCount = 0, goodCount = 0, defectCount = 0;

        foreach (var group in cycles.GroupBy(c => c.ItemCode))
        {
            var good = group.Where(c => c.IsGood).ToList();
            var defect = group.Where(c => !c.IsGood).ToList();
            if (good.Count < minGoodPerItem || defect.Count == 0) continue;

            itemCount++;
            goodCount += good.Count;
            defectCount += defect.Count;

            foreach (var (name, selector) in Variables)
            {
                var baseline = good.Select(selector).ToList();
                var mean = Stats.Mean(baseline);
                var sd = Stats.StdDev(baseline);
                if (sd < 1e-9) continue;

                goodZ[name].AddRange(good.Select(c => (selector(c) - mean) / sd));
                defectZ[name].AddRange(defect.Select(c => (selector(c) - mean) / sd));
            }
        }

        var results = new List<ActVariableResult>();
        foreach (var (name, _) in Variables)
        {
            var g = goodZ[name];
            var d = defectZ[name];
            if (g.Count == 0 || d.Count == 0) continue;

            // Cohen's d: 두 집단 평균 차이를 합동 표준편차로 나눈 효과 크기
            var pooled = Math.Sqrt(((g.Count - 1) * Math.Pow(Stats.StdDev(g), 2)
                                  + (d.Count - 1) * Math.Pow(Stats.StdDev(d), 2))
                                  / (g.Count + d.Count - 2));
            var cohensD = pooled < 1e-9 ? 0 : (Stats.Mean(d) - Stats.Mean(g)) / pooled;

            // 관리한계(±3σ) 이탈 비율
            var defectOut = d.Count(z => Math.Abs(z) > 3) * 100.0 / d.Count;
            var goodOut = g.Count(z => Math.Abs(z) > 3) * 100.0 / g.Count;

            var abs = Math.Abs(cohensD);
            var judgement = abs >= 0.8 ? "강한 관련" : abs >= 0.5 ? "중간 관련" : abs >= 0.2 ? "약한 관련" : "관련 낮음";

            results.Add(new ActVariableResult(
                name, Math.Round(cohensD, 2), Math.Round(defectOut, 1), Math.Round(goodOut, 1), judgement));
        }

        return Ok(new ActAnalysisResponse(
            goodCount, defectCount, itemCount,
            results.OrderByDescending(r => Math.Abs(r.CohensD)).ToList()));
    }
}
