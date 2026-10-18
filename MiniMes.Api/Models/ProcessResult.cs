using System.Text.Json.Serialization;

namespace MiniMes.Api.Models;

public class ProcessResult
{
    public int Id { get; set; }

    // 공정 코드 (예: GRINDING, INSPECTION)
    public string ProcessCode { get; set; } = "";

    public string EquipmentCode { get; set; } = "";

    public int GoodQty { get; set; }

    public int DefectQty { get; set; }

    // 실제 소요 시간(초). 표준 Cycle Time과 비교하는 데 쓴다.
    public double CycleTimeSec { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.Now;

    public int LotId { get; set; }

    [JsonIgnore]
    public Lot? Lot { get; set; }
}
