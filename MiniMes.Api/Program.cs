using Microsoft.EntityFrameworkCore;
using MiniMes.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<MiniMesDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Python 예측 서버(FastAPI) 호출용 HttpClient
builder.Services.AddHttpClient("predictor", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Predictor:BaseUrl"] ?? "http://localhost:8000");
    client.Timeout = TimeSpan.FromSeconds(15);
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // 개발 환경: 시작할 때 마이그레이션을 적용하고 기본 품목을 채운다.
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<MiniMesDbContext>();
        db.Database.Migrate();
        DbSeeder.SeedItems(db);
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 작업자 화면: wwwroot/index.html을 루트(/)에서 바로 제공한다.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
