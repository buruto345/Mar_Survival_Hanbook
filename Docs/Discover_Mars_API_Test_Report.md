# Báo cáo kiểm thử API Discover Mars

Ngày tổng hợp: **06/11/2026**. Backend thật tại `http://127.0.0.1:5093`, dùng bản sao SQL Server LocalDB của `MarsSurvivalHandbook`.

Đã chạy **271 HTTP request**, bao phủ **84/84 operation Discover Mars** (mỗi cặp method + route là một operation). Có thêm login và me để hỗ trợ xác thực. **78 operation đạt các ca đã chạy; 6 operation có lỗi trong ít nhất một ca.** Không có operation nào bị bỏ qua.

Các nhóm kết quả không đồng nghĩa với chất lượng dữ liệu: API có thể hoạt động đúng nhưng chưa có dữ liệu NASA được xuất bản. Các thao tác ghi chỉ chạy trên database clone, worker NASA bị tắt. Fixture được ghi rõ là dữ liệu kiểm thử, không phải bằng chứng khoa học.

## Các lỗi đã tái hiện

| Vấn đề | Kết quả thực tế | Phạm vi |
|---|---|---|
| Citation của bài tiếng Anh | GET public sources/{id}/revisions/{revisionId}?language=en trả 404 sau khi fixture đã publish, nguồn đã Accepted | 1 operation; SourceAsync gọi Publications() mặc định vi |
| URL ảnh trong snapshot tiếng Anh | URL chứa articleVersionId nhưng thiếu language=en trả 404; cùng file thêm language=en trả 200 image/jpeg | 1 operation; cần sửa nơi sinh URL và kiểm tra cover/thumbnail dùng lại URL này |
| Bounding box bản đồ NASA | boundingBoxJson là chuỗi rỗng, vi phạm minLength của schema | 4 operation: list map-layers, detail map-layer, article version detail, article preview |

Các lỗi trên chưa được sửa trong lượt kiểm thử này. Không có response 5xx trong 271 request đã ghi nhận.

## Dữ liệu thật hiện có và dữ liệu còn thiếu

| Nhóm | Baseline | Ý nghĩa |
|---|---:|---|
| Articles | 5 | Đều là Draft; chưa có bài public |
| Sources | 25 | Revision chờ duyệt; chưa được xác nhận để xuất bản |
| Import jobs | 4 | Có dữ liệu lịch sử import để tra cứu |
| Facts | 8 | Tra cứu qua admin được |
| Missions | 5 | Tra cứu qua admin được |
| Locations | 4 | Tra cứu qua admin được |
| Timeline events | 4 | Tra cứu qua admin được |
| Media | 10 | Có metadata/ảnh lưu trữ; public phụ thuộc xuất bản |
| Map layers | 1 | Có dữ liệu nhưng boundingBoxJson chưa hợp lệ |
| Measurements | 0 | Chưa có bản ghi đo đạc chuẩn hóa để tra; fixture chứng minh API đọc/ghi hoạt động |
| Review tasks | 0 | Chưa có review thật; luồng submit/claim/approve đã chạy bằng fixture |

Baseline public articles, facts, missions, locations, timeline, map-layers và measurements đều total=0. Đây là kết quả đúng khi chưa xuất bản. Với fixture tiếng Anh đã được duyệt và publish, các danh sách và detail tài nguyên đã trả dữ liệu; ngoại lệ citation và URL ảnh nêu trên.

## Kiểm tra từng trường JSON

Đã kiểm tra schema cho **241 response**: success JSON đối chiếu live Swagger lúc chạy; ProblemDetails được kiểm tra bổ sung từ OpenAPI lưu trong repository. Có **4 response lỗi schema**, đều liên quan boundingBoxJson.

Validator đi đệ quy qua object/array và kiểm tra type, required theo schema, nullable, enum, UUID/date-time, giới hạn số và độ dài chuỗi khi contract có khai báo. Không coi field optional hoặc additionalProperties được schema cho phép là lỗi. Vì vậy kết quả đạt schema không chứng minh tính chính xác khoa học, cũng không bảo đảm các field chưa được contract ràng buộc.

