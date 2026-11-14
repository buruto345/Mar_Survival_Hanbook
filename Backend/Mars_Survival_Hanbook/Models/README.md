# Models của Mars Survival Handbook

Bộ model bám theo [đặc tả dự án](../../../Docs/Reference/Mars_Survival_Handbook.pdf), đặc biệt các mục 7–10. Sử dụng .NET 8, EF Core 8 và SQL Server theo các dependency đã có trong project. Namespace giữ nguyên tên hiện tại của backend: `Mars_Survival_Hanbook`.

## Danh mục

Có 50 entity, 24 enum và lớp cơ sở `Entity`, bao gồm các model Discover Mars bổ sung ngày 03/11/2026.

| Thư mục | Entity |
| --- | --- |
| Content | Module, Category, Article, ArticleSection, Tag, ArticleTag, ArticleRelation, GlossaryTerm, GlossaryAlias, ArticleGlossaryTerm, ArticleMission, ArticleLocation, ArticleExplorationEvent, ArticleMapLayer, ArticleMeasurement |
| Science | ScientificSource, SourceRevision, ArticleSource, ScientificFact, ArticleFact, Measurement, Unit, ResearchPaper |
| Technology | Technology, Resource, Hazard, ArticleTechnology, TechnologySource, TechnologyFact, TechnologyResource, ArticleResource, ArticleHazard, TechnologyHazard |
| Exploration | Mission, MarsLocation, ExplorationEvent |
| Media | MediaAsset, ArticleMedia, MapLayer |
| Identity | User, Bookmark |
| Editorial | ArticleVersion, ArticleVersionSource, PublicationResource, ReviewTask, AuditLog, FeedbackReport |
| Ingestion | NasaConnector, IngestionJob, IngestionItem |

Các bảng liên kết bổ sung giúp sử dụng lại fact, nguồn, công nghệ, tài nguyên và glossary giữa nhiều bài. `ExplorationEvent` phục vụ timeline; `MapLayer` phục vụ bản đồ ở giai đoạn sau. Module thứ 6, nếu được chấp thuận, là một bản ghi `Module`, không cần thêm class.

## Quy ước dữ liệu

- Entity độc lập dùng `Guid`, `CreatedAt`, `UpdatedAt`, `ArchivedAt` và `RowVersion`. Thời gian tạo/cập nhật được duy trì trong `SaveChanges` và `SaveChangesAsync`.
- Bảng liên kết dùng khóa ghép. `TechnologyResource` đưa `Direction` vào khóa để một tài nguyên có thể vừa là đầu vào vừa là đầu ra; `ArticleRelation` đưa loại quan hệ vào khóa.
- Enum lưu thành tên chuỗi có giới hạn độ dài và CHECK constraint. Các giá trị enum có mã số tường minh để tránh đổi nghĩa khi thêm trạng thái.
- Slug bài viết là duy nhất toàn hệ thống; slug category duy nhất trong module. `TranslationGroupId` và `LanguageCode` chuẩn bị cho các bản dịch mà không quyết định phạm vi song ngữ của MVP.
- Category con phải thuộc cùng module với category cha. Service vẫn cần phát hiện chu trình nhiều cấp khi thay đổi cây category.
- Mỗi `ReviewTask` tham chiếu một `ArticleVersion` thuộc đúng bài qua khóa ngoại ghép. Snapshot phiên bản phải là JSON hợp lệ.
- `ScientificFact` và `Measurement` dùng `decimal(28,12)`, lưu cả giá trị/đơn vị gốc, nguồn, điều kiện và loại bằng chứng. `Unit.QuantityKind` phân biệt đại lượng vật lý khi so sánh.
- `EvidenceType.Unspecified` và `TestEnvironment.Unspecified` phục vụ dữ liệu chưa kiểm chứng; không mặc định dữ liệu mới là đã quan sát trên Sao Hỏa. Fact được đánh dấu Accepted phải có người/ngày xác minh và evidence/environment cụ thể.
- `TechnologyFact` gắn thông số với tiêu chí so sánh và bối cảnh. Service phải kiểm tra sự tương thích đơn vị, điều kiện, loại số liệu và môi trường trước khi so sánh.
- Source có URL, DOI, tác giả, tổ chức NASA, quyền sở hữu nhiệm vụ, ngày truy xuất và trạng thái xác minh. `ResearchPaper` dùng metadata thư mục từ Source, bổ sung mã tài liệu, abstract và thông tin nghiên cứu.
- Ảnh/media có nguồn, credit, usage note và alt text. `NasaConnector.AuthenticationSecretReference` chỉ lưu tên/tham chiếu secret; không lưu API key thật trong model/configuration JSON.
- User dùng `Username`, `FullName`, `Email`, `PasswordHash`, role `Admin`/`Viewer`. `NormalizedUsername` và `NormalizedEmail` có unique index. Hai field chuẩn hóa và password hash được loại khỏi JSON. Đăng ký dùng DTO bốn field, luôn cấp Viewer; đăng nhập/profile trả cùng cấu trúc tài khoản.

