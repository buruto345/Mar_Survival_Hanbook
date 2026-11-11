# Discover Mars — triển khai và vận hành

Cập nhật 04/11/2026. Backend đã hiện thực API trong [hướng dẫn](Discover_Mars_API_Implementation_Guide.md), giữ .NET 8, EF Core 8.0.33 và SQL Server. Không thay `InitialCreate`. Đã bổ sung [đăng ký và hai role Admin/Viewer](User_Registration_And_Authorization.md).

## Phần đã hiện thực

- JWT login, đăng ký Viewer và profile `/api/auth/me`; kiểm tra lại tài khoản/role trên mỗi request. Admin quản lý toàn bộ CMS, gồm tự duyệt và publish; Viewer chỉ đọc nội dung public và profile.
- Identity nguồn phân biệt hoa/thường; revision bất biến; fingerprint gồm metadata chuẩn hóa và checksum attachment; nguồn Pending không ghi đè bản Accepted. Có amend metadata thành revision mới, decision, revoke và đọc raw được phân quyền.
- Import NTRS, NASA Images, PDS và Mars Trek qua HTTP allowlist. Job/item/checkpoint/lease nằm trong SQL Server; idempotency, pagination, retry có giới hạn và resume sau restart. NASA Web dùng đăng ký nguồn thủ công có bằng chứng, không scrape URL tùy ý.
- CRUD có DTO riêng cho fact, measurement, mission, location, timeline, media và map layer; kiểm tra provenance, đơn vị, ngày, tọa độ, bbox và quality.
- Snapshot đóng băng nội dung, từng claim/evidence/locator, nguồn, tài nguyên, caption/displayOrder; dependency FK ghép và hash. Sửa bản nháp tạo version mới. Task duyệt cũ bị hủy khi có bản thay thế.
- Publish/restore/unpublish và audit trong transaction; publish/revoke dùng khóa chung trong SQL Server. Mọi public list/detail/search/count/resource đọc bản đang công bố; nguồn bị revoke làm bài và file ảnh liên quan trả 404 ngay. Public responses dùng `Cache-Control: no-store`.
- Bản đồ có style, matrix, projection, bbox, template và attribution. Snapshot ảnh có checksum được phục vụ từ file nội bộ đã lưu; request đọc công khai không gọi NASA.

## Chạy backend

Yêu cầu: SDK hỗ trợ build `net8.0`, .NET 8 runtime, SQL Server/LocalDB và EF CLI từ `dotnet-tools.json`. Kiểm tra tại máy hiện tại bằng SDK 10.0.302 và Postman CLI 1.75.0.

Chạy tại gốc repository:

```powershell
dotnet tool restore
dotnet restore Backend/Mars_Survival_Hanbook
dotnet ef database update --project Backend/Mars_Survival_Hanbook
dotnet run --project Backend/Mars_Survival_Hanbook --no-launch-profile -- seed
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project Backend/Mars_Survival_Hanbook --no-launch-profile --urls http://localhost:5086
```

Swagger: `http://localhost:5086/swagger`. Module: `GET /api/discover-mars`. Backend không tự migrate hoặc seed khi khởi động. Development sinh JWT key tạm nếu chưa cấu hình; token sẽ hết hiệu lực sau khi restart. Production bắt buộc cấu hình `Jwt__SigningKey` từ secret store, ít nhất 32 bytes, và HTTPS.

Các cấu hình vận hành:

| Khóa | Công dụng |
| --- | --- |
| `ConnectionStrings__MarsDatabase` | Ghi đè kết nối runtime/design time |
| `Jwt__SigningKey`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpiryMinutes` | Chữ ký và thời hạn token |
| `Storage__RawRoot` | Kho tài liệu và manifest theo checksum; mặc định `App_Data/raw` dưới content root |
| `Storage__ImageRoot` | Kho ảnh theo checksum, mặc định `ImageMarHanbook` dưới content root. Nếu chỉ đặt `Storage__RawRoot`, thư mục ảnh nằm trong raw root tùy chỉnh. |
| `Nasa__WorkerEnabled`, `Nasa__PollSeconds` | Worker durable, mặc định bật và poll 5 giây |
| `Nasa__TimeoutSeconds`, `Nasa__MaxResponseBytes` | Mặc định 20 giây và 20 MB mỗi response |
| `Nasa__Import__MaxItems` | Mặc định tối đa 500 item/job |
| `Nasa__Pds__MaxSamples` | Mặc định staging tối đa 100 sample/product |
| `Nasa__ApprovedPartnerHosts` | Cho phép máy chủ PDS đối tác cụ thể; vẫn kiểm tra DNS/private IP |
| `Nasa__MarsTrek__Layers` | Allowlist layer key → capabilities path phía server |
| `Discovery__MaxMeasurementDays` | Mặc định khoảng query measurement tối đa 31 ngày |

Không ghi token/mật khẩu vào Git. `App_Data`, kết quả test và log simulation được ignore. Backup database cùng kho raw và thư mục ảnh; DB giữ checksum tham chiếu các bytes này.

## Tạo tài khoản và sử dụng CMS

Người dùng đăng ký bằng `POST /api/auth/register` với `username`, `fullName`, `email`, `password`, luôn nhận role Viewer. Tạo Admin bằng lệnh provision; lệnh từ chối ghi đè tài khoản đã tồn tại. Không có mật khẩu mặc định. Nhập mật khẩu cục bộ, không in ra console:

```powershell
$secret = Read-Host 'Mật khẩu ít nhất 12 ký tự' -AsSecureString
$credential = New-Object System.Management.Automation.PSCredential('bootstrap', $secret)
$env:MARS_BOOTSTRAP_PASSWORD = $credential.GetNetworkCredential().Password
try {
    dotnet run --project Backend/Mars_Survival_Hanbook --no-launch-profile -- provision --username localadmin --full-name "Local Admin" --email admin@example.org --role Admin
} finally {
    Remove-Item Env:MARS_BOOTSTRAP_PASSWORD
}
```

Chỉ có role `Admin` và `Viewer`. Thay email mẫu bằng người phụ trách thực tế. Đăng nhập bằng `POST /api/auth/login` với `email`, `password`; dùng `accessToken` trong `Authorization: Bearer ...`. Không gửi role trong login/register.

Luồng làm việc:

1. Admin tạo import hoặc `/api/admin/discover-mars/sources/manual`. Lưu `id` của revision và ETag.
2. Admin đọc metadata/raw, xác minh tác giả, tổ chức, quyền dùng và bằng chứng. Nếu thiếu metadata, gọi `/source-revisions/{id}/amend`; kết quả là revision Pending mới. Gửi decision của revision đúng bằng chứng.
3. Admin tạo working content và bài. `sourceRevisionIds` ở claim và resource đều được ghim vào snapshot. Đơn vị đã seed có thể tra bảng `Units`; không tự bịa GUID unit.
4. GET article lấy ETag mới nhất; submit `/articles/{id}/versions/{versionId}/submit` dùng **ETag article**.
5. Admin GET task, claim bằng ETag task, kiểm tra snapshot/claim/units/rights, rồi decision bằng ETag mới sau claim. Có thể duyệt bài do chính mình tạo; checklist phải phản ánh việc đối chiếu thực tế.
6. Admin GET article rồi publish đúng `currentDraftVersionId` đã Approved, với ETag article. Bản nháp mới cần review mới.
7. Restore tạo draft mới từ snapshot lịch sử. Unpublish và revoke nguồn cần Admin và reason; không dùng archive working content để thu hồi publication.

GET resource/version trả ETag phù hợp. Các mutation có ID yêu cầu `If-Match`: thiếu trả 428, cũ trả 412. Tải lại resource và xem thay đổi trước khi thử lại. `409` là xung đột state/identity/idempotency; `422` là publication gate. Body lỗi nghiệp vụ có `code`, `traceId`; auth middleware có thể trả 401/403 không có body. Login bị giới hạn theo IP.

## Hợp đồng và request examples

[OpenAPI đã xuất](../postman/specs/discover-mars.openapi.json) là hợp đồng chi tiết của 87 operations/70 paths. [Collection đầy đủ](../postman/collections/Discover%20Mars%20API) được sinh từ Swagger, chứa request mẫu cho tất cả route. Các GUID/body sinh tự động là chỗ để điền dữ liệu thực; không chạy cả collection này như một workflow. Chọn request cần dùng, đặt `baseUrl`, `bearerToken`, ID và ETag tương ứng.

[Smoke collection](../postman/collections/Discover%20Mars%20Smoke) có 8 request chỉ đọc, 20 assertions và response examples ghi nhận từ backend thật. Environment local không chứa secret.

```powershell
postman collection run 'postman/collections/Discover Mars Smoke' --no-report-events
powershell -NoProfile -File scripts/export-discovery-openapi.ps1
```

Script export cần backend Development đang chạy tại `127.0.0.1:5086`, hoặc truyền `-BaseUrl`. Nó thay collection sinh tự động, giữ nguyên collection Smoke và loại ví dụ ngẫu nhiên do generator tạo. Không lưu sửa tay trong collection sinh tự động. Chưa publish collection lên Postman cloud.

Một số quyết định cụ thể hóa các ví dụ đề xuất trong guide:

- Import dùng DTO typed phẳng (`query`, `recordIds`, `pageSize`, `maxItems`, `layerKey`, `includeMembers`, `pdsCollection`, `from`, `to`); không nhận túi `parameters` JSON tùy ý. `recordIds` là citation IDs/NASA IDs/LIDVID tùy connector. Category được chọn khi biên tập bài.
- Response import dùng `id`, bộ đếm và `Location` để poll; không dùng tên `jobId` của JSON minh họa trong guide.
- Images pilot chỉ lấy ảnh; không tải video/audio tùy ý. PDS pilot đọc bảng MEDA calibrated pressure, giữ raw label/data và candidate có lỗi. SCLK/LMST không được suy diễn thành UTC; thiếu UTC hoặc quality thì không tạo measurement được công bố. Muốn mở rộng instrument/quantity phải thêm parser có kiểm thử.
- `mediaPlacements` trong request draft nhận `{ mediaId, displayOrder, caption }`; ID phải nằm trong `resourceRefs.mediaIds`.
- Snapshot schema hiện tại là 1. Snapshot cũ/không tương thích được giữ để kiểm tra; gate từ chối dùng trực tiếp và yêu cầu biên tập lại thành draft mới. Không tự gán approval cho lịch sử thiếu bằng chứng.
- Public search dùng SQL trên trường snapshot đã đóng băng, chưa có full-text index. Sort `relevance` ưu tiên title match rồi ngày publish; không tuyên bố có semantic search.

Ví dụ import thực tế:

```http
POST /api/admin/discover-mars/imports
Authorization: Bearer <editor-token>
Idempotency-Key: ntrs-mars-gram-2024-001
Content-Type: application/json

