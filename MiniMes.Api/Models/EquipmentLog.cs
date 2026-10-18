using System.Text.Json.Serialization;

namespace MiniMes.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EquipmentStatus
{
    Run,
    Idle,
    Alarm
}

// 설비에서 주기적으로 수집한 한 건의 상태 기록
public class EquipmentLog
{
    public long Id { get; set; }

    public string EquipmentCode { get; set; } = "";

    public EquipmentStatus Status { get; set; }

    public double SpindleRpm { get; set; }

    public double FeedRate { get; set; }

    public double SpindleLoadPct { get; set; }

    public DateTime CollectedAt { get; set; } = DateTime.Now;
}
