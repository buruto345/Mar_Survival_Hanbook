# Kế hoạch triển khai backend Living on Mars

**Mục tiêu sản phẩm.** Người dùng cấu hình một căn cứ, chạy mô phỏng qua nhiều sol (ngày Sao Hỏa) và biết nhu cầu nào thiếu trước, thiếu bao nhiêu, vì sao. Họ có thể đổi một quyết định và chạy lại để so sánh. Sáu chủ đề gồm nhà ở, nước và oxy, thực phẩm, năng lượng, vệ sinh và chất thải, sinh hoạt cộng đồng.

**Phạm vi MVP đề xuất.** Một căn cứ mẫu, môi trường cố định, mô phỏng tất định theo sol, preview công khai. Người dùng chỉnh số người, dự trữ, thiết bị, ưu tiên điện và phân bổ lao động. Backend lưu bộ kịch bản đã phát hành; mỗi lượt tính trả kết quả trực tiếp. Tiếng Anh là ngôn ngữ bắt buộc cho toàn bộ văn bản do API Living on Mars trả về, kể cả CMS, preview và lỗi. Đây là kế hoạch triển khai, các route, model và lệnh phát hành mới bên dưới là đề xuất.

## Bước 1. Chốt luật của kịch bản trước khi tạo migration

1. Viết một fixture nhỏ cho vòng nước, chứa tồn đầu, công suất máy, điện và nhu cầu của đoàn. Dùng ca tính tay ở bước 5 làm đáp án.
2. Chốt biên của một sol. Thiết bị hoàn thành xây dựng trong sol bắt đầu hoạt động ở sol kế tiếp. Nước tái chế từ tồn nước thải đầu sol dùng được ngay. Nước thải mới thu gom chờ đến sol sau. Ghi rõ thời điểm thu hoạch và thời điểm lương thực khả dụng.
3. Chốt những lựa chọn hợp lệ. Dữ liệu âm, đơn vị sai loại hoặc ID thiết bị ngoài kịch bản là lỗi đầu vào. Căn cứ hợp lệ nhưng thiếu tài nguyên vẫn chạy hết thời hạn và báo thiếu hụt. Hết dự trữ và thiếu nhu cầu có hai mã riêng.
4. Chốt giới hạn số sol, quy mô đoàn, số thiết bị và độ lớn response từ một phép đo hiệu năng trước khi mở endpoint công khai. Trong MVP, kết quả thể hiện cân bằng năng lượng cả sol; đánh giá mất điện ban đêm cần bước thời gian ngắn hơn.

**Hoàn thành khi** hai người có thể dùng cùng fixture, cùng thứ tự xử lý và tính tay ra cùng kết quả mà không phải đoán quy tắc.

## Bước 2. Tạo module và tái sử dụng CMS

### 2.1. Thiết lập hợp đồng tiếng Anh xuyên suốt feature

1. **Đầu vào và truy vấn.** Quy ước mã ngôn ngữ của module là `en`. Qua các route public và admin, bỏ trống `language` hoặc `languageCode` thì mặc định `en`; truyền mã khác trả 400 với thông báo tiếng Anh. Header `Accept-Language` không đổi ngôn ngữ response. Áp dụng bộ lọc cả cho list, count, search, detail theo ID, version, preview và dữ liệu liên kết. Bản tiếng Anh chưa có thì list bỏ qua, detail trả 404 bằng tiếng Anh. Giới hạn thay đổi ở module Living on Mars.
2. **Seed, import và dữ liệu dùng chung.** Viết bằng tiếng Anh tên module, sáu category, nội dung bài, tên kịch bản, tài nguyên, thiết bị, hoạt động, đơn vị hiển thị, giả định và chú thích. Rà soát cả caption, alt text, tiêu đề nguồn và mô tả địa điểm mà serializer đưa vào DTO. Với metadata NASA bằng ngôn ngữ khác, giữ bản gốc trong kho nguồn nội bộ, tạo trường hiển thị tiếng Anh đã duyệt cho Living on Mars và liên kết tới revision gốc. Giữ nguyên tên riêng, tác giả, ID, URL, số liệu và ký hiệu khoa học để truy nguyên được.
3. **Duyệt văn bản trước khi trả về.** Gắn `LanguageCode = en` cho nội dung và scenario pack có phiên bản. Người biên tập kiểm tra văn bản thực tế, kể cả text trộn ngôn ngữ trong bản ghi đã gắn nhãn `en`. Lệnh publish phải kiểm tra đủ trường tiếng Anh và trạng thái duyệt của toàn bộ text được đưa vào snapshot. Áp dụng lại khi restore, tái phát hành hoặc thay nguồn. Với draft/import chưa xác minh, CMS có thể trả ID, trạng thái và danh sách field cần sửa bằng tiếng Anh; tránh phản chiếu nguyên văn text chưa kiểm tra trong response và lỗi validation.
4. **Chuẩn hóa response.** Đặt `Content-Language: en` trên response có nội dung. Dùng chung danh mục thông báo tiếng Anh cho kết quả, cảnh báo và lỗi từ model binding, validation, xác thực/phân quyền, conflict và exception handler. Ví dụ `InsufficientPower` đi kèm `"The water recycler received 2 of the 4 kWh it requested."` với số liệu lấy từ engine. Map lỗi từ NASA, DB và framework sang mã ổn định cùng thông báo tiếng Anh; giữ chi tiết kỹ thuật gốc trong log. Trả số và thời điểm dưới dạng JSON number và chuỗi ISO 8601 để locale máy chủ không đổi cách diễn giải.

