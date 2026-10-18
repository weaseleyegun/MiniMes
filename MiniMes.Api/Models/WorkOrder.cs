using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MiniMes.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorkOrderStatus
{
    Planned,
    InProgress,
    Completed
}

public class WorkOrder
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    public string OrderNo { get; set; } = "";

    [Required, StringLength(30)]
    public string ItemCode { get; set; } = "";

    [Range(1, 100000)]
    public int Quantity { get; set; }

    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Planned;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonIgnore]
    public List<Lot> Lots { get; set; } = new();
}
