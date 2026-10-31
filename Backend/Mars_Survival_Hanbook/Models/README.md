# Models của Mars Survival Handbook

Bộ model bám theo [đặc tả dự án](../../../Docs/Mars_Survival_Handbook.pdf), đặc biệt các mục 7–10. Sử dụng .NET 8, EF Core 8 và SQL Server theo các dependency đã có trong project. Namespace giữ nguyên tên hiện tại của backend: `Mars_Survival_Hanbook`.

## Danh mục

Có 41 entity, 21 enum và lớp cơ sở `Entity`.

| Thư mục | Entity |
| --- | --- |
| Content | Module, Category, Article, ArticleSection, Tag, ArticleTag, ArticleRelation, GlossaryTerm, GlossaryAlias, ArticleGlossaryTerm |
| Science | ScientificSource, ArticleSource, ScientificFact, ArticleFact, Measurement, Unit, ResearchPaper |
| Technology | Technology, Resource, Hazard, ArticleTechnology, TechnologySource, TechnologyFact, TechnologyResource, ArticleResource, ArticleHazard, TechnologyHazard |
| Exploration | Mission, MarsLocation, ExplorationEvent |
| Media | MediaAsset, ArticleMedia, MapLayer |
| Identity | User, Bookmark |
| Editorial | ArticleVersion, ReviewTask, AuditLog, FeedbackReport |
| Ingestion | NasaConnector, IngestionJob |

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
- `User.PasswordHash` được loại khỏi JSON. Service cần hash mật khẩu bằng password hasher và chuẩn hóa `NormalizedEmail` trước khi lưu. API nên dùng DTO để tránh trả navigation graph hoặc nhận trực tiếp các trường quyền/trạng thái từ client.

## EF Core

`Data/MarsDbContext.cs` khai báo đủ DbSet. Các file trong `Data/Configurations` cấu hình quan hệ, precision, chỉ mục, enum, concurrency token và CHECK constraint.

Tất cả quan hệ dùng `DeleteBehavior.NoAction` để tránh cascade-delete bài, nguồn, lịch sử và citation. Dùng archive cho nội dung đã công khai; việc xóa vật lý phải xử lý rõ các bản ghi liên quan.

Không dùng global query filter: CMS cần thấy draft/archive và các citation cũ vẫn cần được truy nguyên. Khi xây truy vấn công khai, bắt buộc lọc `Status == ArticleStatus.Published` và `ArchivedAt == null` cho bài.

`Program.cs` đã đăng ký context bằng connection string cấu hình của môi trường:

```csharp
builder.Services.AddDbContext<MarsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MarsDatabase")));
```

Connection string mặc định dùng LocalDB và database `MarsSurvivalHandbook`; migration đầu tiên nằm trong `Data/Migrations`. Xem [hướng dẫn chạy Update-Database](../Data/README.md). Full-text catalog/index sẽ được bổ sung bằng migration sau khi xác định ngôn ngữ và cách tìm kiếm.

## Phần phải thực hiện tại service

Model và constraint bảo vệ tính toàn vẹn dữ liệu, nhưng chưa triển khai workflow nghiệp vụ. Tầng service cần:

1. Kiểm tra quyền của Editor/Reviewer/Admin và các bước chuyển trạng thái; chỉ xuất bản phiên bản hiện tại đã được duyệt.
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