**Đầu ra của bước 2.1.** Lập danh sách các field văn bản của từng response DTO và nơi cung cấp bản tiếng Anh cho từng field (snapshot đã duyệt, danh mục thông báo hoặc metadata hiển thị đã xác minh). Bổ sung field mới vào danh sách này khi mở rộng sáu chủ đề hoặc thêm API lưu phiên.

| Thành phần hiện có | Công việc |
|---|---|
| [ContentModule](../Backend/Mars_Survival_Hanbook/Services/ContentModule.cs), [Program](../Backend/Mars_Survival_Hanbook/Program.cs) | Đăng ký slug `living-on-mars`, khai báo metadata module trên controller và cập nhật resolver. Hiện endpoint thiếu `SurvivalKnowledgeAttribute` được suy ra là Discover Mars. Kiểm tra cả nơi tạo URL và luồng CLI không có HTTP context. |
| `Module`, `Category`, `Article`, `ArticleSection`, `ArticleVersion` | Seed module và sáu category theo slug, có thứ tự ổn định. Dùng lại bài Survival Knowledge liên quan qua liên kết đến bài đã xuất bản. Seed chạy lại phải giữ ID. |
| `ArticleService`, `ReviewService`, `SnapshotService`, `DiscoveryService` | Dùng lại luồng draft, duyệt, publish và đọc phiên bản công khai. Kiểm tra bộ lọc module cho list, detail, review, media và revision. Chỉ mở rộng snapshot/DTO nếu bài cần trả thêm quan hệ thiết bị, tài nguyên hoặc nguy cơ. |
| `Technology`, `Resource`, `Hazard`, `TechnologyResource` | Tái sử dụng danh mục và quan hệ input/output. Công suất, định mức và hiệu suất phụ thuộc cấu hình kịch bản, đặt trong bộ quy tắc có phiên bản ở bước 3. |

**Nghiệm thu.** Seed hai lần vẫn có đúng sáu category trong module mới. Anonymous chỉ đọc bản đã publish. Thay ID hoặc slug bằng nội dung của module khác không làm lộ bản nháp. Chạy regression cho Discover Mars và Survival Knowledge, kể cả URL trong snapshot cũ.

## Bước 3. Thiết kế dữ liệu kịch bản và trạng thái tính toán

Thêm model dưới `Backend/Mars_Survival_Hanbook/Models/Living`, mapping trong `Data/Configurations` và đăng ký ở [MarsDbContext](../Backend/Mars_Survival_Hanbook/Data/MarsDbContext.cs). Tên dưới đây mô tả trách nhiệm và field tối thiểu.

