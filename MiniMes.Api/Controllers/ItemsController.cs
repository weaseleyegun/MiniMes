using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMes.Api.Data;
using MiniMes.Api.Models;

namespace MiniMes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly MiniMesDbContext _db;

    public ItemsController(MiniMesDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Item>>> GetAll()
    {
        return Ok(await _db.Items.OrderBy(i => i.ItemCode).ToListAsync());
    }

    [HttpGet("{itemCode}")]
    public async Task<ActionResult<Item>> GetByCode(string itemCode)
    {
        var item = await _db.Items.FirstOrDefaultAsync(i => i.ItemCode == itemCode);
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Item>> Create(CreateItemRequest req)
    {
        if (await _db.Items.AnyAsync(i => i.ItemCode == req.ItemCode))
            return Conflict($"이미 존재하는 품목 코드입니다: {req.ItemCode}");

        var item = new Item
        {
            ItemCode = req.ItemCode,
            Name = req.Name,
            Material = req.Material,
            Diameter = req.Diameter,
            CutLength = req.CutLength,
            FluteCount = req.FluteCount
        };
        _db.Items.Add(item);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetByCode), new { itemCode = item.ItemCode }, item);
    }
}
