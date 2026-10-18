using System.Net.Http.Json;

namespace MiniMes.Collector;

public class Worker(
    ILogger<Worker> logger,
    IHttpClientFactory httpClientFactory,
    IConfiguration config) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var codes = config.GetSection("Collector:Equipments").Get<string[]>() ?? ["CNC-01"];
        var interval = TimeSpan.FromSeconds(config.GetValue("Collector:IntervalSeconds", 3));

        var simulators = codes.Select(c => new EquipmentSimulator(c)).ToList();
        var client = httpClientFactory.CreateClient("mes-api");

        logger.LogInformation("수집 시작: 설비 {Count}대, 주기 {Sec}초", simulators.Count, interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var sim in simulators)
            {
                var sample = sim.Next();
                try
                {
                    var res = await client.PostAsJsonAsync("api/equipment/logs", sample, stoppingToken);
                    res.EnsureSuccessStatusCode();
                    logger.LogInformation("{Code} {Status} RPM={Rpm} Feed={Feed} Load={Load}%",
                        sample.EquipmentCode, sample.Status, sample.SpindleRpm, sample.FeedRate, sample.SpindleLoadPct);
                }
                catch (HttpRequestException ex)
                {
                    // API가 꺼져 있어도 수집기는 죽지 않고 다음 주기에 다시 시도한다.
                    logger.LogWarning("전송 실패({Code}): {Message}", sample.EquipmentCode, ex.Message);
                }
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