| Response | Field sai | Actual |
|---|---|---|
| `GET /api/admin/discover-mars/content/map-layers` | `items/0/boundingBoxJson` | `'' should be non-empty` |
| `GET /api/admin/discover-mars/content/map-layers/{id}` | `boundingBoxJson` | `'' should be non-empty` |
| `GET /api/admin/discover-mars/articles/{id}/versions/{versionId}` | `snapshot/mapLayers/0/boundingBoxJson` | `'' should be non-empty` |
| `GET /api/admin/discover-mars/articles/{id}/versions/{versionId}/preview` | `mapLayers/0/boundingBoxJson` | `'' should be non-empty` |

Ngoại lệ về hợp đồng: health chưa có response schema đủ để kiểm tra từng field; 25 raw manifest JSON cũng chưa có schema JSON tương ứng. Chúng parse được nhưng không được tính là đạt kiểm tra từng field. File ảnh là binary, không phải JSON; lượt này xác nhận HTTP 200 và MIME image/jpeg, chưa kiểm tra trực quan ảnh hoặc checksum qua HTTP. Các 401 rỗng là response xác thực, không phải lỗi JSON.

Health trả databaseReady=true và lastPoll=0001-01-01T00:00:00+00:00: worker tắt trong môi trường audit, nên giá trị này không chứng minh lần đồng bộ NASA gần nhất.

Các ca âm đã chạy: không token vào admin trả 401; page=0, enum status=999, thiếu bộ lọc thời gian measurements trả 400; bài không tồn tại/chưa publish trả 404. ProblemDetails có thể chứa thông báo tiếng Việt dù nội dung bài là tiếng Anh; nếu yêu cầu toàn bộ API chỉ tiếng Anh thì cần chuẩn hóa thông báo lỗi riêng.

## Danh sách đầy đủ 84 API

Đạt = status đúng kỳ vọng và không phát hiện lỗi schema trong các ca đã chạy; không phải cam kết mọi đầu vào đều đúng. Các dòng import POST chỉ xác nhận nhận job/retry (202), chưa chứng minh worker tải NASA trực tiếp trong lượt HTTP audit. 401/400/404 cố ý trong ca âm không bị tính là lỗi.

