# Kế hoạch triển khai Human Survival

Human Survival giúp người đọc phổ thông hiểu cách con người sinh hoạt, phối hợp và chăm sóc nhau tại một căn cứ giả định trên bề mặt Sao Hỏa. Người đọc chọn một chủ đề, phân tích một tình huống, đọc lời giải thích rồi truy được bằng chứng cho nhận định mình muốn kiểm tra.

Phạm vi đề xuất cho bản đầu gồm sáu bài tiếng Anh đọc công khai, mỗi chủ đề một bài. Tình huống và câu hỏi tự suy nghĩ nằm ngay trong bài. Quiz có chấm điểm, lưu tiến độ và mô phỏng sức khỏe cá nhân thuộc các phiên bản sau nếu có nhu cầu học tập cụ thể. Tài liệu này mô tả công việc cần làm ở backend và dữ liệu, cùng hợp đồng để frontend dựng được trải nghiệm đọc. Các route, slug và lệnh CLI mới bên dưới là thiết kế đề xuất.

## Bước 1. Chốt sáu bài và kết quả học tập

| Chủ đề và category slug | Tình huống do nhóm biên tập xây dựng | Người đọc làm được gì sau bài học? |
|---|---|---|
| Physical Health · `physical-health` | Ca bảo trì kéo dài sang khoảng vận động và nghỉ của một thành viên. | Chỉ ra hoạt động bị ảnh hưởng và giải thích giới hạn khi áp dụng bằng chứng về trọng lực một phần vào lịch giả định. |
| Mental Health · `mental-health` | Một thành viên làm lệch ca cho biết họ ít có dịp trò chuyện với đội. | Phân biệt điều thành viên tự mô tả với suy đoán của đồng đội và đề xuất một câu hỏi để hiểu nhu cầu hỗ trợ. |
| Nutrition · `nutrition` | Kho còn thực phẩm nhưng ca tối thiếu thời gian và thiết bị để chuẩn bị một số món. | Xác định thông tin cần có ngoài lượng hàng tồn trước khi điều chỉnh thực đơn. |
| Medical Care · `medical-care` | Một vấn đề sức khỏe được báo gần cuối ca và cần bàn giao cho người phụ trách tiếp theo. | Lần theo luồng báo cáo và nhận ra thông tin bị bỏ sót. Bài tập trung vào tổ chức chăm sóc. |
| Teamwork · `teamwork` | Lời bàn giao “Filter work complete” bỏ sót việc kiểm tra sau khi thay bộ lọc. | Viết lại bàn giao để ca sau biết việc đã làm, bước chưa kiểm tra và người nhận việc. |
| Adapting to Isolation · `adapting-to-isolation` | Ca ngày muốn họp tại phòng chung khi ca đêm cần nghỉ. | So sánh hai cách bố trí, chỉ ra người chịu ảnh hưởng và cách đánh giá lại thỏa thuận. |

Mỗi bài có một nhóm chính. Khi chọn giữa ba nhóm dễ giao nhau, lấy câu hỏi cuối bài làm căn cứ. Mental Health theo trải nghiệm cá nhân, Teamwork theo việc phối hợp hoàn thành một nhiệm vụ, Adapting to Isolation theo thỏa thuận sống chung qua nhiều ngày. Chốt tên, thứ tự, mục tiêu và câu hỏi trước khi seed để URL và phân nhóm ổn định.

## Bước 2. Lập danh mục nguồn cho từng bài

