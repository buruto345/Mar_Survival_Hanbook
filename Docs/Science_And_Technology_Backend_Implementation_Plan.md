# Kế hoạch triển khai backend Science & Technology

MVP phục vụ ba màn hình: tổng quan năm chủ đề, danh sách công nghệ trong một chủ đề và hồ sơ chi tiết. Người đọc cần hiểu vấn đề công nghệ giải quyết, nguyên lý, khả năng, giới hạn và bằng chứng về môi trường đã thử nghiệm. Living on Mars phụ trách mô phỏng vận hành căn cứ. Survival Knowledge phụ trách hướng dẫn xử lý tình huống.

Ngôn ngữ phát hành đã chốt là tiếng Anh. Kế hoạch sử dụng hai quy tắc đã đề xuất theo các module hiện có: mỗi hồ sơ có đúng một nhóm và mỗi công nghệ có tối đa một hồ sơ chính thức. Một hồ sơ có nhiều phiên bản. URL công khai luôn dẫn tới phiên bản đang được xuất bản và đủ điều kiện hiển thị.

## Bước 1. Chốt danh mục và contract bằng một hồ sơ mẫu

| Thứ tự | Tên tiếng Anh đề xuất | Category slug | Căn cứ chọn nhóm |
|---|---|---|---|
| 1 | In-Situ Resource Utilization (ISRU) | `in-situ-resource-utilization` | Thu hoặc chuyển hóa vật liệu tại chỗ thành tài nguyên sử dụng được |
| 2 | Robotics and Artificial Intelligence | `robotics-and-ai` | Điều khiển robot, tự hành hoặc ra quyết định |
| 3 | Greenhouses and Space Agriculture | `greenhouses-and-space-agriculture` | Kiểm soát môi trường và sản xuất cây trồng |
| 4 | Water Recycling Systems | `water-recycling-systems` | Thu hồi và xử lý nước đã qua sử dụng |
| 5 | Radiation Protection Technologies | `radiation-protection` | Giảm phơi nhiễm bức xạ |

Chọn nhóm theo chức năng chính được giải thích. Hồ sơ về thu băng bằng robot có thể xếp vào ISRU và liên kết bài về tự hành. Liên kết bài liên quan không làm tăng số hồ sơ ở nhóm thứ hai.

Dùng hồ sơ *Water Recovery System* để thống nhất response với frontend. Tên và slug ở đây là ví dụ thiết kế. Người biên tập chọn hệ thống, cấu hình và nguồn cụ thể trước khi phát hành.

| Thành phần | Trang danh sách | Trang chi tiết |
|---|---|---|
| Nhận diện | ID hồ sơ, slug, title, summary, categorySlug | Thêm technologyId, languageCode, version và ngày phát hành |
| Kỹ thuật | Trạng thái phát triển và môi trường thử đại diện | Thêm vấn đề, nguyên lý, khả năng, giới hạn, loại bằng chứng, tài nguyên đầu vào/đầu ra và mối nguy liên quan |
| Bằng chứng | — | Dữ kiện kèm giá trị, đơn vị, điều kiện đo, môi trường thử và trích dẫn theo phiên bản nguồn |
| Đọc thêm | — | Các hồ sơ hoặc bài liên quan đang công khai và người đọc mở được |

**Hoàn thành khi** frontend dựng được thẻ danh sách và trang chi tiết từ response mẫu, kể cả hồ sơ định tính với `facts: []`. Đặt một tên trường thống nhất cho mỗi giá trị và ghi vào OpenAPI khi triển khai endpoint.

## Bước 2. Đăng ký module và tạo năm nhóm

1. Bổ sung slug `science-technology` và metadata nhận diện endpoint trong `Services/ContentModule.cs`. Gắn metadata lên cả public controller lẫn controller quản trị. Kiểm tra bằng request HTTP vì cơ chế hiện tại mặc định về Discovery khi không nhận diện được module.
2. Tạo module và seed năm category theo thứ tự trên. Dùng khóa module + category slug để chạy lại an toàn. Áp cùng danh mục hợp lệ cho thao tác tạo, sửa và restore hồ sơ.
3. Khi không truyền ngôn ngữ, dùng `en`. Đề xuất trả lỗi validation cho giá trị ngôn ngữ khác ở query hoặc request body. Áp quy tắc ở service để đường import cũng tuân thủ. Người duyệt kiểm tra văn bản thực tế gồm cả caption, nhãn tài nguyên và nội dung trích dẫn hiển thị.

