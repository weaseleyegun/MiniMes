using MiniMes.Collector;

var builder = Host.CreateApplicationBuilder(args);

// MES API 호출용 HttpClient (이름: mes-api). 주소는 appsettings.json에서 읽는다.
builder.Services.AddHttpClient("mes-api", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["MesApi:BaseUrl"]!);
});

builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<CycleWorker>();

var host = builder.Build();
host.Run();
