using System.Net.Http.Json;
using System.Text.Json;

namespace MiniMes.OperatorUI;

// MES API 호출을 한곳에 모은 클래스. 화면은 이 클래스만 통해 서버와 통신한다.
public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(string baseUrl)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public string BaseUrl => _http.BaseAddress!.ToString();

    // ---------- 조회 ----------

    // GetFromJsonAsync는 camelCase/대소문자 무시 설정이 기본이라 API 응답을 바로 읽을 수 있다.
    public async Task<List<EquipmentBoardRow>> GetEquipmentBoardAsync() =>
        await GetAsync<List<EquipmentBoardRow>>("api/dashboard/equipment");

    public async Task<List<ItemRow>> GetItemsAsync() =>
        await GetAsync<List<ItemRow>>("api/items");

    // 사이클 이력을 시간 오름차순으로 돌려준다 (API는 최신순이라 뒤집는다).
    public async Task<List<CycleRow>> GetCyclesAsync(
        string? itemCode = null, string? equipmentCode = null, DateTime? from = null, DateTime? to = null,
        int take = 5000)
    {
        var query = new List<string> { $"take={take}" };
        if (!string.IsNullOrEmpty(itemCode)) query.Add("itemCode=" + Uri.EscapeDataString(itemCode));
        if (!string.IsNullOrEmpty(equipmentCode)) query.Add("equipmentCode=" + Uri.EscapeDataString(equipmentCode));
        if (from is { } f) query.Add("from=" + Uri.EscapeDataString(f.ToString("s")));
        if (to is { } t) query.Add("to=" + Uri.EscapeDataString(t.ToString("s")));

        var rows = await GetAsync<List<CycleRow>>("api/cycles?" + string.Join("&", query));
        rows.Reverse();
        return rows;
    }

    // 최근 설비 상태 로그를 시간 오름차순으로 돌려준다.
    public async Task<List<EquipmentLogRow>> GetEquipmentLogsAsync(string equipmentCode, int take = 60)
    {
        var rows = await GetAsync<List<EquipmentLogRow>>(
            $"api/equipment/logs?equipmentCode={Uri.EscapeDataString(equipmentCode)}&take={take}");
        rows.Reverse();
        return rows;
    }

    public async Task<ActAnalysisResponse> GetActAnalysisAsync(string? itemCode)
    {
        var url = "api/analysis/act";
        if (!string.IsNullOrEmpty(itemCode))
            url += "?itemCode=" + Uri.EscapeDataString(itemCode);

        return await GetAsync<ActAnalysisResponse>(url);
    }

    // ---------- 표준화 / 예측 ----------

    public async Task<RecalculateResponse> RecalculateStandardsAsync() =>
        await ReadAsync<RecalculateResponse>(await _http.PostAsync("api/standards/recalculate", null));

    public async Task<ModelInfo> GetModelInfoAsync() =>
        await GetAsync<ModelInfo>("api/prediction/model");

    public async Task<ModelInfo> TrainModelAsync() =>
        await ReadAsync<ModelInfo>(await _http.PostAsync("api/prediction/train", null));

    public async Task<PredictionResult> PredictAsync(PredictRequest request) =>
        await ReadAsync<PredictionResult>(await _http.PostAsJsonAsync("api/prediction/ct", request));

    // ---------- 공통 ----------

    private async Task<T> GetAsync<T>(string url) where T : class =>
        await ReadAsync<T>(await _http.GetAsync(url));

    // 실패 응답이면 서버가 준 사유({ "detail": "..." })를 예외 메시지로 올린다.
    private static async Task<T> ReadAsync<T>(HttpResponseMessage response) where T : class
    {
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await DescribeErrorAsync(response));

        return await response.Content.ReadFromJsonAsync<T>()
               ?? throw new InvalidOperationException("빈 응답입니다.");
    }

    private static async Task<string> DescribeErrorAsync(HttpResponseMessage response)
    {
        try
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                return detail.GetString()!;
        }
        catch (JsonException)
        {
            // JSON이 아닌 응답이면 상태 코드만 보여준다.
        }
        return $"HTTP {(int)response.StatusCode}";
    }
}
