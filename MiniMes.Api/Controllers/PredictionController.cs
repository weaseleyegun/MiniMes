using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMes.Api.Data;
using MiniMes.Api.Models;

namespace MiniMes.Api.Controllers;

// 신규 품목 Cycle Time 예측. 계산은 Python 예측 서버가 하고, 이 컨트롤러는 중계한다.
[ApiController]
[Route("api/prediction")]
public class PredictionController : ControllerBase
{
    private readonly MiniMesDbContext _db;
    private readonly IHttpClientFactory _httpFactory;

    public PredictionController(MiniMesDbContext db, IHttpClientFactory httpFactory)
    {
        _db = db;
        _httpFactory = httpFactory;
    }

    // 모델 정보 (학습 품목 수, 교차검증 오차, 품목별 예측/실제 비교)
    [HttpGet("model")]
    public Task<IActionResult> GetModel() => Forward(HttpMethod.Get, "model");

    // 표준 CT가 계산된 품목으로 모델을 다시 학습
    [HttpPost("train")]
    public Task<IActionResult> Train() => Forward(HttpMethod.Post, "train");

    // 품목 사양을 직접 입력해 CT 예측
    [HttpPost("ct")]
    public Task<IActionResult> Predict(PredictRequest req) => Forward(HttpMethod.Post, "predict", req);

    // 표준 CT가 아직 없는 등록 품목(신규 품목)을 한꺼번에 예측
    [HttpGet("new-items")]
    public async Task<ActionResult<IEnumerable<NewItemPrediction>>> NewItems()
    {
        var items = await _db.Items
            .Where(i => i.StandardCycleTimeSec == null)
            .OrderBy(i => i.ItemCode)
            .ToListAsync();

        var client = _httpFactory.CreateClient("predictor");
        var result = new List<NewItemPrediction>();
        try
        {
            foreach (var item in items)
            {
                var res = await client.PostAsJsonAsync("predict",
                    new PredictRequest(item.Material, item.Diameter, item.CutLength, item.FluteCount));

                if (!res.IsSuccessStatusCode)
                    return await ErrorFrom(res);

                var p = await res.Content.ReadFromJsonAsync<PredictorResult>();
                if (p is null) continue;

                result.Add(new NewItemPrediction(
                    item.ItemCode, item.Name, item.Material, item.Diameter, item.CutLength, item.FluteCount,
                    p.PredictedCtSec, p.ExpectedErrorPct, p.Warning));
            }
        }
        catch (HttpRequestException)
        {
            return Unavailable();
        }
        return Ok(result);
    }

    // 예측 서버로 요청을 그대로 넘기고 응답(상태 코드 포함)을 돌려준다.
    private async Task<IActionResult> Forward(HttpMethod method, string path, object? body = null)
    {
        var client = _httpFactory.CreateClient("predictor");
        try
        {
            using var request = new HttpRequestMessage(method, path);
            if (body is not null) request.Content = JsonContent.Create(body);

            var response = await client.SendAsync(request);
            return await ErrorFrom(response);
        }
        catch (HttpRequestException)
        {
            return Unavailable();
        }
    }

    // 예측 서버의 응답 본문과 상태 코드를 그대로 돌려주는 결과 객체
    private static async Task<ContentResult> ErrorFrom(HttpResponseMessage res) => new()
    {
        Content = await res.Content.ReadAsStringAsync(),
        ContentType = "application/json",
        StatusCode = (int)res.StatusCode
    };

    private ObjectResult Unavailable() =>
        StatusCode(503, new { detail = "예측 서버에 연결할 수 없습니다. MiniMes.Predictor를 실행하세요." });
}