| Model lưu trong SQL Server | Field và ràng buộc chính |
|---|---|
| `LivingScenarioVersion` | `Id, ScenarioKey, Version, Title, LocationId?, DefaultCrewSize, DurationSols, Status, EngineVersion, PublishedAt?, RowVersion`. Unique theo `(ScenarioKey, Version)`. Publish cố định toàn bộ cấu hình con. |
| `ScenarioParameter` | `Id, ScenarioVersionId, Key, OriginalValue, OriginalUnitId, Basis, NormalizedValue, NormalizedUnitId, NormalizedBasis, EvidenceType, TestEnvironment, SourceRevisionId?, SourceLocator?, AssumptionNote?`. Unique theo version và key. Basis phân biệt mỗi người/24 giờ, mỗi người/sol, mỗi giờ máy, mỗi kg xử lý hoặc một lần. |
| `ScenarioResource` | `ScenarioVersionId, ResourceId, UnitId, InitialQuantity, Capacity?, MinInitial, MaxInitial`. Mỗi phiên bản định nghĩa các kho khả dụng, giới hạn và đơn vị. Phân biệt nước sạch, nước thải, oxy, thực phẩm ăn được, vật tư, chất thải rắn và điện lưu trữ. |
| `FacilityDefinition` | `Id, ScenarioVersionId, TechnologyId, DefaultCount, MaxCount, BuildCrewHours, BuildDurationSols, CrewCapacity, OperatingMode, ThroughputParameterId?, OperatorHoursParameterId?, DefaultPriority`. Tách thiết bị có sẵn từ sol đầu và thiết bị người dùng lên lịch xây. |
| `FacilityFlow` | `Id, FacilityDefinitionId, ResourceId, Direction, Phase, ParameterId`. Phase phân biệt xây dựng và vận hành. Ví dụ flow quy định kWh trên kg nước thải xử lý, kg nước sạch thu hồi trên kg nước thải. Kiểm tra basis và tham số cùng phiên bản với thiết bị. |
| Cấu hình cây trồng và hoạt động | Profile cây gồm `Key, ScenarioVersionId, GrowthSols, YieldPerM2ParameterId, WaterParameterId, PowerParameterId, MinInputFraction`. Hoạt động gồm `Key, ScenarioVersionId, DefaultCrewHours, MinCrewHours`. Có thể lưu dưới dạng cấu hình con của version nếu chưa cần truy vấn độc lập. |

Trong preview, dùng DTO cho cấu hình người dùng và trạng thái đang tính: `RunFacility` (số lượng, tiến độ, sol hoạt động), `CropBatch` (diện tích, sol gieo, tiến độ, trạng thái thu hoạch), `CrewActivityAllocation` (hoạt động, người-giờ) và `SolResourceBalance` (tồn đầu, vào, thực cấp, thải bỏ, thiếu, tồn cuối). Chỉ ghi vào DB khi phát triển chức năng lưu phiên ở bước 9.

**Nghiệm thu.** Migration chạy được trên DB mới và bản đã có hai module. Dùng `decimal` và thống nhất precision cho lượng tài nguyên. Từ chối công suất âm, hiệu suất thu hồi ngoài [0, 1], tồn đầu vượt sức chứa và tham chiếu chéo phiên bản. Chạy lại import cùng phiên bản phải giữ nguyên nội dung đã phát hành.

## Bước 4. Chuẩn bị nguồn NASA và phát hành bộ tham số