**Hoàn thành khi** seed hai lần vẫn có đúng năm nhóm, kể cả nhóm có số hồ sơ bằng 0. Request public và admin đều nhận đúng module. Một ID hoặc category thuộc module khác không dùng được để sửa hồ sơ Science & Technology.

## Bước 3. Liên kết công nghệ dùng chung với hồ sơ chính thức

1. Giữ `Technology` làm định danh công nghệ dùng chung với Living. Dùng `Article` và `ArticleVersion` cho nội dung biên tập. Quan hệ `ArticleTechnology` hiện phục vụ việc bài viết nhắc tới công nghệ, nên cần biểu diễn riêng ý nghĩa “hồ sơ chính thức”.
2. Thiết kế đề xuất là một bảng liên kết `ScienceTechnologyProfile` với `ArticleId` làm khóa chính và `TechnologyId` có unique index. Cả hai là khóa ngoại. Ràng buộc này cho phép công nghệ tồn tại trước khi có hồ sơ và chặn hai request đồng thời tạo hai hồ sơ cho cùng công nghệ. Service kiểm tra article thuộc đúng module và lưu article, liên kết cùng phiên bản đầu trong một transaction.
3. Dùng slug của article cho URL hồ sơ và khóa slug sau lần xuất bản đầu theo `ArticleService`. Giữ định danh công nghệ của hồ sơ ổn định qua các lần sửa. Chuyển nhóm bằng cách tạo phiên bản mới có `CategorySlug` mới.
4. Tạo EF migration, cập nhật `MarsDbContext` và configuration. Khi nâng cấp database đã có dữ liệu Living, để các thiết bị hiện hữu ở trạng thái chỉ có bản ghi công nghệ cho tới khi người biên tập chủ động tạo hồ sơ.

**Hoàn thành khi** migration chạy được trên SQL Server từ schema hiện tại. Một công nghệ được nhiều bài nhắc tới nhưng chỉ có một hồ sơ chính thức. Tạo trùng trả conflict, không để lại article mồ côi.

## Bước 4. Xây bản nội dung cố định cho từng phiên bản

1. Mở rộng contract draft và snapshot với khối `technologyProfile` tùy chọn cho dữ liệu cũ, bắt buộc với hồ sơ Science & Technology. Trong CMS, cho admin chọn hoặc tạo định danh công nghệ, nhập phần giải thích tiếng Anh và chọn các dữ kiện, nguồn, tài nguyên, mối nguy dùng cho phiên bản đó. Khi chọn công nghệ có sẵn, dùng giá trị phù hợp làm gợi ý ban đầu. Các lần sửa hồ sơ sau đó tạo bản nháp riêng để tránh ghi đè dữ liệu Living dùng chung.
2. Quy định nơi nhập liệu. Title, summary và các section giải thích thuộc bài viết. Khả năng, giới hạn, trạng thái, loại bằng chứng, môi trường thử và quan hệ đầu vào/đầu ra thuộc phần kỹ thuật của hồ sơ. Giao diện có thể trình bày nhiều phần từ cùng snapshot mà người biên tập chỉ nhập mỗi thông tin một lần.
3. Trong `SnapshotService`, chụp giá trị hiển thị của phần kỹ thuật, gồm cả nhãn và ghi chú của tài nguyên/mối nguy. Giữ riêng `TechnologyStatus`, `EvidenceType` và `TestEnvironment`. Mỗi fact giữ giá trị gốc, đơn vị, bối cảnh, môi trường, cùng giả định hoặc độ bất định khi có. Một kết quả trong mô phỏng hoặc trên ISS phải mang đúng nhãn đó.
4. Đưa toàn bộ source revision mà phần kỹ thuật, claim và fact sử dụng vào danh sách phụ thuộc của phiên bản để cơ chế thu hồi nguồn hiện có nhìn thấy. Khi gửi duyệt và publish, kiểm tra trường bắt buộc, nguồn được chấp nhận, bằng chứng phù hợp và mỗi số liệu có đơn vị, bối cảnh, revision truy vết được. Cho phép hồ sơ định tính có nguồn giải thích.
5. Dùng cùng snapshot cho preview, publish, đọc public và restore. Kiểm tra phần tạo hash, đọc snapshot và bộ lọc schema trong `DiscoveryService.Publications` hiện nhận version 1. Ưu tiên mở rộng tương thích bằng trường tùy chọn, xác minh snapshot cũ đọc được và hash đã lưu vẫn hợp lệ.

