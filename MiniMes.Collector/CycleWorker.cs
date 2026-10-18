using System.Net.Http.Json;

namespace MiniMes.Collector;

// 가공 사이클 결과를 API로 보내는 수집기.
// 시작할 때 이력이 비어 있으면 과거 데이터를 한꺼번에 채우고(backfill), 이후 주기적으로 1건씩 보낸다.
public class CycleWorker(
    ILogger<CycleWorker> logger,
    IHttpClientFactory httpClientFactory,
    IConfiguration config) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var client = httpClientFactory.CreateClient("mes-api");
        var equipments = config.GetSection("Collector:Equipments").Get<string[]>() ?? ["CNC-01"];
        var exclude = config.GetSection("Cycle:ExcludeItemCodes").Get<string[]>() ?? [];
        var intervalMs = config.GetValue("Cycle:IntervalMs", 1000);
        var backfillCount = config.GetValue("Cycle:BackfillCount", 1500);

        // 1) 품목 목록 가져오기 (API가 켜질 때까지 재시도)
        List<ItemInfo> items;
        while (true)
        {
            try
            {
                var all = await client.GetFromJsonAsync<List<ItemInfo>>("api/items", stoppingToken) ?? [];
                items = all.Where(i => !exclude.Contains(i.ItemCode)).ToList();
                break;
            }
            catch (HttpRequestException)
            {
                logger.LogWarning("품목 조회 실패. 3초 후 재시도");
                await Task.Delay(3000, stoppingToken);
            }
        }
        logger.LogInformation("생산 대상 품목 {Count}개 (신규 품목 {Ex}개 제외)", items.Count, exclude.Length);

        // 2) 이력이 비어 있으면 과거 데이터 채우기
        try
        {
            var existing = await client.GetFromJsonAsync<int>("api/cycles/count", stoppingToken);
            if (existing == 0 && backfillCount > 0)
                await BackfillAsync(client, items, equipments, backfillCount, stoppingToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning("이력 확인 실패: {Message}", ex.Message);
        }

        // 3) 실시간 전송
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var eq in equipments)
            {
                var item = items[Random.Shared.Next(items.Count)];
                var sample = CycleGenerator.Generate(item, eq, DateTime.Now, $"LOT-{DateTime.Now:yyyyMMdd}-{eq}");
                try
                {
                    var res = await client.PostAsJsonAsync("api/cycles", sample, stoppingToken);
                    res.EnsureSuccessStatusCode();
                    logger.LogInformation("{Eq} {Item} CT={Ct}s {Result}",
                        eq, item.ItemCode, sample.CycleTimeSec, sample.IsGood ? "양품" : "불량");
                }
                catch (HttpRequestException ex)
                {
                    logger.LogWarning("사이클 전송 실패({Eq}): {Message}", eq, ex.Message);
                }
            }
            await Task.Delay(intervalMs, stoppingToken);
        }
    }

    private async Task BackfillAsync(HttpClient client, List<ItemInfo> items, string[] equipments,
        int count, CancellationToken ct)
    {
        logger.LogInformation("과거 이력 {Count}건 생성 중...", count);

        // 설비당 약 5분 간격으로 과거로 거슬러 올라간다.
        var batch = new List<CycleSample>();
        for (var i = 0; i < count; i++)
        {
            var eq = equipments[i % equipments.Length];
            var item = items[Random.Shared.Next(items.Count)];
            var startedAt = DateTime.Now.AddMinutes(-5.0 * (count - i) / equipments.Length);
            batch.Add(CycleGenerator.Generate(item, eq, startedAt, $"LOT-{startedAt:yyyyMMdd}-{eq}"));

            if (batch.Count == 100 || i == count - 1)
            {
                var res = await client.PostAsJsonAsync("api/cycles/batch", batch, ct);
                res.EnsureSuccessStatusCode();
                batch.Clear();
            }
        }
        logger.LogInformation("과거 이력 생성 완료");
    }
}
