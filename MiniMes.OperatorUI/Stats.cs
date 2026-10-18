namespace MiniMes.OperatorUI;

// 화면에서 쓰는 간단한 통계 함수
public static class Stats
{
    public static double Mean(IReadOnlyList<double> v) => v.Count == 0 ? 0 : v.Sum() / v.Count;

    // 표본 표준편차 (n-1)
    public static double StdDev(IReadOnlyList<double> v)
    {
        if (v.Count < 2) return 0;
        var m = Mean(v);
        return Math.Sqrt(v.Sum(x => (x - m) * (x - m)) / (v.Count - 1));
    }

    // 백분위수 (선형 보간, p: 0~1). 정렬되지 않은 목록도 받는다.
    public static double Percentile(IEnumerable<double> values, double p)
    {
        var s = values.OrderBy(x => x).ToList();
        if (s.Count == 0) return 0;
        var pos = (s.Count - 1) * p;
        var lo = (int)Math.Floor(pos);
        var hi = (int)Math.Ceiling(pos);
        return s[lo] + (s[hi] - s[lo]) * (pos - lo);
    }

    public static double Median(IEnumerable<double> values) => Percentile(values, 0.5);
}

// ACT 변수 목록: 화면에서 쓰는 이름/단위와 값을 꺼내는 방법
public record ActVariable(string Key, string Label, string Unit, Func<CycleRow, double> Select)
{
    public string Display => string.IsNullOrEmpty(Unit) ? Label : $"{Label} ({Unit})";

    public static readonly ActVariable[] All =
    {
        new("SpindleLoadAvg", "주축부하 평균", "%", c => c.SpindleLoadAvg),
        new("SpindleLoadMax", "주축부하 최대", "%", c => c.SpindleLoadMax),
        new("SpindleRpmAvg", "주축 RPM", "rpm", c => c.SpindleRpmAvg),
        new("FeedRateAvg", "Feed", "mm/min", c => c.FeedRateAvg),
        new("VibrationMax", "진동 최대", "mm/s", c => c.VibrationMax),
    };

    public override string ToString() => Display;
}