**Hoàn thành khi** sửa dữ liệu gốc hoặc tạo draft mới không làm thay đổi nội dung phiên bản đã duyệt. Restore tạo draft từ dữ liệu đã chụp, gồm đúng nhóm, giới hạn và revision nguồn tại thời điểm đó.

## Bước 5. Nối luồng quản trị và xuất bản

Mở các route dưới `/api/admin/science-technology` theo controller đang có cho articles và reviews. Dùng lại chính sách CMS, Edit và Admin, kiểm tra quyền sở hữu nếu áp dụng, cơ chế ETag/If-Match, audit và transaction hiện hành.

| Thao tác | Hành vi cần triển khai |
|---|---|
| Tạo và sửa draft | Lưu một nhóm hợp lệ, định danh công nghệ và phiên bản tiếng Anh. Trả ID và ETag để client tiếp tục chỉnh sửa. |
| Preview, submit và review | Preview đúng version. Gửi duyệt chạy validation. Duyệt hoặc yêu cầu chỉnh sửa gắn với phiên bản cụ thể và checklist hiện có. |
| Publish | Chỉ chuyển `PublishedVersionId` sang đúng current draft đã được duyệt. Kiểm tra lại nguồn tại thời điểm publish. |
| Unpublish | Ghi lý do và audit. Khi trạng thái mất hiệu lực công khai, hồ sơ biến mất khỏi danh sách, chi tiết và số đếm. |
| Restore | Dựng draft mới từ phiên bản đã chọn. Người đọc tiếp tục thấy phiên bản đang phát hành tới khi draft mới được duyệt và publish. |

**Hoàn thành khi** chạy được luồng CMS cho một hồ sơ thu hồi nước, từ tạo đến xuất bản, sửa và xuất bản lại. Request dùng ETag cũ bị từ chối và draft chưa duyệt không thể publish.

## Bước 6. Triển khai public API từ cùng tập hồ sơ đủ điều kiện

Xây một truy vấn dùng chung cho cả tổng quan, nhóm, danh sách và chi tiết. Từ article chính thức thuộc module, lấy version qua `PublishedVersionId`. Kiểm tra trạng thái Published, chưa archive, tiếng Anh, snapshot hợp lệ và nguồn còn được chấp nhận theo điều kiện của `DiscoveryService.Publications`. Lọc và đếm bằng `ArticleVersion.CategorySlug`. Trả trường kỹ thuật từ snapshot đang public.

| Method và route | Hành vi |
|---|---|
| `GET /api/science-technology` | Thông tin module và năm nhóm theo thứ tự đã chốt |
| `GET /api/science-technology/categories` | Slug, tên tiếng Anh, mô tả và số hồ sơ công khai trong từng nhóm |
| `GET /api/science-technology/technologies` | Danh sách phân trang theo convention hiện tại. Hỗ trợ `category`, `page`, `pageSize`. Thứ tự mặc định theo ngày xuất bản rồi ID để phân trang ổn định |
| `GET /api/science-technology/technologies/{slug}` | Hồ sơ chi tiết kèm nguồn, phiên bản và ngày publish. Slug không có hồ sơ đủ điều kiện trả 404 |
| `GET /api/science-technology/sources/{id}/revisions/{revisionId}` | Mở đúng revision được dẫn từ một hồ sơ đang công khai của module |

Nhóm hợp lệ chưa có bài trả 200 với danh sách rỗng. Kiểm tra quyền truy cập nguồn và media thông qua hồ sơ đủ điều kiện. Nếu có liên kết tới module khác, dùng URL công khai của đích và kiểm tra đích còn có thể mở. Kiểm tra các URL do snapshot/renderer tạo để chúng trỏ đúng module.

**Hoàn thành khi** người đọc đi được từ tổng quan đến hồ sơ rồi mở đúng nguồn. Thiết bị mô phỏng chưa có hồ sơ, draft, bài đã gỡ và nội dung ngoài module không xuất hiện qua bất kỳ đường đọc nào.

## Bước 7. Chuẩn bị dữ liệu cho bản phát hành

