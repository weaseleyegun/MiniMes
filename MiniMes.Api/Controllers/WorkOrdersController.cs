using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMes.Api.Data;
using MiniMes.Api.Models;

namespace MiniMes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkOrdersController : ControllerBase
{
    private readonly MiniMesDbContext _db;

    public WorkOrdersController(MiniMesDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkOrder>>> GetAll()
    {
        return Ok(await _db.WorkOrders.OrderByDescending(o => o.Id).ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkOrder>> GetById(int id)
    {
        var order = await _db.WorkOrders.FindAsync(id);
        if (order is null) return NotFound();
        return Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<WorkOrder>> Create(WorkOrder input)
    {
        if (await _db.WorkOrders.AnyAsync(o => o.OrderNo == input.OrderNo))
            return Conflict($"이미 존재하는 작업지시 번호입니다: {input.OrderNo}");

        input.Id = 0;
        input.CreatedAt = DateTime.Now;
        _db.WorkOrders.Add(input);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, WorkOrder input)
    {
        var order = await _db.WorkOrders.FindAsync(id);
        if (order is null) return NotFound();

        order.OrderNo = input.OrderNo;
        order.ItemCode = input.ItemCode;
        order.Quantity = input.Quantity;
        order.Status = input.Status;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _db.WorkOrders.FindAsync(id);
        if (order is null) return NotFound();

        _db.WorkOrders.Remove(order);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
