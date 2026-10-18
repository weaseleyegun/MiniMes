using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMes.Api.Data;
using MiniMes.Api.Models;

namespace MiniMes.Api.Controllers;

[ApiController]
[Route("api/equipment")]
public class EquipmentController : ControllerBase
{
    private readonly MiniMesDbContext _db;

    public EquipmentController(MiniMesDbContext db)
    {
        _db = db;
    }

    // 수집기가 호출: 설비 상태 1건 저장
    [HttpPost("logs")]
    public async Task<IActionResult> AddLog(CreateEquipmentLogRequest req)
    {
        _db.EquipmentLogs.Add(new EquipmentLog
        {
            EquipmentCode = req.EquipmentCode,
            Status = req.Status,
            SpindleRpm = req.SpindleRpm,
            FeedRate = req.FeedRate,
            SpindleLoadPct = req.SpindleLoadPct
        });
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // 최근 수집 이력 (최신순)
    [HttpGet("logs")]
    public async Task<ActionResult<IEnumerable<EquipmentLog>>> GetLogs(string? equipmentCode, int take = 20)
    {
        take = Math.Clamp(take, 1, 200);

        var query = _db.EquipmentLogs.AsQueryable();
        if (!string.IsNullOrEmpty(equipmentCode))
            query = query.Where(l => l.EquipmentCode == equipmentCode);

        return Ok(await query.OrderByDescending(l => l.Id).Take(take).ToListAsync());
    }

    // 설비별 가장 최근 상태 1건씩
    [HttpGet("status")]
    public async Task<ActionResult<IEnumerable<EquipmentLog>>> GetLatestStatus()
    {
        var latest = await _db.EquipmentLogs
            .Where(l => l.Id == _db.EquipmentLogs
                .Where(x => x.EquipmentCode == l.EquipmentCode)
                .Max(x => x.Id))
            .OrderBy(l => l.EquipmentCode)
            .ToListAsync();
        return Ok(latest);
    }
}