1. Làm trọn một hồ sơ thu hồi nước trước rồi bổ sung ít nhất một hồ sơ cho mỗi nhóm còn lại. Mỗi hồ sơ có tên và slug ổn định, công nghệ được định danh cụ thể, nguyên lý, khả năng, giới hạn, môi trường thử và nguồn sơ cấp người biên tập đã kiểm tra.
2. Với mỗi số liệu, ghi lại đoạn/bảng trong tài liệu nguồn và cấu hình áp dụng. Duyệt phần tiếng Anh theo đúng snapshot. Trường thiếu bằng chứng phải được sửa, lược bỏ hoặc mô tả đúng mức độ đề xuất trước khi phát hành.
3. Nếu dùng data pack như các module trước, đặt khóa ổn định theo slug và dấu hiệu nhận biết phiên bản nội dung. Import lần hai cùng dữ liệu phải an toàn. Thay đổi pack tạo draft để review, không tự thay version công khai. Thử lỗi giữa lần import và chạy lại.

**Hoàn thành khi** staging có ít nhất năm hồ sơ tiếng Anh đã duyệt, mỗi nhóm có ít nhất một hồ sơ và mọi trích dẫn mở được đúng revision.

## Bước 8. Kiểm thử và phát hành

| Ca kiểm thử | Kết quả nghiệm thu |
|---|---|
| V1 thuộc ISRU. Draft V2 đổi sang Robotics and AI đồng thời sửa giới hạn và nguồn | Trước publish, cùng slug trả toàn bộ V1 và vẫn đếm ở ISRU. Sau publish, cùng slug trả toàn bộ V2, nhóm cũ giảm một và nhóm mới tăng một |
| Thu hồi revision nguồn duy nhất của một số liệu trong hồ sơ | Theo cơ chế publication hiện có, hồ sơ bị loại khỏi danh sách, chi tiết và số đếm. Sau khi thay bằng revision hợp lệ, duyệt và publish lại, hồ sơ xuất hiện trở lại |
| Hai lần tạo hồ sơ đồng thời, hai lần publish cùng version hoặc sửa bằng ETag cũ | Một liên kết chính thức, một kết quả publish nhất quán, không mất bản cập nhật đã lưu |
| Nguồn định tính, fact có môi trường thử khác hồ sơ, fact thiếu đơn vị | Hồ sơ định tính publish được. Fact trả đúng môi trường riêng. Số liệu không đủ bối cảnh bị chặn khi duyệt |
| Ngôn ngữ sai, category ngoài danh mục, truy cập CMS bằng Viewer, đoán slug draft hoặc ID ngoài module | Validation, phân quyền và 404 đúng contract. Response không lộ draft hoặc nội dung module khác |
| Unpublish và restore | Gỡ hồ sơ cập nhật mọi đường đọc. Restore tạo draft phải qua duyệt trước khi công khai |
| Nâng cấp DB có dữ liệu cũ | Thiết bị Living vẫn dùng được trong mô phỏng. Snapshot, URL và API cũ của Discovery, Survival và Living tiếp tục đọc được |

Chạy HTTP tests qua app thật và các ca migration/concurrency với SQL Server fixture của repo. Chạy `dotnet test Backend/Mars_Survival_Hanbook.Tests` với cấu hình test database của dự án. Trên staging, áp migration, seed hai lần, nạp dữ liệu, duyệt, publish và kiểm tra các request công khai, quản trị, nguồn và số đếm. Bàn giao OpenAPI, request Postman mẫu và hướng dẫn seed/import theo lệnh đã triển khai.

Nếu một hồ sơ phát hành sai, dùng unpublish, restore phiên bản phù hợp thành draft rồi duyệt và publish. Nếu migration hoặc dữ liệu không đạt nghiệm thu trên staging, sửa và chạy lại từ database đại diện trước khi phát hành.

## Mốc bàn giao

| Mốc | Phụ thuộc | Kết quả kiểm chứng được |
|---|---|---|
| 1. Contract và nền dữ liệu | Bước 1–3 | Năm nhóm, một hồ sơ chính thức cho mỗi công nghệ, migration chạy được trên DB có sẵn |
| 2. Một hồ sơ chạy xuyên suốt | Bước 4–6 | Admin publish hồ sơ thu hồi nước và người đọc mở được URL ổn định, dữ kiện và nguồn. Ca đổi nhóm V1/V2 đạt |
| 3. MVP có thể phát hành | Bước 7–8 | Đủ dữ liệu năm nhóm, regression đạt, có OpenAPI và quy trình phát hành đã chạy trên staging |