| Nguồn | Cách lấy và chuyển thành đầu vào |
|---|---|
| NASA NTRS | Dùng connector NTRS hiện có với `GET /api/citations/search` và `GET /api/citations/{id}`. Bắt đầu với BVAD record `20210024855` cho nhu cầu đoàn, nước, thực phẩm và chất thải. Metadata trỏ đến báo cáo; người biên tập trích giá trị, trang/bảng, điều kiện áp dụng và đơn vị. [Record](https://ntrs.nasa.gov/api/citations/20210024855) |
| PDS/MEDA | Dùng `GET /api/search/1/products/{lidvid}`, label và file sản phẩm. Parser trong repo hiện đọc ba trường áp suất BAROCAP. Khi bổ sung điện theo quan trắc, mở rộng parser cho kênh bức xạ phù hợp, lọc chất lượng, xử lý khoảng trống thời gian và tích phân W/m² theo thời gian trước khi áp dụng diện tích và hiệu suất. Ghi địa điểm, thời gian và revision cho profile. [Parser](../Backend/Mars_Survival_Hanbook/Integrations/Nasa/PdsMedaParser.cs) · [PDS API](https://nasa-pds.github.io/pds-api/guides/search/endpoints.html) |
| Mars Trek và SWIM | Trek WMTS cung cấp lớp bản đồ. SWIM hỗ trợ mô tả bằng chứng về băng ở địa điểm. Muốn tính kg nước khai thác mỗi sol cần thêm cấu hình thiết bị và giả định địa điểm. [Trek](https://trek.nasa.gov/tiles/apidoc/trekAPI.html?body=mars) · [SWIM](https://ntrs.nasa.gov/citations/20205008958) |
| CHAPEA và NASA Images | Dùng nguồn CHAPEA cho bố trí căn cứ, hoạt động và trồng cây trong môi trường analog. Dùng connector Images cho minh họa, giữ credit và điều kiện sử dụng. Các định mức thời gian do nhóm chọn được gắn nhãn giả định. [CHAPEA](https://www.nasa.gov/humans-in-space/chapea/about-chapea/) |

1. Dùng lại `NasaConnectorClient`, `ImportService`, `IngestionWorker`, `ImportedContentMapper`, `SourceService` và `RawStorage`. Kiểm tra trạng thái revision tại môi trường triển khai trước khi chọn dữ liệu. [Review nguồn đã có](NASA_Source_Review_2026-11-07.md)
2. Thêm `LivingScenarioService` để nạp, kiểm tra và đọc phiên bản công khai. Chuẩn hóa đơn vị, basis và thời lượng sol một lần lúc phát hành. Phân biệt `null` (chưa biết) với `0`. `Measurement` tiếp tục lưu quan trắc có instrument, địa điểm và thời gian.
3. Chuẩn bị một data pack có phiên bản và lệnh phát hành đề xuất `publish-living-scenario`, theo cơ chế command mode trong `Program.cs`. Kiểm tra toàn bộ tham số bắt buộc, revision được chấp nhận, locator hoặc lời giải thích giả định. Ghi version và cấu hình con trong cùng transaction. Nếu nguồn bị thu hồi, ngừng cung cấp bản bị ảnh hưởng cho preview mới và phát hành bản thay thế sau khi duyệt.

**Nghiệm thu.** Public API giải thích được xuất xứ từng định mức. Thiếu hiệu suất hoặc suất tiêu thụ điện của thiết bị đang bật phải chặn publish với tên field. Engine chạy được từ bản đã phát hành khi NASA gián đoạn. MVP dùng profile môi trường cố định; `EnvironmentProfileService` chỉ cần khi thêm chuỗi quan trắc.

## Bước 5. Xây engine và hoàn thiện vòng nước đầu tiên

Tạo `Services/Living/LivingSimulationService.cs`. Engine nhận cấu hình đã chuẩn hóa, phiên bản quy tắc và số sol, trả kết quả tất định. Tránh phụ thuộc HTTP, DbContext và đồng hồ hệ thống trong phép tính.

1. Mở sol với tồn kho, trạng thái pin, thiết bị khả dụng và ngân sách người-giờ. Tính điện phát, giới hạn xả pin và tải nền.
2. Xét thiết bị theo ưu tiên, dùng khóa ổn định để phân xử khi bằng nhau. Mỗi lần cấp phải cập nhật lượng còn lại trước khi xét thiết bị kế tiếp.
3. Tính lượng xử lý thực tế bằng giới hạn thấp nhất giữa công suất, nguyên liệu, điện, lao động và sức chứa đầu ra. Chế độ chạy liên tục cho phép chạy một phần. Thiết bị theo mẻ chỉ chạy khi đủ một mẻ; nếu MVP chỉ triển khai chế độ liên tục, bộ phát hành phải từ chối cấu hình theo mẻ.
4. Cấp nhu cầu cho đoàn từ các kho sử dụng được. Tính `unmet = requested - supplied` và `closing = opening + received - supplied - discarded` trên từng sổ kho, bao gồm cả đầu vào đã cấp cho thiết bị. Ghi rõ tỷ lệ thu gom chất thải, phần thất thoát và lượng điện dư.
5. Chốt sol, cập nhật xây dựng và cây trồng. Trả tồn kho, vận hành thiết bị, nguyên nhân giới hạn và thiếu hụt. Làm tròn ở đầu ra hiển thị.

**Fixture nghiệm thu đầu tiên.** Tồn đầu 8 kg nước sạch và 4 kg nước thải; máy có thể xử lý 4 kg/sol, dùng 1 kWh/kg, được cấp 2 kWh, thu hồi 75%. Sức chứa và nhân lực đủ. Đoàn yêu cầu 10 kg nước sạch. Kết quả phải xử lý 2 kg nước thải, tạo 1,5 kg nước sạch, ghi 0,5 kg không thu hồi, cấp 9,5 kg và thiếu 0,5 kg. Tồn sạch cuối bằng 0. Còn 2 kg nước thải cũ trước khi cộng nước thải mới thu gom. Nguyên nhân máy bị giới hạn là điện. Đây là số test.

## Bước 6. Mở contract HTTP cho lát cắt đầu tiên

Tạo controller Living, request/response trong `Contracts`, đăng ký service ở `Program.cs`. Dùng tiền tố `/api/living-on-mars` cho các route public.

| Method và route tương đối | Trách nhiệm |
|---|---|
| `GET /`, `GET /categories`, `GET /articles`, `GET /articles/{slug}` | Điều hướng sáu chủ đề và đọc nội dung đã xuất bản qua service hiện có. |
| `GET /scenarios` | Danh sách kịch bản khả dụng, version hiện hành và tóm tắt. |
| `GET /scenarios/{key}/versions/{version}` | Trả mặc định, giới hạn, đơn vị, thiết bị, quy tắc theo sol và tham chiếu nguồn đúng revision. |
| `POST /simulations/preview` | Nhận `scenarioKey, scenarioVersion, crewSize, durationSols, initialStocks, facilities, powerPriority, crewAllocations`. Dùng default của version cho field tùy chọn bị bỏ trống. |

Response preview cần có `engineVersion`, `scenarioVersion`, `firstShortage` (có thể null), `sols[]`. Mỗi sol trả balance theo tài nguyên, nhu cầu và thực cấp của đoàn, công suất yêu cầu/thực tế của thiết bị, `limitingFactors[]` và giá trị giới hạn kèm đơn vị. Khi điện thiếu, trả điện yêu cầu và thực cấp để client giải thích được nguyên nhân. Hai máy cùng tranh điện phải để lại dấu vết phân bổ.

**Nghiệm thu.** Fixture bước 5 qua HTTP có cùng số liệu như unit test. Thiếu tài nguyên trả 200 với kết quả đã tính. Input sai trả 400 theo định dạng lỗi hiện có, có field gây lỗi. Version không tồn tại hoặc chưa công khai trả 404. Xuất OpenAPI có ví dụ thành công và lỗi, chạy smoke test qua normal entry point của ứng dụng.

## Bước 7. Mở rộng cùng engine đến đủ sáu chủ đề

| Thứ tự | Phần triển khai | Ca nghiệm thu đặc trưng |
|---|---|---|
| 1 | Nước, oxy và vệ sinh. Tách nhu cầu uống, vệ sinh, oxy; khai báo đầu vào theo đúng công nghệ. Bổ sung bồn nước thải, sức chứa chất thải rắn và quy tắc thu gom. | Máy điện phân tiêu thụ cả nước và điện. Đoàn chỉ tạo nước thải theo mức dùng thực tế và tỷ lệ thu gom. Bồn đầy có lượng tràn hoặc lượng xử lý bị giới hạn. |
| 2 | Năng lượng. Thêm phát điện, tải nền, pin, giới hạn sạc/xả và ưu tiên thiết bị. | Đổi ưu tiên oxy và tái chế làm thay đổi lượng thực cấp đúng thiết bị. Tổng điện cấp không vượt điện phát cộng điện thực xả. |
| 3 | Nhà ở. Trừ vật tư và người-giờ đúng một lần theo tiến độ, cập nhật sức chứa và tải nền khi module hoạt động. | Công trình thiếu lao động chậm hoàn thành. Trước sol kích hoạt, công trình chưa tăng sức chứa. |
| 4 | Thực phẩm. Thêm khẩu phần dự trữ, lứa cây, diện tích, nước, điện, lao động và thời điểm thu hoạch. Chốt quy tắc tăng trưởng khi thiếu đầu vào trong version. | Gieo hôm nay chưa tăng kho thực phẩm. Một lứa chỉ thu hoạch một lần, năng suất dùng cùng cơ sở khối lượng với khẩu phần. |
| 5 | Sinh hoạt cộng đồng. Phân bổ ngân sách người-giờ cho ngủ, vệ sinh, bảo trì, trồng cây và sinh hoạt chung. | Tổng giờ phân bổ không vượt ngân sách. Tăng lao động xây dựng làm giảm thời gian khả dụng cho hoạt động khác; response nêu hoạt động bị thiếu giờ. |

**Hoàn thành khi** một kịch bản đã phát hành có lựa chọn và hệ quả quan sát được cho cả sáu chủ đề. Thử thay đúng một lựa chọn, giải thích chênh lệch bằng số liệu trả về.

## Bước 8. Kiểm thử tích hợp và bàn giao

- **Điều kiện phát hành về tiếng Anh.** Thêm integration test từ HTTP cho route public, CMS, preview và các lỗi có body. Chạy với request mặc định, `language=en`, `language=vi`, body có `languageCode=vi` và header `Accept-Language: vi-VN`. Đối chiếu status, header và nội dung từng field văn bản lồng nhau với fixture tiếng Anh đã duyệt. Bao phủ cả summary thiếu hụt, lý do thiết bị bị giới hạn, nguồn, media, lỗi DTO và lỗi phân quyền có body. Chạy lại với locale máy chủ `vi-VN`.
- **Ca chống lọt dữ liệu.** Seed một bản tiếng Việt, một bản gắn nhãn `en` nhưng còn câu tiếng Việt và một nguồn dùng chung thiếu tiêu đề hiển thị tiếng Anh. Xác minh list/detail theo ID, CMS preview, snapshot/restore và lỗi import đều tuân thủ bước 2.1; pack còn field chưa duyệt bị chặn publish. Kiểm tra nội dung bằng fixture và review biên tập; kiểm tra ASCII hoặc chỉ so sánh `languageCode` không phát hiện được mọi lỗi ngôn ngữ. Chạy các ca này trong CI trước khi phát hành hoặc thêm route mới.

- **Engine:** chạy fixture qua nhiều sol, kiểm tra không có tồn âm, bảo toàn dòng tài nguyên kể cả thải bỏ, thiếu hụt đầu tiên, pin đầy, máy hết đầu vào, thu hoạch và thời điểm hoàn thành nhà. Chạy lại cùng cấu hình phải cho cùng kết quả. Thử trường hợp hai giới hạn cùng chạm ngưỡng.
- **Dữ liệu và SQL Server:** test migration từ schema hiện tại, seed và publish lặp, rollback khi một flow sai, concurrent publish, version bất biến, thu hồi nguồn và cách ly module. Dùng fixture SQL Server trong `Backend/Mars_Survival_Hanbook.Tests`.
- **HTTP và quyền:** public chỉ đọc bản phát hành; thao tác CMS dùng Admin policy hiện có. Kiểm tra ID chéo module, dữ liệu vượt giới hạn, thứ tự sol và nguồn của tham số. Chạy regression cho hai module cũ. Dùng fixture NASA hoặc mock đang có cho test connector.
- **Release:** chạy `dotnet test Backend/Mars_Survival_Hanbook.Tests` với cấu hình SQL Server test của repo. Trên DB staging, áp migration, chạy seed, import/duyệt nguồn, publish scenario, rồi chạy preview mẫu qua HTTP. Ghi version kịch bản, engine, thời gian xử lý và kích thước response ở mức tải tối đa đã chốt.
- **Bàn giao:** cập nhật OpenAPI, request mẫu Postman, lệnh seed/publish và ví dụ giải thích thiếu hụt. Khi một pack lỗi, rút bản đó khỏi danh sách khả dụng và dùng lại bản trước còn đủ điều kiện nguồn.

## Bước 9. Mở rộng khi sản phẩm cần lưu hoặc điều hành từng sol

Thêm `LivingRun(Id, UserId, ScenarioVersionId, EngineVersion, CurrentSol, Status, RowVersion)`, trạng thái thiết bị/cây, allocation và balance theo sol. Mỗi quyết định cần gắn với sol có hiệu lực để tái hiện lịch sử. Bổ sung `LivingRunService` và route tạo, đọc, tiếp tục phiên sau đăng nhập. Kiểm tra quyền sở hữu; ghi quyết định, trạng thái kho và sol mới trong cùng transaction. Dùng khóa duy nhất theo run/sol và khóa idempotency cho lệnh chuyển sol. Hai lần gửi cùng lệnh phải trả cùng kết quả mà chỉ trừ tài nguyên một lần.

Mở rộng môi trường theo vị trí và mùa sau khi xác minh parser, độ phủ thời gian và chất lượng dữ liệu. Khi cần đánh giá tải ban đêm, thêm bước ngày/đêm, phát hành engine version mới và giữ thông tin phiên bản cho kết quả cũ.

**Mốc có thể demo:** sau bước 6 có vòng nước–điện chạy xuyên suốt qua API. **Mốc hoàn thành backend MVP:** sau bước 8 có một kịch bản đủ sáu chủ đề, truy nguyên được các tham số, chạy ổn định khi NASA gián đoạn và có bộ test hồi quy.
