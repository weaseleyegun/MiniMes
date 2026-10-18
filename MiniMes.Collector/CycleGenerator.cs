namespace MiniMes.Collector;

// API의 품목 조회 결과 (필요한 필드만)
public record ItemInfo(string ItemCode, string Material, double Diameter, double CutLength, int FluteCount);

// API로 보낼 사이클 1건 (API의 CreateCycleRequest와 필드 이름이 같다)
public record CycleSample(
    string ItemCode,
    string EquipmentCode,
    string? LotNo,
    DateTime StartedAt,
    double CycleTimeSec,
    bool IsGood,
    double SpindleLoadAvg,
    double SpindleLoadMax,
    double SpindleRpmAvg,
    double FeedRateAvg,
    double VibrationMax);

// 가상 가공 결과 생성기.
// 정답을 심어 둔다:
//  - 이론 CT = (40 + 4*직경 + 0.6*절삭길이 + 5*날수) * 재질계수 (+ 노이즈, 가끔 이상치)
//  - 불량의 약 75%는 SpindleLoadMax와 VibrationMax가 비정상적으로 높다.
public static class CycleGenerator
{
    private static readonly Dictionary<string, double> MaterialFactor = new()
    {
        ["Steel"] = 1.0,
        ["Stainless"] = 1.25,
        ["Aluminum"] = 0.8,
        ["Carbide"] = 1.4
    };

    private static double Factor(string material) => MaterialFactor.GetValueOrDefault(material, 1.0);

    // 노이즈 없는 이론 CT (예측 모델 검증 때 정답으로 쓴다)
    public static double ExpectedCycleTime(ItemInfo item) =>
        (40 + 4.0 * item.Diameter + 0.6 * item.CutLength + 5.0 * item.FluteCount) * Factor(item.Material);

    public static CycleSample Generate(ItemInfo item, string equipmentCode, DateTime startedAt, string lotNo)
    {
        var mf = Factor(item.Material);

        // ---- Cycle Time: 노이즈 3%, 2%는 셋업 지연 같은 이상치 ----
        var ct = ExpectedCycleTime(item) * (1 + Gauss(0, 0.03));
        if (Random.Shared.NextDouble() < 0.02)
            ct *= 1 + (0.3 + Random.Shared.NextDouble() * 0.5);

        // ---- 불량 여부: 기본 4%, 난삭재(Stainless/Carbide)는 7% ----
        var defectRate = item.Material is "Stainless" or "Carbide" ? 0.07 : 0.04;
        var isGood = Random.Shared.NextDouble() >= defectRate;

        // ---- ACT 값 ----
        var loadAvg = 40 * mf + 0.5 * item.Diameter + Gauss(0, 2);
        var loadMax = loadAvg * 1.25 + Gauss(0, 2);
        var rpm = 9000 - 150 * item.Diameter + Gauss(0, 80);
        var feed = 250 + Gauss(0, 8);
        var vib = 0.5 + 0.02 * item.Diameter + Gauss(0, 0.04);

        // 불량의 75%는 부하 최대값과 진동이 튄다 (나머지 25%는 정상처럼 보이는 불량)
        if (!isGood && Random.Shared.NextDouble() < 0.75)
        {
            loadMax *= 1.30;
            vib *= 1.6;
        }

        return new CycleSample(
            item.ItemCode, equipmentCode, lotNo, startedAt,
            Math.Round(ct, 1), isGood,
            Math.Round(loadAvg, 1), Math.Round(loadMax, 1),
            Math.Round(rpm, 0), Math.Round(feed, 1), Math.Round(vib, 3));
    }

    // 평균 mean, 표준편차 sd인 정규분포 난수 (Box-Muller 변환)
    private static double Gauss(double mean, double sd)
    {
        var u1 = 1.0 - Random.Shared.NextDouble();
        var u2 = Random.Shared.NextDouble();
        return mean + sd * Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
