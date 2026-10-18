namespace MiniMes.Api.Models;

// 품목 마스터: 절삭공구 사양
public class Item
{
    public int Id { get; set; }

    public string ItemCode { get; set; } = "";

    public string Name { get; set; } = "";

    // Steel, Stainless, Aluminum, Carbide
    public string Material { get; set; } = "";

    public double Diameter { get; set; }

    public double CutLength { get; set; }

    public int FluteCount { get; set; }

    // 표준화(Step 8)에서 계산해 채우는 값. 이력이 없으면 null.
    public double? StandardCycleTimeSec { get; set; }

    public DateTime? StandardUpdatedAt { get; set; }
}
