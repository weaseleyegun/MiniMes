using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMes.Api.Data;
using MiniMes.Api.Models;

namespace MiniMes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CyclesController : ControllerBase
{
    private readonly MiniMesDbContext _db;

    public CyclesController(MiniMesDbContext db)
    {
        _db = db;
    }

    // 사이클 1건 저장 (수집기 실시간 전송용)
    [HttpPost]
    public async Task<IActionResult> Add(CreateCycleRequest req)
    {
        _db.ProductionCycles.Add(ToEntity(req));
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // 여러 건 한 번에 저장 (과거 이력 채우기용)
    [HttpPost("batch")]
    public async Task<IActionResult> AddBatch(List<CreateCycleRequest> reqs)
    {
        _db.ProductionCycles.AddRange(reqs.Select(ToEntity));
        await _db.SaveChangesAsync();
        return Ok(new { saved = reqs.Count });
    }

    // 조회 (최신순). from/to는 사이클 시작 시각 기준 기간 필터.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductionCycle>>> Get(
        string? itemCode, string? equipmentCode, bool? isGood,
        DateTime? from, DateTime? to, int take = 50)
    {
        take = Math.Clamp(take, 1, 5000);

        var q = _db.ProductionCycles.AsQueryable();
        if (!string.IsNullOrEmpty(itemCode)) q = q.Where(c => c.ItemCode == itemCode);
        if (!string.IsNullOrEmpty(equipmentCode)) q = q.Where(c => c.EquipmentCode == equipmentCode);
        if (isGood is not null) q = q.Where(c => c.IsGood == isGood);
        if (from is not null) q = q.Where(c => c.StartedAt >= from);
        if (to is not null) q = q.Where(c => c.StartedAt <= to);

        return Ok(await q.OrderByDescending(c => c.Id).Take(take).ToListAsync());
    }

    // 전체 건수
    [HttpGet("count")]
    public async Task<ActionResult<int>> Count()
    {
        return Ok(await _db.ProductionCycles.CountAsync());
    }

    private static ProductionCycle ToEntity(CreateCycleRequest r) => new()
    {
        ItemCode = r.ItemCode,
        EquipmentCode = r.EquipmentCode,
        LotNo = r.LotNo,
        StartedAt = r.StartedAt,
        CycleTimeSec = r.CycleTimeSec,
        IsGood = r.IsGood,
        SpindleLoadAvg = r.SpindleLoadAvg,
        SpindleLoadMax = r.SpindleLoadMax,
        SpindleRpmAvg = r.SpindleRpmAvg,
        FeedRateAvg = r.FeedRateAvg,
        VibrationMax = r.VibrationMax
    };
}
