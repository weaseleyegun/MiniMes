using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMes.Api.Data;
using MiniMes.Api.Models;

namespace MiniMes.Api.Controllers;

[ApiController]
public class LotsController : ControllerBase
{
    private readonly MiniMesDbContext _db;

    public LotsController(MiniMesDbContext db)
    {
        _db = db;
    }

    // 작업지시 하나에 LOT 생성
    [HttpPost("api/workorders/{workOrderId}/lots")]
    public async Task<ActionResult<Lot>> CreateLot(int workOrderId, CreateLotRequest req)
    {
        if (!await _db.WorkOrders.AnyAsync(w => w.Id == workOrderId))
            return NotFound($"작업지시 {workOrderId}가 없습니다.");

        if (await _db.Lots.AnyAsync(l => l.LotNo == req.LotNo))
            return Conflict($"이미 존재하는 LOT 번호입니다: {req.LotNo}");

        var lot = new Lot
        {
            LotNo = req.LotNo,
            Quantity = req.Quantity,
            WorkOrderId = workOrderId
        };
        _db.Lots.Add(lot);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetLot), new { id = lot.Id }, lot);
    }

    // 작업지시의 LOT 목록
    [HttpGet("api/workorders/{workOrderId}/lots")]
    public async Task<ActionResult<IEnumerable<Lot>>> GetLotsByWorkOrder(int workOrderId)
    {
        var lots = await _db.Lots
            .Where(l => l.WorkOrderId == workOrderId)
            .OrderBy(l => l.Id)
            .ToListAsync();
        return Ok(lots);
    }

    // LOT 상세 (공정 실적 포함)
    [HttpGet("api/lots/{id}")]
    public async Task<ActionResult<Lot>> GetLot(int id)
    {
        var lot = await _db.Lots
            .Include(l => l.Results)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (lot is null) return NotFound();
        return Ok(lot);
    }

    // 공정 실적 입력
    [HttpPost("api/lots/{lotId}/results")]
    public async Task<ActionResult<ProcessResult>> AddResult(int lotId, CreateResultRequest req)
    {
        var lot = await _db.Lots.FindAsync(lotId);
        if (lot is null) return NotFound($"LOT {lotId}가 없습니다.");

        if (req.GoodQty + req.DefectQty == 0)
            return BadRequest("양품 수량과 불량 수량이 모두 0일 수 없습니다.");

        var result = new ProcessResult
        {
            LotId = lotId,
            ProcessCode = req.ProcessCode,
            EquipmentCode = req.EquipmentCode,
            GoodQty = req.GoodQty,
            DefectQty = req.DefectQty,
            CycleTimeSec = req.CycleTimeSec
        };
        _db.ProcessResults.Add(result);

        // 첫 실적이 들어오면 LOT 상태를 진행중으로 변경
        if (lot.Status == LotStatus.Created)
            lot.Status = LotStatus.InProcess;

        await _db.SaveChangesAsync();
        return Created($"api/lots/{lotId}", result);
    }
}
