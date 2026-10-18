using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMes.Api.Analysis;
using MiniMes.Api.Data;
using MiniMes.Api.Models;

namespace MiniMes.Api.Controllers;

[ApiController]
[Route("api/standards")]
public class StandardsController : ControllerBase
{
    private readonly MiniMesDbContext _db;

    public StandardsController(MiniMesDbContext db)
    {
        _db = db;
    }

    // 품목별 표준 Cycle Time 재계산.
    // 양품 사이클만 사용하고, IQR 방식으로 이상치를 제거한 뒤 중앙값을 표준으로 정한다.
    [HttpPost("recalculate")]
    public async Task<ActionResult<RecalculateResponse>> Recalculate(int minSamples = 30)
    {
        var rows = await _db.ProductionCycles
            .Where(c => c.IsGood)
            .Select(c => new { c.ItemCode, c.CycleTimeSec })
            .ToListAsync();

        var items = await _db.Items.ToDictionaryAsync(i => i.ItemCode);

        var updated = new List<StandardResult>();
        var skipped = new List<string>();

        foreach (var group in rows.GroupBy(r => r.ItemCode))
        {
            if (!items.TryGetValue(group.Key, out var item)) continue;

            var sorted = group.Select(r => r.CycleTimeSec).OrderBy(v => v).ToList();
            if (sorted.Count < minSamples)
            {
                skipped.Add(group.Key);
                continue;
            }

            // IQR 이상치 제거: Q1 - 1.5*IQR ~ Q3 + 1.5*IQR 밖의 값을 버린다.
            var q1 = Stats.Percentile(sorted, 0.25);
            var q3 = Stats.Percentile(sorted, 0.75);
            var iqr = q3 - q1;
            var kept = sorted.Where(v => v >= q1 - 1.5 * iqr && v <= q3 + 1.5 * iqr).ToList();

            var standard = Math.Round(Stats.Median(kept), 1);
            var previous = item.StandardCycleTimeSec;

            item.StandardCycleTimeSec = standard;
            item.StandardUpdatedAt = DateTime.Now;

            updated.Add(new StandardResult(
                group.Key, sorted.Count, sorted.Count - kept.Count,
                standard, Math.Round(Stats.StdDev(kept), 2), previous));
        }

        await _db.SaveChangesAsync();
        return Ok(new RecalculateResponse(updated.OrderBy(u => u.ItemCode).ToList(), skipped));
    }

    // 표준 CT와 최근 실적(품목별 최근 양품 20건 평균) 비교
    [HttpGet("comparison")]
    public async Task<ActionResult<IEnumerable<StandardComparisonRow>>> Comparison()
    {
        var items = await _db.Items.OrderBy(i => i.ItemCode).ToListAsync();

        var recent = await _db.ProductionCycles
            .Where(c => c.IsGood)
            .OrderByDescending(c => c.Id)
            .Take(5000)
            .Select(c => new { c.ItemCode, c.CycleTimeSec })
            .ToListAsync();

        var recentByItem = recent
            .GroupBy(r => r.ItemCode)
            .ToDictionary(g => g.Key, g => g.Take(20).Select(r => r.CycleTimeSec).ToList());

        var result = new List<StandardComparisonRow>();
        foreach (var item in items)
        {
            recentByItem.TryGetValue(item.ItemCode, out var values);
            double? avg = values is { Count: > 0 } ? Math.Round(Stats.Mean(values), 1) : null;

            double? deviation = null;
            var judgement = "표준 없음";
            if (item.StandardCycleTimeSec is { } std && avg is { } a)
            {
                deviation = Math.Round((a - std) / std * 100, 1);
                judgement = Math.Abs(deviation.Value) switch
                {
                    <= 5 => "정상",
                    <= 10 => "주의",
                    _ => "초과"
                };
            }
            else if (item.StandardCycleTimeSec is not null)
            {
                judgement = "실적 없음";
            }

            result.Add(new StandardComparisonRow(
                item.ItemCode, item.Name, item.StandardCycleTimeSec, avg,
                values?.Count ?? 0, deviation, judgement));
        }
        return Ok(result);
    }
}
