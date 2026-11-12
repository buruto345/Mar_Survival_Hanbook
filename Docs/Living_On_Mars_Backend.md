# Vận hành backend Living on Mars

## Phát hành

Chạy từ thư mục gốc với ConnectionStrings__MarsDatabase trỏ đến SQL Server đích. CLI và API dùng chung Storage__RawRoot.

```powershell
dotnet tool restore
dotnet ef database update --project Backend/Mars_Survival_Hanbook
dotnet run --project Backend/Mars_Survival_Hanbook -- seed
dotnet run --project Backend/Mars_Survival_Hanbook -- publish-living-scenario
dotnet run --project Backend/Mars_Survival_Hanbook -- --urls http://127.0.0.1:5086
```

Lệnh phát hành tạo sáu bài tiếng Anh và hai phiên bản outpost. Bản 1 dùng giả định cho fixture. Bản 2 dùng định mức oxy từ BVAD, quy đổi 0,84 kg/người/24 giờ thành 0,8631 kg/người/sol theo giả định 24,66 giờ/sol. Các định mức khác được đánh dấu Assumption. CLI lưu trích dẫn và ghi chú vào SourceService và RawStorage. [BVAD, trang in 47](https://ntrs.nasa.gov/api/citations/20210024855/downloads/BVAD_2.15.22-final.pdf)

Để dùng revision từ connector NTRS, nhập record 20210024855, duyệt revision rồi chạy `publish-living-scenario --bvad-revision <guid> --version 3`. Dùng `withdraw-living-scenario outpost 2` để rút phiên bản. Danh sách tự chọn bản cao nhất còn đủ điều kiện. Thu hồi revision làm ẩn cả bài và kịch bản phụ thuộc từ lượt đọc tiếp theo.

## Chạy preview

Import [collection Postman](../postman/collections/Living%20on%20Mars.postman_collection.json), đặt baseUrl rồi chạy collection. Development cung cấp Swagger tại /swagger/v1/swagger.json. [OpenAPI Living on Mars](../postman/specs/living-on-mars.openapi.json)

Gửi POST tới /api/living-on-mars/simulations/preview với body sau.

```json
{
  "scenarioKey": "outpost",
  "scenarioVersion": 1,
  "durationSols": 1,
  "facilities": { "solar": 1, "habitat": 2, "electrolyzer": 0 }
}
```

Một dàn pin tạo 4 kWh. Hai habitat dùng 2 kWh, máy tái chế nhận 2 kWh và xử lý 2 kg nước thải. Kho sạch nhận thêm 1,5 kg vào 8 kg ban đầu. Đoàn uống 8 kg rồi dùng được 1,5 trong 2 kg nước vệ sinh yêu cầu. firstShortage.need.key là hygieneWater. Nếu chỉ tăng solar lên 2, máy xử lý đủ 4 kg và hai nhu cầu nước đều được cấp đủ.

GET /api/living-on-mars/scenarios/outpost/versions/2 trả đơn vị, giới hạn, định mức, nguồn, thiết bị và thứ tự trong sol. Kho đầu nhập theo `initialStocks.water = {"quantity":8,"unit":"kg"}`. Facilities là số thiết bị có sẵn. HousingToBuild là số habitat xếp hàng xây. CropArea là m² của lứa gieo đầu tiên. PowerPriority là hoán vị đầy đủ của recycler, electrolyzer và crops. CrewAllocations tính theo người-giờ của cả đoàn mỗi sol. Các khóa bị bỏ trống giữ mặc định. Engine gộp thiết bị cùng loại và tính điện trên cả sol. EmptyStock cho biết kho trống. UnmetDemand cho biết nhu cầu chưa được đáp ứng.

## CMS và nguồn văn bản tiếng Anh

| DTO | Trường văn bản và nơi cung cấp |
|---|---|
| Module, category | Name, slug, description từ taxonomy tiếng Anh của DiscoverySeed. |
| Article card, detail, citation, media | Title, summary, sections, headings, claims, locator, caption, alt text, credit, source title, usage note và văn bản của resource đến từ snapshot đã duyệt theo hash. Tên riêng, tác giả, ID và URL giữ danh tính nguồn. Bài Survival Knowledge liên quan có link khi đang public bằng tiếng Anh. |
| CMS article, version, review | ID, hash, trạng thái và tên trường cần duyệt cho draft. Nội dung version và preview yêu cầu duyệt tiếng Anh. |
| Scenario list và version | Title, key, engineVersion, languageCode, unit, basis, assumptionNote, evidenceType, testEnvironment, sourceLocator, sourceUrl, solRules và khóa hoạt động từ pack hoặc trích dẫn BVAD. |
| Preview | Khóa nhu cầu, hoạt động, thiết bị, kho, đơn vị từ kịch bản. Mã thiếu hụt, cảnh báo kho, limitingFactors và câu giải thích điện từ engine. |
| Problem Details | Title, code, tên trường và thông báo validation do API tạo. Trace ID dùng để tra log. |

Biên tập viên khai báo englishCitations theo revision cho tiêu đề, credit và usage note. Xuất nội dung chờ duyệt bằng `export-living-review <version-guid> <new-file.json>`, đọc toàn bộ snapshot và so hash với GET version. Gửi POST đến /api/admin/living-on-mars/articles/{articleId}/versions/{versionId}/english-review với ETag của version và body `{"snapshotHash":"<sha256>","allTextReviewed":true}`. Tiếp tục submit, claim review, quyết định và publish qua CMS. Restore tạo version cần duyệt lại.

## Kiểm thử và giới hạn tải

Pack giới hạn 1–60 sol, 1–16 người, 8 thiết bị mỗi loại và 100 m² cây. Trên Kestrel và LocalDB của máy phát triển, outpost 2, engine 1, 60 sol, 16 người, 8 thiết bị mỗi loại và 100 m² cây trả 187.815 byte JSON. Mười lần gọi sau warm-up mất 15–33 ms ở cấu hình đó.

Chạy `./scripts/test-discovery.ps1` trên Windows có LocalDB và Postman CLI để bật fixture SQL Server và mock NASA. Workflow CI chạy các ca Living cùng hồi quy SQL Server. Preview đọc phiên bản đã lưu trong DB.
