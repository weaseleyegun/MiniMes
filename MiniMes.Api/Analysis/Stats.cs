namespace MiniMes.Api.Analysis;

// 통계 계산 모음. DB에서 값을 꺼낸 뒤 메모리에서 계산할 때 쓴다.
public static class Stats
{
    public static double Mean(IReadOnlyList<double> values) =>
        values.Count == 0 ? 0 : values.Sum() / values.Count;

    // 표본 표준편차 (n-1로 나눈다)
    public static double StdDev(IReadOnlyList<double> values)
    {
        if (values.Count < 2) return 0;
        var mean = Mean(values);
        var sumSq = values.Sum(v => (v - mean) * (v - mean));
        return Math.Sqrt(sumSq / (values.Count - 1));
    }

    // 정렬된 목록에서 백분위수를 선형 보간으로 구한다 (p: 0~1)
    public static double Percentile(IReadOnlyList<double> sorted, double p)
    {
        if (sorted.Count == 0) return 0;
        var pos = (sorted.Count - 1) * p;
        var lo = (int)Math.Floor(pos);
        var hi = (int)Math.Ceiling(pos);
        return sorted[lo] + (sorted[hi] - sorted[lo]) * (pos - lo);
    }

    public static double Median(IReadOnlyList<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        return Percentile(sorted, 0.5);
    }
}
