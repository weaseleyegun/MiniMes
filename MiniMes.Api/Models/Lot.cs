using System.Text.Json.Serialization;

namespace MiniMes.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LotStatus
{
    Created,
    InProcess,
    Completed
}

public class Lot
{
    public int Id { get; set; }

    public string LotNo { get; set; } = "";

    public int Quantity { get; set; }

    public LotStatus Status { get; set; } = LotStatus.Created;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // FK: 이 LOT이 속한 작업지시
    public int WorkOrderId { get; set; }

    [JsonIgnore]
    public WorkOrder? WorkOrder { get; set; }

    public List<ProcessResult> Results { get; set; } = new();
}
