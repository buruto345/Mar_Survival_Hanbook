# Science and Technology: vận hành backend

Các lệnh PowerShell dưới đây chạy từ thư mục gốc repo. Chọn đúng DB đích qua cấu hình ASP.NET Core (ví dụ biến môi trường `ConnectionStrings__MarsDatabase`). Không đưa mật khẩu, token hoặc connection string có thông tin đăng nhập vào Git.

```powershell
dotnet tool restore
dotnet ef database update --project Backend/Mars_Survival_Hanbook
dotnet run --project Backend/Mars_Survival_Hanbook -- seed
dotnet run --project Backend/Mars_Survival_Hanbook -- seed
dotnet run --project Backend/Mars_Survival_Hanbook -- import-science-pack
```

Migration chỉ tạo bảng liên kết có hai khóa ngoại; không chuyển thiết bị của Living thành bài viết. Pack mặc định là [release.en.json](../Backend/Mars_Survival_Hanbook/Data/Science/release.en.json). Có thể dùng `import-science-pack --file PATH`. Nguồn mới ở trạng thái Pending; đầu ra in version ID và revision ID cần duyệt. Lỗi giữa chừng: sửa nguyên nhân và chạy lại. Mỗi mục có slug và hash nội dung. Import cùng pack giữ nguyên phiên bản; thay đổi tạo draft, còn phiên bản công khai vẫn giữ nguyên. Nếu draft đã được người biên tập chỉnh, importer dừng ở mục đó để tránh ghi đè.

## Mở lại bản đã nghiệm thu trên máy này

Database `MarsScienceAcceptance_20261112` trên `(localdb)\MSSQLLocalDB` đã có năm hồ sơ được duyệt và xuất bản. Chạy từ thư mục gốc repo trong PowerShell để mở API với dữ liệu đó.

```powershell
$env:ConnectionStrings__MarsDatabase = 'Server=(localdb)\MSSQLLocalDB;Database=MarsScienceAcceptance_20261112;Integrated Security=true;TrustServerCertificate=true'
$env:Storage__RawRoot = Join-Path $env:LOCALAPPDATA 'MarsScienceAcceptance_20261112/raw'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Nasa__WorkerEnabled = 'false'
dotnet run --no-launch-profile --project Backend/Mars_Survival_Hanbook/Mars_Survival_Hanbook.csproj -- --urls http://127.0.0.1:5086
```

Khi app khởi động, mở [tổng quan](http://127.0.0.1:5086/api/science-technology) hoặc [hồ sơ thu hồi nước](http://127.0.0.1:5086/api/science-technology/technologies/iss-water-recovery-system).

## Contract cho frontend

Dùng [OpenAPI](../postman/specs/science-technology.openapi.json) và [collection Postman](../postman/collections/Science%20and%20Technology.postman_collection.json). Đi từ `GET /api/science-technology` tới `/technologies?category=water-recycling-systems&page=1&pageSize=20` rồi `/technologies/iss-water-recovery-system` (hai đường dẫn sau cùng dùng prefix module). Tổng quan có `categories[].publishedArticleCount`. Danh sách dùng `items, page, pageSize, total`; thẻ có `id, slug, title, summary, categorySlug, technologyStatus, testEnvironment`.

Response mẫu lấy trực tiếp từ API công khai trên LocalDB nghiệm thu: [danh sách thu hồi nước](Science_Examples/water-cards.json) và [hồ sơ ISS Water Recovery System](Science_Examples/water-profile.json). ID trong hai file thuộc DB nghiệm thu.

Chi tiết và preview dùng cùng snapshot. Đọc `sections` với anchor `problem` và `principle`; khối `technologyProfile` chứa `technologyId`, trạng thái, môi trường, loại bằng chứng, khả năng, giới hạn, tài nguyên và mối nguy. Mảng `facts` có thể rỗng. Nếu có fact, đọc `originalValue, originalUnit, context, evidenceType, testEnvironment, assumptions, uncertainty, sourceRevisionId` trên **fact**, và vị trí trong nguồn ở `technologyProfile.factLocators[fact.id]`. Dùng `sources` trong cùng response để tìm `sourceId`, mở `/api/science-technology/sources/{sourceId}/revisions/{sourceRevisionId}`. Các trường dịch có thể nhập qua `englishCitations` theo revision ID. Mặc định tiếng Anh; ngôn ngữ khác trả 400.

## Duyệt và phát hành

1. Đăng nhập bằng tài khoản Admin qua `POST /api/auth/login`. Chọn hoặc tạo định danh ở `/api/admin/science-technology/catalog`. GET theo ID trả giá trị gợi ý; chỉnh draft không sửa bản ghi dùng chung. Với số liệu, chọn fact từ CMS dùng chung `/api/admin/discover-mars/content/facts`. Tra cứu ID ở `catalog/resources` và `catalog/hazards`. Chọn các tài nguyên/mối nguy qua ID và chụp nhãn tiếng Anh, ghi chú, hướng dòng hoặc mô tả giảm thiểu vào draft.
2. Kiểm tra URL, tác giả, cấu hình thử và locator. Dùng CMS nguồn dùng chung: GET `/api/admin/discover-mars/source-revisions/{id}`, rồi POST `.../{id}/decision` với ETag của revision và `{"decision":"Accepted","notes":"..."}`. Duyệt fact nếu có. Nếu cần sửa fact chưa duyệt trong snapshot, lưu draft mới sau khi duyệt fact.
3. Trong collection, dùng GET article để lấy `articleId`, `currentDraftVersionId` và ETag. Preview đúng version, kiểm tra cả caption, đơn vị, nhãn tài nguyên và trích dẫn bằng tiếng Anh. Submit với ETag của **article**. Claim và quyết định bằng ETag của **review task**. Nếu nội dung cần sửa, chọn `ChangesRequested`.
4. GET lại article để lấy ETag mới rồi publish đúng `versionId`. Dùng GET public kiểm tra năm nhóm theo thứ tự, thẻ và revision. Collection có cả lệnh ghi và gỡ bài: chọn từng request phù hợp khi vận hành. Với pack đã import, chọn draft có sẵn thay vì POST tạo lại hồ sơ.
5. Nếu phát hành nhầm, unpublish kèm lý do. Restore một version thành draft, rồi submit, duyệt và publish lại. Nếu nguồn đã bị thu hồi, thay revision trong draft mới trước khi submit.

Để chạy đúng fixture SQL Server LocalDB và mô phỏng NASA của repo trên Windows, dùng `./scripts/test-discovery.ps1`. Để xuất lại OpenAPI, chạy app ở Development rồi `python scripts/export-science-openapi.py --base-url http://127.0.0.1:5086`. Thử nghiệm trên LocalDB không thay cho kiểm tra DB và tài khoản của staging thực.