{"connectorKey":"ntrs","recordIds":["20240012934"],"pageSize":1,"maxItems":1}
```

Cùng key/body trả cùng job. Khác body/cùng key trả 409. Retry không reset item thành công. `RecordsAccepted` là item staging thành công, không phải quyết định khoa học Accepted.

## Migration và dữ liệu cũ

Đã kiểm tra đúng `(localdb)\MSSQLLocalDB` / `MarsSurvivalHandbook`, backup trước migration tại `Backend/Mars_Survival_Hanbook/App_Data/backups/MarsSurvivalHandbook_before_discovery_20261103.bak` (4.378.624 bytes). DB trước upgrade có 0 article, 0 source.

Đã áp dụng tuần tự expand → backfill dry-run/apply → integrity → observation identity; sau đó chạy lại database update và seed hai lần. Kết quả: 4 migration gồm `InitialCreate`, 1 module Discover Mars, 5 category, 5 connector, 7 unit. Không mất/nhân đôi bài hoặc nguồn. Không còn pending migration/model change.

SQL để review nằm trong [Docs/Sql](Sql). Với DB cũ khác có dữ liệu, backup trước và chạy theo thứ tự:

```powershell
dotnet ef database update AddDiscoverMarsVersioning --project Backend/Mars_Survival_Hanbook
dotnet run --project Backend/Mars_Survival_Hanbook --no-launch-profile -- backfill
dotnet run --project Backend/Mars_Survival_Hanbook --no-launch-profile -- backfill --apply
dotnet ef database update --project Backend/Mars_Survival_Hanbook
dotnet run --project Backend/Mars_Survival_Hanbook --no-launch-profile -- seed
```

Backfill báo candidate URL trùng để người quản trị đối chiếu; giữ identity legacy và FK, không tự merge nguồn không đủ căn cứ. Revision legacy là Pending, giữ metadata review gốc trong evidence nhưng không chuyển thành approval mới. Publication legacy thiếu snapshot hợp lệ được chuyển Draft có audit và giữ lịch sử. Không dùng rollback migration để hoàn tác nội dung; cần kế hoạch restore backup nếu migration lỗi.

## Kiểm thử và bằng chứng

```powershell
powershell -NoProfile -File scripts/test-discovery.ps1
```

Script mở NASA simulation ẩn ở port 4010, chạy xUnit + SQL Server + HTTP, build với warning-as-error, kiểm tra EF model, rồi dừng đúng process đã tạo. Nếu đã tự chạy simulation của repository, dùng `-UseExistingSimulation`. Test tạo DB ngẫu nhiên với prefix được kiểm tra và không xóa database ứng dụng.

Kết quả kiểm tra tại máy này: **56 tests pass, 0 fail, 0 skip**; build 0 warning/0 error; **20/20 Postman assertions pass**; OpenAPI syntax, collection và environment lint không có lỗi/cảnh báo. Log xUnit: `Backend/Mars_Survival_Hanbook.Tests/TestResults/discovery.trx`.

| Guide | Bằng chứng trong test |
| --- | --- |
| T01, T14–T17, T26, T30–T31 | `DiscoveryHttpTests`: seed/auth, ownership/task claim, validation, stale ETag, draft/search ẩn |
| T02–T11 | `DiscoveryHttpTests`: concurrent manual source, idempotency; `NasaSimulationTests`: pagination, PDF đổi, A→B→A, partial retry, resume sau commit; `DiscoveryUnitTests`: canonicalization |
| T12–T13 | `PdsParserTests`, NASA simulation: ngày/quality/units thiếu giữ Pending, không tạo số 0 giả |
| T18–T20, T23, T28, T32 | `DiscoveryHttpTests`: freeze dữ liệu/caption, Pending amendment, approval version cũ không tái dùng, snapshot/link bất biến, Pending/Rejected bị chặn, revoke/unpublish, evidence riêng claim |
| T21–T22 | `DiscoveryIntegrityTests`, `SqlServerModelTests`: composite FK, unique/check constraint |
| T24–T25 | `DiscoveryHttpTests`: publish/revoke race; SQL trigger gây lỗi audit để chứng minh rollback |
| T27 | `NasaSimulationTests`: timeout/429/500/schema lỗi, public endpoint vẫn đọc DB |
| T29 | `DiscoveryHttpTests`: resource filter/canonical representation, pin version, republish khiến version cũ không public |
| T33–T35 | `SqlServerModelTests`, `MigrationUpgradeTests`: database trống, InitialCreate có dữ liệu, migrate/backfill lại, không giả review |
| T36 | `DiscoveryUnitTests`: schema không hỗ trợ và legacy snapshot bị chặn; giữ bản gốc để tái biên tập có kiểm soát |

Simulation chạy worker thật với HTTP và SQL; payload mock là dữ liệu tổng hợp. Official fixtures trong thư mục test dùng đối chiếu parser, không phải bằng chứng NASA đang hoạt động hay toàn bộ kho dữ liệu đã import. Chưa load-test production hoặc kiểm chứng mọi dataset PDS. Health nội bộ: `GET /api/admin/discover-mars/health` với admin token; xem database connectivity và thời điểm worker poll.

## Điều kiện nghiệm thu nội dung còn cần thực hiện

Database ứng dụng hiện chưa có bài khoa học được duyệt. Guide yêu cầu **ít nhất một bài thật đã duyệt cho mỗi category**. API và quy trình hỗ trợ đầy đủ việc này; cần editor/reviewer thực tế kiểm chứng nguồn, claim và quyền sử dụng, rồi administrator xuất bản. Không dùng các quyết định giả trong integration test làm review thật.

[Gói nội dung chờ review](Discovery_Content_Review_Pack.md) cung cấp 5 bản thảo ngắn và nguồn NASA để bắt đầu. Chúng chưa được nạp/publish và không tự nhận là bài đã duyệt.
