# ASP.NET Core ↔ Spring Boot 대응표

이 프로젝트를 만들면서 정리한 대응 관계입니다. 개념은 거의 같고 이름과 문법만 다릅니다.

| 하는 일 | ASP.NET Core / C# (이 프로젝트) | Spring Boot / Java |
|---|---|---|
| 언어 / 빌드 | C#, `dotnet` CLI, `.csproj` | Java, Maven/Gradle, `pom.xml` |
| 컨트롤러 | `[ApiController]`, `ControllerBase` | `@RestController` |
| 경로 매핑 | `[Route("api/[controller]")]`, `[HttpGet("{id}")]` | `@RequestMapping`, `@GetMapping("/{id}")` |
| 요청 본문 바인딩 | 파라미터로 받으면 자동 (`[ApiController]`) | `@RequestBody` |
| 검증 | `[Required]`, `[Range]`, `[StringLength]` | `@NotNull`, `@Min/@Max`, `@Size` |
| 응답 | `Ok()`, `NotFound()`, `NoContent()`, `ActionResult<T>` | `ResponseEntity.ok()`, `.notFound()` |
| DTO | `record` (C# 9) | `record` (Java 16) |
| 의존성 주입 | 생성자 주입 (`Program.cs`에 등록) | 생성자 주입 (`@Autowired`/자동 구성) |
| 설정 | `appsettings.json`, `IConfiguration` | `application.yml`, `@Value` |
| ORM | EF Core (`DbContext`, `DbSet<T>`) | JPA (`EntityManager`, `Repository`) |
| 변경 추적 | `SaveChangesAsync()` 시 변경분 UPDATE | 영속성 컨텍스트 더티 체킹 |
| 연관 로딩 | `.Include(x => x.Results)` | `JOIN FETCH` |
| 마이그레이션 | `dotnet ef migrations add` | Flyway / Liquibase |
| 비동기 | `async` / `await`, `Task<T>` | `CompletableFuture` |
| 백그라운드 작업 | `BackgroundService` (Worker Service) | `@Scheduled`, 별도 스레드 |
| 외부 HTTP 호출 | `IHttpClientFactory` | `RestTemplate` / `WebClient` |
| JSON | `System.Text.Json` | Jackson |
| API 문서 | Swashbuckle (Swagger) | springdoc-openapi |
| 정적 파일 | `UseStaticFiles()`, `wwwroot/` | `static/` 리소스 |

## 코드로 보는 차이

**컨트롤러**

```csharp
// C#
[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly MiniMesDbContext _db;
    public ItemsController(MiniMesDbContext db) => _db = db;

    [HttpGet("{itemCode}")]
    public async Task<ActionResult<Item>> GetByCode(string itemCode)
    {
        var item = await _db.Items.FirstOrDefaultAsync(i => i.ItemCode == itemCode);
        return item is null ? NotFound() : Ok(item);
    }
}
```

```java
// Java (Spring)
@RestController
@RequestMapping("/api/items")
public class ItemsController {
    private final ItemRepository repo;
    public ItemsController(ItemRepository repo) { this.repo = repo; }

    @GetMapping("/{itemCode}")
    public ResponseEntity<Item> getByCode(@PathVariable String itemCode) {
        return repo.findByItemCode(itemCode)
                   .map(ResponseEntity::ok)
                   .orElse(ResponseEntity.notFound().build());
    }
}
```

**DTO**

```csharp
public record CreateLotRequest(
    [Required, StringLength(30)] string LotNo,
    [Range(1, 100000)] int Quantity);
```

```java
public record CreateLotRequest(
    @NotBlank @Size(max = 30) String lotNo,
    @Min(1) @Max(100000) int quantity) {}
```

## 헷갈리기 쉬운 점

- C# record의 검증 어노테이션은 `[property: ...]` 없이 **생성자 파라미터에 직접** 붙입니다. `property:`를 붙이면 ASP.NET Core 8이 예외를 냅니다.
- EF Core는 기본이 **지연 로딩 꺼짐**입니다. `.Include()`를 쓰지 않으면 연관 컬렉션이 빈 목록입니다.
- 컨트롤러는 요청마다 새로 만들어지므로 요청 사이에 값을 유지하려면 DB나 `static`/싱글톤을 써야 합니다.
- `dotnet run`은 시작할 때 한 번만 빌드합니다. 코드를 바꾸면 재시작해야 하고, `dotnet watch run`은 저장 시 자동 재시작합니다.
