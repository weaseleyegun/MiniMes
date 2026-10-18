using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMes.Api.Data;
using MiniMes.Api.Models;

namespace MiniMes.Api.Controllers;

// 작업자 화면(VB)이 주기적으로 호출하는 설비 현황 API
[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly MiniMesDbContext _db;

    public DashboardController(MiniMesDbContext db)
    {
        _db = db;
    }

    [HttpGet("equipment")]
    public async Task<ActionResult<IEnumerable<EquipmentBoardRow>>> Equipment()
    {
        // 설비별 가장 최근 상태 로그
        var latestLogs = await _db.EquipmentLogs
            .Where(l => l.Id == _db.EquipmentLogs
                .Where(x => x.EquipmentCode == l.EquipmentCode)
                .Max(x => x.Id))
            .ToListAsync();

        // 최근 사이클 (설비별 최근 100건만 쓴다)
        var recent = await _db.ProductionCycles
            .OrderByDescending(c => c.Id)
            .Take(1000)
            .ToListAsync();
        var cyclesByEq = recent
            .GroupBy(c => c.EquipmentCode)
            .ToDictionary(g => g.Key, g => g.Take(100).ToList());

        var standards = await _db.Items
            .Where(i => i.StandardCycleTimeSec != null)
            .ToDictionaryAsync(i => i.ItemCode, i => i.StandardCycleTimeSec!.Value);

        var codes = latestLogs.Select(l => l.EquipmentCode)
            .Union(cyclesByEq.Keys)
            .OrderBy(c => c)
            .ToList();

        var rows = new List<EquipmentBoardRow>();
        foreach (var code in codes)
        {
            var log = latestLogs.FirstOrDefault(l => l.EquipmentCode == code);
            cyclesByEq.TryGetValue(code, out var cycles);
            cycles ??= new List<ProductionCycle>();

            var last = cycles.FirstOrDefault();   // 가장 최근 사이클
            var defects = cycles.Count(c => !c.IsGood);

            double? std = null, deviation = null;
            var judgement = "표준 없음";
            if (last is not null && standards.TryGetValue(last.ItemCode, out var s))
            {
                std = s;
                deviation = Math.Round((last.CycleTimeSec - s) / s * 100, 1);
                judgement = Math.Abs(deviation.Value) switch
                {
                    <= 5 => "정상",
                    <= 10 => "주의",
                    _ => "초과"
                };
            }

            rows.Add(new EquipmentBoardRow(
                code,
                log?.Status.ToString() ?? "Unknown",
                log?.SpindleRpm ?? 0,
                log?.FeedRate ?? 0,
                log?.SpindleLoadPct ?? 0,
                log?.CollectedAt,
                cycles.Count,
                defects,
                cycles.Count == 0 ? 0 : Math.Round(defects * 100.0 / cycles.Count, 1),
                last?.ItemCode,
                last?.CycleTimeSec,
                std,
                deviation,
                judgement));
        }
        return Ok(rows);
    }
}
