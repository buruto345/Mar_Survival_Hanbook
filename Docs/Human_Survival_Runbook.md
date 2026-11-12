# Human Survival trên SQL Server

Bản nghiệm thu cục bộ dùng database `MarsHumanAcceptance_20261112` trên SQL Server LocalDB. Sáu bài dùng snapshot schema 1. Bài tập, câu hỏi, lời giải và giới hạn nằm trong các section có thứ tự. Không cần migration riêng cho module này.

## Mở bản nghiệm thu

Chạy từ thư mục gốc repo. Sau khi host khởi động, mở [tổng quan](http://127.0.0.1:5097/api/human-survival/overview).

```powershell
$env:ConnectionStrings__MarsDatabase = 'Server=(localdb)\MSSQLLocalDB;Database=MarsHumanAcceptance_20261112;Integrated Security=true;TrustServerCertificate=true'
$env:Storage__RawRoot = Join-Path $env:LOCALAPPDATA 'MarsHumanAcceptance_20261112/raw'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Nasa__WorkerEnabled = 'false'
dotnet run --no-launch-profile --project Backend/Mars_Survival_Hanbook -- --urls http://127.0.0.1:5097
```

## Khởi tạo database mới

Đặt `ConnectionStrings__MarsDatabase` cho database đích và `Storage__RawRoot` cho thư mục lưu bằng chứng có quyền ghi. Bên ngoài Development, cấu hình `Jwt__SigningKey` qua kho bí mật của môi trường. Từ thư mục gốc chạy các lệnh sau.

```powershell
dotnet tool restore
dotnet ef database update --project Backend/Mars_Survival_Hanbook
dotnet run --project Backend/Mars_Survival_Hanbook -- seed
dotnet run --project Backend/Mars_Survival_Hanbook -- import-human-survival-pack
```

[Gói tiếng Anh](../Backend/Mars_Survival_Hanbook/Data/HumanSurvival/release.en.json) chứa sáu tình huống. Khóa `source` trong mỗi bài trỏ tới `sources[].key`. Mỗi claim có locator, môi trường và giới hạn diễn giải. Import ghép NTRS ID với revision cục bộ hoặc tải metadata và tài liệu công khai qua connector. Hai tài liệu theo URL dùng luồng nguồn thủ công. Nguồn mới cần được duyệt. Bài viết nhập vào ở trạng thái draft.

Muốn nhập bản sửa, dùng `import-human-survival-pack --file PATH`. Đầu ra ghi slug, article ID, version ID và source revision ID. Nếu tải lỗi, xử lý mục được báo rồi chạy lại. Các mục trước đó đã commit. Import cùng nội dung tái sử dụng draft. Khi gặp version do CMS chỉnh, importer dừng và ghi slug cần đối chiếu. Biên tập viên tiếp tục sửa qua CMS. Các ID trong ví dụ thuộc database nghiệm thu.

## Hợp đồng cho frontend

1. Đọc `GET /api/human-survival/overview`. Các nhóm ở đúng thứ tự hiển thị. Dùng `articleSlug` khi nhóm có đúng một bài công khai. Giá trị null cũng xuất hiện ở nhóm rỗng.
2. Đọc `GET /api/human-survival/articles?category=teamwork&page=1&pageSize=20` để lấy `items`, `page`, `pageSize`, `total`. Sau đó mở `/articles/filter-handover` dưới cùng prefix.
3. Hiển thị `sections` theo thứ tự. Để người đọc tự viết lời bàn giao tại `question` trước khi mở `explanation`. Đọc `evidence-limits` và nối `claims[].sourceRevisionIds` với `sources[].sourceRevisionId`. Claim trong bộ sáu bài dùng key của section chứa bằng chứng. Locator nằm ở `claims[].locator.section`.
4. Mở `GET /api/human-survival/sources/{sourceId}/revisions/{revisionId}` với hai ID từ cùng một citation. Chi tiết Physical Health có `survivalKnowledgeUrl` khi bài trọng lực tiếng Anh đang công khai. Render liên kết khi có trường này. Đọc các link CMS khác từ `relatedArticles`.
5. API mặc định tiếng Anh. `?language=vi` trả 400. Slug sai, bài đã rút và revision thiếu bài công khai tham chiếu trả 404. Thu hồi nguồn bắt buộc cũng loại bài khỏi danh sách và số đếm.

[OpenAPI](../postman/specs/human-survival.openapi.json) · [Postman collection](../postman/collections/Human%20Survival.postman_collection.json) · [Tổng quan thực tế](Human_Survival_Examples/overview.json) · [Thẻ Teamwork](Human_Survival_Examples/teamwork-cards.json)

| Bài mẫu | Body tạo draft | Response công khai |
|---|---|---|
| Physical Health | [Draft](Human_Survival_Examples/physical-health-draft.json) | [Article](Human_Survival_Examples/physical-health-article.json) |
| Teamwork | [Draft](Human_Survival_Examples/teamwork-draft.json) | [Article](Human_Survival_Examples/teamwork-article.json) |

## Duyệt, sửa và rút bài

Đăng nhập Admin ở `POST /api/auth/login`, dùng Bearer token. Các request ghi trong collection dành cho người vận hành chọn từng bước. Khi dùng bài đã nhập, lấy article ID từ CLI hoặc danh sách CMS. Request tạo bài thực hành dùng slug riêng.

1. Đọc revision tại `/api/admin/discover-mars/source-revisions/{id}`, kiểm tra tài liệu, tác giả, bằng chứng và locator. Gửi `POST .../{id}/decision` với `If-Match` từ GET revision, body gồm `decision: Accepted` và `notes`. Duyệt fact trước khi chụp snapshot nếu dùng fact.
2. Dưới `/api/admin/human-survival`, GET `articles/{id}`, lấy `currentDraftVersionId` và ETag. GET `articles/{id}/versions/{versionId}/preview`. Đọc mọi trường hiển thị, kể cả caption và ghi chú nguồn nếu có. So sánh với bản gốc và xác nhận tình huống giả định được ghi rõ.
3. POST `articles/{id}/versions/{versionId}/submit` với ETag article. Claim review bằng `reviews/{taskId}/claim`; dùng ETag mới của task để POST `reviews/{taskId}/decision`. Chọn `ChangesRequested` và ghi rõ trường cần sửa, hoặc `Approved` sau khi hoàn tất checklist. Ghi việc duyệt tiếng Anh vào `notes` của chính version đó.
4. PUT `articles/{id}/draft` với ETag article để lưu bản sửa thành version mới. Bản đã xuất bản vẫn phục vụ khi draft đang được duyệt. GET lại ETag article rồi POST `articles/{id}/publish` với `versionId` đã duyệt.
5. POST `articles/{id}/unpublish` với `reason` để rút. POST `articles/{id}/restore` với `sourceVersionId` và `changeSummary` để tạo draft từ nội dung cũ. Duyệt và publish version mới. Cả hai thao tác lấy ETag từ article.

## Bằng chứng nghiệm thu

[Đối chiếu sáu claim và version đã duyệt](Human_Survival_Examples/release-evidence.json) ghi nguồn, tác giả, locator, giới hạn, review ID và revision ID từ DB nghiệm thu. Teamwork ở version 3 sau thử nghiệm sửa câu hỏi tiếng Việt, yêu cầu dịch lại, restore câu hỏi tiếng Anh và duyệt lại. Các ví dụ phản ánh dữ liệu cục bộ.

Chạy `./scripts/test-discovery.ps1` trên Windows có SQL Server LocalDB, Postman CLI và .NET. Script bật fixture SQL và NASA simulation, chạy test HTTP, build với warning thành lỗi và kiểm tra model EF. Bộ test Human Survival bao gồm nhập lặp, phục hồi lỗi nguồn, kiểm tra ngôn ngữ, ranh giới module, phân quyền, thu hồi revision và trạng thái hiển thị. Kiểm tra CMS trên bản nghiệm thu còn bao gồm ETag cũ, rút và xuất bản lại bài, cùng luồng đọc chéo sang Survival Knowledge.

Lần nghiệm thu ngày 12/11/2026 đạt 87/87 test, không có test bị bỏ qua. Build Debug và Release đều có 0 warning, 0 error. EF xác nhận model khớp migration. Năm request đọc trong collection Postman chạy trên host nghiệm thu trả HTTP 200 và đạt 9/9 assertion. Khi tắt connector NTRS, cả sáu chi tiết và revision nguồn vẫn đọc được. Khi rút bài trọng lực rồi xuất bản lại, liên kết trong Physical Health lần lượt biến mất và xuất hiện lại.