| Bài | Điểm bắt đầu | Việc phải làm trước khi đưa vào bản phát hành |
|---|---|---|
| Physical Health | NTRS [20210019591](https://ntrs.nasa.gov/citations/20210019591) và bài `partial-gravity-and-human-health` trong [gói Survival](../Backend/Mars_Survival_Hanbook/Data/Survival/release.en.json). | Kiểm tra revision trên DB đích, ghi trang hoặc mục chứa kết luận và giới hạn. Tìm nguồn riêng nếu bài đưa khuyến nghị về thời lượng hoặc cường độ tập. |
| Mental Health | Nhóm Behavioral Medicine trong [HRR Evidence](https://humanresearchroadmap.nasa.gov/Evidence/). | Chọn báo cáo phù hợp với nhận định về áp lực hoặc hỗ trợ, ghi môi trường nghiên cứu và phạm vi diễn giải. |
| Nutrition | [HRR Food and Nutrition](https://humanresearchroadmap.nasa.gov/Risks/risk.aspx?i=176) và bối cảnh kho thực phẩm của [Living on Mars](Living_On_Mars_Backend.md). | Ghi nguồn cho nhận định về thực phẩm. Đánh dấu số lượng hàng tồn do tác giả đặt ra là giả định tình huống. |
| Medical Care | Nhóm Exploration Medical Capability trong [HRR Evidence](https://humanresearchroadmap.nasa.gov/Evidence/). | Chọn tài liệu có nội dung cụ thể về tổ chức hỗ trợ y tế. Đối chiếu từng bước nếu bài mô tả một quy trình thực tế. |
| Teamwork | Báo cáo về cooperation, coordination và communication trong [HRR Evidence](https://humanresearchroadmap.nasa.gov/Evidence/). | Tìm đoạn hỗ trợ phần giải thích về phối hợp. Ghi rõ hội thoại giao ca do tác giả xây dựng. |
| Adapting to Isolation | Nghiên cứu HERA hoặc CHAPEA qua [HRR Explore](https://humanresearchroadmap.nasa.gov/Explore/). | Chọn báo cáo có kết quả phù hợp. Ghi địa điểm mô phỏng, điều kiện, thời gian và giới hạn khi suy rộng sang Sao Hỏa. |

HRR dùng để tìm tài liệu theo chủ đề. Với hồ sơ NTRS, dùng connector hiện có qua `GET /api/citations/search` và `GET /api/citations/{id}`. Đọc bản báo cáo trước khi chọn nhận định. Với tài liệu chỉ có URL chính thức, dùng luồng nguồn thủ công của CMS. Frontend lấy bài đã xuất bản từ backend. [Connector hiện tại](../Backend/Mars_Survival_Hanbook/Integrations/Nasa/NasaConnectorClient.cs) · [NTRS OpenAPI](https://ntrs.nasa.gov/api/openapi/) · [Luồng nguồn dùng chung](Survival_Knowledge_API.md)

**Đầu ra** là bảng đối chiếu từng nhận định với tài liệu, tác giả, NTRS ID hoặc URL, trang hoặc mục, bối cảnh bằng chứng và giới hạn áp dụng. Sau khi nhập, ghi revision ID của DB đích. Các trang danh mục HRR ở bảng trên là đầu mối để tìm báo cáo. Tình trạng phê duyệt từng nguồn cần kiểm tra trên DB triển khai.

## Bước 3. Dựng hai bài mẫu và chốt hợp đồng dữ liệu

Viết hoàn chỉnh Physical Health và Teamwork trước. Bài đầu kiểm tra khả năng đi từ nhận định đến nguồn và sang bài Survival Knowledge liên quan. Bài thứ hai kiểm tra người đọc có thể tự viết lời bàn giao trước khi xem lời giải hay không.

Dùng article, version và các section có thứ tự hiện tại. Thử các anchor `scenario`, `question`, `explanation` và `evidence-limits`. Phần tình huống ghi rõ bối cảnh giả định. Nhận định khoa học gắn với revision và vị trí trong nguồn. Bài định tính có thể có danh sách số liệu rỗng. Dùng bài liên quan của Living cho tài nguyên, Survival Knowledge cho môi trường và Science and Technology cho thiết bị khi chúng giúp trả lời câu hỏi của bài.

**Hoàn thành khi** hai mẫu thể hiện được bằng draft và response mẫu, câu hỏi xuất hiện trước lời giải, người đọc truy được nhận định đến đúng nguồn. Đối chiếu với ArticleDraftRequest, SnapshotService và quy tắc validation trước khi quyết định bổ sung trường. Nếu section và claim hiện có đáp ứng được hai mẫu, dùng schema hiện tại. Nếu thiếu liên kết cần thiết, ghi rõ trường thiếu, cách đọc version cũ và migration cần có trước khi sửa model.

## Bước 4. Đăng ký module và seed danh mục

1. Thêm `human-survival` vào [ContentModule](../Backend/Mars_Survival_Hanbook/Services/ContentModule.cs). Gắn metadata nhận diện cho cả controller đọc và quản trị. Endpoint thiếu metadata hiện có thể rơi về Discover Mars.
2. Thêm sáu nhóm trong [DiscoverySeed](../Backend/Mars_Survival_Hanbook/Data/DiscoverySeed.cs), dùng khóa module + category slug, cập nhật tên, mô tả và thứ tự khi seed lại. Cho phép nhóm rỗng khi mới triển khai.
3. Áp quy tắc tiếng Anh cho truy vấn, tạo draft, import và restore. Mặc định ngôn ngữ là `en`, trả lỗi validation khi client yêu cầu ngôn ngữ chưa hỗ trợ. Rà soát các nhánh theo module ở tầng service và snapshot.

**Nghiệm thu** bằng cách chạy seed hai lần, kiểm tra đúng sáu nhóm theo thứ tự và thử đọc hoặc sửa một article ID của module khác qua route Human Survival.

## Bước 5. Nối vào CMS và quy trình duyệt

Dùng ArticleService, SnapshotService và ReviewService cho chuỗi tạo draft → preview → gửi duyệt → yêu cầu sửa hoặc chấp thuận → xuất bản. Mở các route article và review dưới `/api/admin/human-survival`. Tiếp tục dùng endpoint nguồn và fact chung của CMS. Giữ cơ chế phân quyền, ETag và lịch sử phiên bản theo [quy trình Survival Knowledge](Survival_Knowledge_API.md).

Khi preview, hiện đủ thứ tự section, claim, vị trí nguồn và nhãn tình huống giả định. Người duyệt đối chiếu bằng chứng với từng nhận định và phạm vi chuyên môn của bài, nhất là nội dung y tế và tinh thần. Bản sửa đi qua một version mới. Preview và trang công khai phải đọc đúng snapshot tương ứng.

**Nghiệm thu** bằng một bài được yêu cầu sửa rồi duyệt lại. Trong lúc chỉnh, bản đã xuất bản vẫn đọc được. Thử cập nhật với ETag cũ, thao tác thiếu quyền, xuất bản bản chưa duyệt và truy cập review của module khác.

## Bước 6. Nhập bộ sáu bài và nguồn vào database

Tạo gói dự kiến `Data/HumanSurvival/release.en.json` và lệnh `import-human-survival-pack` trong CLI backend. Tham chiếu nguồn bằng khóa ổn định của gói và NTRS ID hoặc URL để triển khai được trên DB mới. Khi nhập, ánh xạ sang revision cục bộ và in ID bài, version, revision cùng trạng thái cần duyệt. Đối chiếu cơ chế import của [Science and Technology](Science_And_Technology_Runbook.md) trước khi viết logic mới.

Import tạo hoặc tái sử dụng nguồn và draft theo khóa và nội dung. Gói cũ chạy lại phải giữ ID, thay đổi nội dung tạo version mới, draft đã sửa trong CMS cần được bảo toàn. Nếu mất kết nối NASA giữa chừng, báo chính xác mục lỗi và cho chạy lại từ trạng thái đã lưu. Duyệt nguồn, fact nếu có, rồi duyệt và xuất bản từng version đủ điều kiện bằng CMS.

**Nghiệm thu** trên DB sạch và DB đã có nguồn NTRS dùng chung. Chạy gói hai lần, sửa một bài, thử lỗi tải nguồn rồi chạy lại. Kiểm tra số bài, số phiên bản, tính toàn vẹn của trích dẫn và khả năng phục hồi.

## Bước 7. Mở API đọc và bàn giao luồng frontend

| Route GET đề xuất, dưới `/api/human-survival` | Kết quả cần hỗ trợ |
|---|---|
| `/overview` | Tên module và sáu nhóm theo thứ tự, mô tả và số bài đang công khai. Với nhóm có một bài, cung cấp slug của bài đó nếu frontend mở thẳng từ tổng quan. |
| `/articles?category=teamwork&page=1&pageSize=20` | Thẻ bài có slug, tiêu đề, tóm tắt, nhóm. Dùng phân trang hiện có với items, page, pageSize và total. |
| `/articles/{slug}` | Snapshot công khai có section theo thứ tự, claim gắn nguồn, locator, danh sách nguồn và liên kết đọc thêm đủ điều kiện. |
| `/sources/{sourceId}/revisions/{revisionId}` | Thông tin đúng phiên bản nguồn mà bài công khai trong module sử dụng. |

Triển khai qua dịch vụ đọc hiện có, kiểm tra điều kiện hiển thị ở overview, danh sách, chi tiết và nguồn. Khi thu hồi nguồn bắt buộc, loại bài phụ thuộc khỏi các kết quả công khai và cập nhật số đếm. Khi chỉ bài đọc thêm bị rút, ẩn riêng liên kết đó. Public API phục vụ dữ liệu cục bộ ngay cả khi dịch vụ NASA gián đoạn.

**Nghiệm thu frontend** bằng luồng tổng quan → bài bàn giao ca → câu hỏi → lời giải → nguồn. Thử thêm nhóm rỗng, slug sai, bài đã rút và link sang bài trọng lực. Bàn giao OpenAPI, response mẫu và Postman collection theo quy ước của repo.

## Bước 8. Nghiệm thu tiếng Anh trước khi xuất bản

Tiếng Anh (`en`) là ngôn ngữ phát hành của Human Survival. Áp dụng bước này khi duyệt sáu bài đầu tiên và mỗi phiên bản được sửa, nhập lại hoặc khôi phục.

1. Kiểm tra hành vi API theo bước 4. Khi bỏ qua tham số ngôn ngữ, API đọc dùng `en` và draft mới nhận `languageCode = en`. Thử gửi `vi` ở truy vấn đọc và body tạo draft, kiểm tra cả hai trả HTTP 400. Kiểm tra luồng import và restore cũng áp dụng quy tắc này.
2. Kiểm tra tên và mô tả module, sáu danh mục và các trường hiển thị trên thẻ bài. Trong preview của từng version, người duyệt đọc tiêu đề, tóm tắt, tình huống, câu hỏi, lời giải, giới hạn bằng chứng, chú thích ảnh, alt text và ghi chú nguồn nếu có. Các đoạn do nhóm biên tập viết phải bằng tiếng Anh. Giữ nguyên tên riêng, mã tài liệu, URL và credit gốc của nguồn.
3. Thử một draft gắn `en` nhưng có câu hỏi bằng tiếng Việt. Người duyệt yêu cầu sửa và ghi rõ trường cần sửa. Sau khi chỉnh, duyệt lại đúng version mới trước khi xuất bản.

**Nghiệm thu** bằng kết quả kiểm thử API và ghi nhận duyệt ngôn ngữ theo version. Sau khi xuất bản trên môi trường nghiệm thu, đối chiếu tổng quan, thẻ bài và chi tiết của cả sáu bài với bản tiếng Anh đã duyệt.

## Bước 9. Kiểm thử tích hợp và phát hành

1. Chạy test HTTP cho public và admin, đặc biệt ở ranh giới module, trạng thái draft/published, ngôn ngữ và thu hồi nguồn. Kiểm tra hai request chỉnh cùng version, import lặp và bản ghi cũ của Survival Knowledge vẫn mở được.
2. Từ thư mục gốc chạy `dotnet test Backend/Mars_Survival_Hanbook.Tests/Mars_Survival_Hanbook.Tests.csproj`. Chạy thử seed, import, duyệt và đọc qua API trên SQL Server nghiệm thu. Ghi nhận lỗi môi trường riêng với lỗi chức năng.
3. Kiểm tra cả sáu bài, câu hỏi học tập và trích dẫn trên response thực. Ngắt kết nối ra NASA sau khi nhập để kiểm tra luồng đọc cục bộ. Lưu ví dụ response và hướng dẫn cấu hình DB, raw storage, seed, import, duyệt, publish, unpublish theo convention của repo.
4. Khi một bài cần sửa sau phát hành, rút bài hoặc khôi phục nội dung của version trước thành draft mới, duyệt và xuất bản lại. Kiểm tra lại URL bài, số đếm, nguồn và các liên kết chéo.

**Điều kiện hoàn tất MVP** là có sáu bài tiếng Anh đã qua bước 8 và công khai đúng sáu nhóm trên môi trường nghiệm thu, từng nhận định khoa học truy được tới nguồn đã duyệt, người đọc hoàn thành hai luồng bài mẫu và biên tập viên sửa, xuất bản lại hoặc rút bài qua CMS. Lưu bằng chứng bằng kết quả test và response thực của API.
