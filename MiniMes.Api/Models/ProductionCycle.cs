namespace MiniMes.Api.Models;

// 가공 1회(사이클)의 결과와 PLC ACT(실측) 요약값
public class ProductionCycle
{
    public long Id { get; set; }

    public string ItemCode { get; set; } = "";

    public string EquipmentCode { get; set; } = "";

    public string? LotNo { get; set; }

    public DateTime StartedAt { get; set; }

    public double CycleTimeSec { get; set; }

    public bool IsGood { get; set; }

    // ---- ACT 요약값 ----
    public double SpindleLoadAvg { get; set; }

    public double SpindleLoadMax { get; set; }

    public double SpindleRpmAvg { get; set; }

    public double FeedRateAvg { get; set; }

    public double VibrationMax { get; set; }
}
