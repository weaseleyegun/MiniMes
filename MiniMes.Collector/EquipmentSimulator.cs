namespace MiniMes.Collector;

// API로 보낼 수집 데이터 1건
public record EquipmentSample(
    string EquipmentCode,
    string Status,
    double SpindleRpm,
    double FeedRate,
    double SpindleLoadPct);

// 실제 PLC/CNC 대신 값을 만들어 내는 가상 설비
public class EquipmentSimulator
{
    private readonly string _code;
    private string _status = "Run";

    public EquipmentSimulator(string code)
    {
        _code = code;
    }

    public EquipmentSample Next()
    {
        _status = NextStatus(_status);

        if (_status != "Run")
            return new EquipmentSample(_code, _status, 0, 0, 0);

        return new EquipmentSample(
            _code,
            _status,
            SpindleRpm: Math.Round(8000 + Random.Shared.NextDouble() * 300 - 150, 1),
            FeedRate: Math.Round(300 + Random.Shared.NextDouble() * 20 - 10, 1),
            SpindleLoadPct: Math.Round(55 + Random.Shared.NextDouble() * 20 - 10, 1));
    }

    // 상태 전이: 가동 중에는 대부분 유지하고, 가끔 대기/알람으로 바뀐다.
    private static string NextStatus(string current)
    {
        var roll = Random.Shared.NextDouble();
        return current switch
        {
            "Run" => roll < 0.90 ? "Run" : roll < 0.97 ? "Idle" : "Alarm",
            "Idle" => roll < 0.50 ? "Run" : "Idle",
            _ => roll < 0.30 ? "Idle" : "Alarm"   // Alarm
        };
    }
}