| Method | Route | HTTP đã thấy | Kết quả | Dữ liệu baseline | JSON đã kiểm tra/request |
|---|---|---|---|---|---|
| GET | `/api/admin/discover-mars/articles` | 200,401 | Đạt các ca đã chạy | Baseline total=5 | 1/2 |
| GET | `/api/admin/discover-mars/articles/{id}` | 200 | Đạt các ca đã chạy | Có response baseline | 8/8 |
| GET | `/api/admin/discover-mars/articles/{id}/versions` | 200 | Đạt các ca đã chạy | Baseline total=2 | 5/5 |
| GET | `/api/admin/discover-mars/articles/{id}/versions/{versionId}` | 200 | Có lỗi | Có response baseline | 5/5 |
| GET | `/api/admin/discover-mars/articles/{id}/versions/{versionId}/preview` | 200 | Có lỗi | Có response baseline | 5/5 |
| GET | `/api/admin/discover-mars/connectors` | 200 | Đạt các ca đã chạy | Có response baseline | 1/1 |
| GET | `/api/admin/discover-mars/connectors/{key}` | 200 | Đạt các ca đã chạy | Có response baseline | 6/6 |
| GET | `/api/admin/discover-mars/content/facts` | 200,401 | Đạt các ca đã chạy | Baseline total=8 | 1/2 |
| GET | `/api/admin/discover-mars/content/facts/{id}` | 200 | Đạt các ca đã chạy | Có response baseline | 9/9 |
| GET | `/api/admin/discover-mars/content/locations` | 200 | Đạt các ca đã chạy | Baseline total=4 | 1/1 |
| GET | `/api/admin/discover-mars/content/locations/{id}` | 200 | Đạt các ca đã chạy | Có response baseline | 5/5 |
| GET | `/api/admin/discover-mars/content/map-layers` | 200 | Có lỗi | Baseline total=1 | 1/1 |
| GET | `/api/admin/discover-mars/content/map-layers/{id}` | 200 | Có lỗi | Có response baseline | 2/2 |
| GET | `/api/admin/discover-mars/content/measurements` | 200 | Đạt các ca đã chạy | Baseline total=0 | 1/1 |
| GET | `/api/admin/discover-mars/content/measurements/{id}` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 2/2 |
| GET | `/api/admin/discover-mars/content/media` | 200 | Đạt các ca đã chạy | Baseline total=10 | 1/1 |
| GET | `/api/admin/discover-mars/content/media/{id}` | 200 | Đạt các ca đã chạy | Có response baseline | 11/11 |
| GET | `/api/admin/discover-mars/content/missions` | 200 | Đạt các ca đã chạy | Baseline total=5 | 1/1 |
| GET | `/api/admin/discover-mars/content/missions/{id}` | 200 | Đạt các ca đã chạy | Có response baseline | 6/6 |
| GET | `/api/admin/discover-mars/content/timeline-events` | 200 | Đạt các ca đã chạy | Baseline total=4 | 1/1 |
| GET | `/api/admin/discover-mars/content/timeline-events/{id}` | 200 | Đạt các ca đã chạy | Có response baseline | 5/5 |
| GET | `/api/admin/discover-mars/health` | 200 | Đạt các ca đã chạy | Có response baseline | 0/1 |
| GET | `/api/admin/discover-mars/imports` | 200 | Đạt các ca đã chạy | Baseline total=4 | 1/1 |
| GET | `/api/admin/discover-mars/imports/{jobId}` | 200 | Đạt các ca đã chạy | Có response baseline | 5/5 |
| GET | `/api/admin/discover-mars/imports/{jobId}/items` | 200 | Đạt các ca đã chạy | Baseline total=1,3,10 | 4/4 |
| GET | `/api/admin/discover-mars/reviews` | 200 | Đạt các ca đã chạy | Baseline total=0 | 1/1 |
| GET | `/api/admin/discover-mars/reviews/{id}` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| GET | `/api/admin/discover-mars/source-revisions/{id}` | 200 | Đạt các ca đã chạy | Có response baseline | 27/27 |
| GET | `/api/admin/discover-mars/source-revisions/{id}/raw` | 200 | Đạt các ca đã chạy | Có response baseline | 0/25 |
| GET | `/api/admin/discover-mars/sources` | 200,401 | Đạt các ca đã chạy | Baseline total=25 | 1/2 |
| GET | `/api/admin/discover-mars/sources/{id}` | 200 | Đạt các ca đã chạy | Có response baseline | 25/25 |
| GET | `/api/admin/discover-mars/sources/{id}/revisions` | 200 | Đạt các ca đã chạy | Baseline total=1 | 25/25 |
| GET | `/api/discover-mars` | 200 | Đạt các ca đã chạy | Có response baseline | 2/2 |
| GET | `/api/discover-mars/articles` | 200,400 | Đạt các ca đã chạy | Baseline total=0 | 3/3 |
| GET | `/api/discover-mars/articles/{slug}` | 200,404 | Đạt các ca đã chạy | Chưa xuất bản: 404 đúng | 7/7 |
| GET | `/api/discover-mars/categories` | 200 | Đạt các ca đã chạy | Có response baseline | 2/2 |
| GET | `/api/discover-mars/categories/{slug}` | 200 | Đạt các ca đã chạy | Có response baseline | 2/2 |
| GET | `/api/discover-mars/facts` | 200 | Đạt các ca đã chạy | Baseline total=0 | 2/2 |
| GET | `/api/discover-mars/locations` | 200 | Đạt các ca đã chạy | Baseline total=0 | 2/2 |
| GET | `/api/discover-mars/locations/{id}` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| GET | `/api/discover-mars/map-layers` | 200 | Đạt các ca đã chạy | Baseline total=0 | 2/2 |
| GET | `/api/discover-mars/map-layers/{id}` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| GET | `/api/discover-mars/measurements` | 200,400 | Đạt các ca đã chạy | Baseline total=0 | 3/3 |
| GET | `/api/discover-mars/media/{id}/file` | 200,404 | Có lỗi | Chỉ kiểm thử fixture | 1/2 |
| GET | `/api/discover-mars/missions` | 200,400 | Đạt các ca đã chạy | Baseline total=0 | 3/3 |
| GET | `/api/discover-mars/missions/{id}` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| GET | `/api/discover-mars/sources/{id}/revisions/{revisionId}` | 404 | Có lỗi | Chỉ kiểm thử fixture | 1/1 |
| GET | `/api/discover-mars/timeline` | 200 | Đạt các ca đã chạy | Baseline total=0 | 2/2 |
| POST | `/api/admin/discover-mars/articles` | 201 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/articles/{id}/publish` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/articles/{id}/restore` | 201 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/articles/{id}/unpublish` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/articles/{id}/versions/{versionId}/submit` | 201 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/content/facts` | 201 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/content/facts/{id}/archive` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/content/locations` | 201 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/content/locations/{id}/archive` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/content/map-layers` | 201 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/content/map-layers/{id}/archive` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/content/measurements` | 201 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/content/measurements/{id}/archive` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/content/media` | 201 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/content/media/{id}/archive` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/content/missions` | 201 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/content/missions/{id}/archive` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/content/timeline-events` | 201 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/content/timeline-events/{id}/archive` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/imports` | 202 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/imports/{jobId}/retry` | 202 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/reviews/{id}/claim` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/reviews/{id}/decision` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/source-revisions/{id}/amend` | 201 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/source-revisions/{id}/decision` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/source-revisions/{id}/revoke` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| POST | `/api/admin/discover-mars/sources/manual` | 201 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| PUT | `/api/admin/discover-mars/articles/{id}/draft` | 201 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| PUT | `/api/admin/discover-mars/connectors/{key}` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| PUT | `/api/admin/discover-mars/content/facts/{id}` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| PUT | `/api/admin/discover-mars/content/locations/{id}` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| PUT | `/api/admin/discover-mars/content/map-layers/{id}` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| PUT | `/api/admin/discover-mars/content/measurements/{id}` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| PUT | `/api/admin/discover-mars/content/media/{id}` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| PUT | `/api/admin/discover-mars/content/missions/{id}` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |
| PUT | `/api/admin/discover-mars/content/timeline-events/{id}` | 200 | Đạt các ca đã chạy | Chỉ kiểm thử fixture | 1/1 |

## Kiểm thử hồi quy và bảo toàn dữ liệu

`scripts/test-discovery.ps1` đã kết thúc exit code 0: **71 passed, 0 failed, 0 skipped**; build **0 warning, 0 error**; EF báo không có thay đổi model chưa có migration. Suite dùng SQL Server và Postman NASA simulation, bao gồm worker, phân trang/dedup/retry và kiểm soát quyền. Đây là bằng chứng với NASA giả lập, không phải lượt gọi NASA production trực tiếp.

Sau kiểm thử đã đối chiếu database chính: 5 Articles đều Draft, 25 SourceRevisions, 10 ArticleVersions, 0 Measurements. Server audit đã dừng; database clone đã được xóa và password fixture trong state.json đã được che. Evidence được giữ lại; muốn chạy lại cần tạo clone và tài khoản audit mới.

## Bằng chứng và giới hạn

- `scripts/audit-discovery-api.py`: harness HTTP, cần server và clone được chuẩn bị riêng; không chạy vào database chính.
- `scripts/report-discovery-api-audit.py`: sinh báo cáo từ evidence, không gọi API hoặc thay đổi database.
- `Docs/Discover_Mars_API_Test_Report.json`: kết quả từng operation và field lỗi, không chứa token/password.
- Raw responses và log ở `Backend/Mars_Survival_Hanbook/App_Data/api-audit/` (bị Git ignore). Không push state.json vì có credential fixture.
- Kiểm thử ở localhost, không chứng minh môi trường deploy, hiệu năng tải cao, mọi tổ hợp filter hoặc kết nối NASA trực tiếp đang sẵn sàng.

Thứ tự xử lý đề xuất: sửa lọc ngôn ngữ citation và URL ảnh; bổ sung bounding box đúng từ metadata nguồn; hoàn tất kiểm chứng nguồn/nội dung rồi publish các bài tiếng Anh; chuẩn hóa measurements có timestamp/quality trước khi nhập bảng đo đạc.