## EF Core

`Data/MarsDbContext.cs` khai báo đủ DbSet. Các file trong `Data/Configurations` cấu hình quan hệ, precision, chỉ mục, enum, concurrency token và CHECK constraint.

Tất cả quan hệ dùng `DeleteBehavior.NoAction` để tránh cascade-delete bài, nguồn, lịch sử và citation. Dùng archive cho nội dung đã công khai; việc xóa vật lý phải xử lý rõ các bản ghi liên quan.

Không dùng global query filter: CMS cần thấy draft/archive và các citation cũ vẫn cần được truy nguyên. Discover Mars dùng `DiscoveryService.Publications`: ngoài trạng thái Published và archive, phải join đúng PublishedVersionId, kiểm tra schema và loại nguồn bị thu hồi. Không truy vấn working resource để trả nội dung công khai.

`Program.cs` đã đăng ký context bằng connection string cấu hình của môi trường:

```csharp
builder.Services.AddDbContext<MarsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MarsDatabase")));
```

Connection string mặc định dùng LocalDB và database `MarsSurvivalHandbook`; migration đầu tiên nằm trong `Data/Migrations`. Xem [hướng dẫn chạy Update-Database](../Data/README.md). Full-text catalog/index sẽ được bổ sung bằng migration sau khi xác định ngôn ngữ và cách tìm kiếm.

## Workflow tại service

Discover Mars đã triển khai các yêu cầu dưới đây trong `Services`, `Integrations/Nasa`, `BackgroundJobs` và controllers. Các module khác chưa tự động có workflow này. Xem [runbook và bằng chứng kiểm thử](https://github.com/buruto345/Mar_Survival_Hanbook/blob/7322c0be43e68681c7ba3d8f8a4fa755b126d435/Docs/Discover_Mars_Implementation_Status.md).

1. Kiểm tra quyền Admin/Viewer và các bước chuyển trạng thái; Admin được tự duyệt nhưng chỉ xuất bản phiên bản hiện tại đã được duyệt.
2. Kiểm tra NASA ownership, claim, nguồn, credit và quyền media trước khi publish; không coi domain NASA hoặc một cờ Accepted là bằng chứng tự động.
3. Vô hiệu hóa yêu cầu duyệt cũ khi nội dung/fact liên quan thay đổi, ghi snapshot và audit trong cùng transaction. Chặn sửa/xóa `ArticleVersion` và `AuditLog` qua API thông thường.
4. Đảm bảo bài published có đủ nguồn được chấp nhận, nội dung, ngày review và các trường bắt buộc theo loại bài; không tự publish kết quả import.
5. Kiểm tra URL, slug, JSON theo schema, chuỗi không rỗng, chuẩn hóa email/đơn vị và quy tắc chuyển đổi. `required` trong C# không thay thế request validation.
6. Quản lý lịch review, retry/rate limit và xử lý xung đột `DbUpdateConcurrencyException`.

## Kiểm thử

Chạy từ thư mục repository:

```powershell
dotnet test Backend/Mars_Survival_Hanbook.Tests/Mars_Survival_Hanbook.Tests.csproj
```

Mặc định chạy kiểm tra EF metadata và JSON; test SQL Server được bỏ qua nếu chưa bật. Để kiểm tra ràng buộc bằng SQL Server LocalDB trên Windows:

```powershell
$env:MARS_RUN_SQLSERVER_TESTS = '1'
dotnet test Backend/Mars_Survival_Hanbook.Tests/Mars_Survival_Hanbook.Tests.csproj
```

Test tạo database có tên ngẫu nhiên bắt đầu bằng `MarsHandbookModelTests_` trên `(localdb)\MSSQLLocalDB`, áp dụng migrations, kiểm tra ghi/đọc và các trường hợp bị từ chối, rồi xóa chính database tạm đó. Không sử dụng connection string của ứng dụng.
