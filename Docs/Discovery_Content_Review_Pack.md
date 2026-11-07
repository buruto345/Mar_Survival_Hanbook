# Discover Mars — 5 bản thảo chờ kiểm chứng

Chuẩn bị ngày 03/11/2026 để editor khởi tạo bài cho đủ 5 category. Đây là bản thảo biên tập, chưa phải quyết định khoa học. Cần lưu bằng chứng raw/locator, xác minh tác giả tổ chức và phân loại evidence cho từng claim trước submit. Các đoạn dưới được diễn đạt lại, không sao chép bài NASA.

| Category | Slug gợi ý | Nguồn và locator |
| --- | --- | --- |
| exploration-history | mariner-4-buoc-ngoat-kham-pha | [NASA Mariner 4](https://science.nasa.gov/mission/mariner-4/), mục mô tả flyby và In Depth |
| structure-and-terrain | cac-dang-dia-hinh-sao-hoa | [NASA Mars Facts](https://science.nasa.gov/mars/facts/), Formation/Structure/Surface |
| atmosphere-temperature-gravity | khi-quyen-mong-cua-sao-hoa | [NASA Mars Facts](https://science.nasa.gov/mars/facts/), Atmosphere |
| water-and-life | dau-vet-nuoc-tren-sao-hoa | [NASA Mars Facts](https://science.nasa.gov/mars/facts/), Water on Mars; [Perseverance](https://science.nasa.gov/mission/mars-2020-perseverance/), Landing Site/Objective |
| missions-and-rovers | perseverance-va-jezero | [NASA Perseverance](https://science.nasa.gov/mission/mars-2020-perseverance/), Landing Site/Science/Perseverance Lands on Mars |

## Mariner 4 và bước ngoặt quan sát Sao Hỏa

Mariner 4 thực hiện chuyến bay ngang Sao Hỏa năm 1965 và gửi về những ảnh chụp cận cảnh đầu tiên của hành tinh này. Những hình ảnh nhiều hố va chạm cùng phép đo qua tín hiệu vô tuyến giúp chuyển việc tìm hiểu Sao Hỏa sang đối chiếu bằng dữ liệu tàu vũ trụ. Phạm vi ảnh của một chuyến bay ngang không đại diện cho toàn bộ bề mặt.

Reviewer cần thống nhất múi giờ khi tạo timeline: trang NASA hiển thị ngày 14/7 theo giờ Mỹ và mô tả thời điểm cận điểm tương ứng ngày 15/7 UTC. Không tự đặt `00:00Z` cho ngày địa phương.

## Những dạng địa hình của Sao Hỏa

Sao Hỏa có lõi, lớp phủ đá và vỏ rắn. Bề mặt chứa hố va chạm, núi lửa và hệ thống hẻm vực; Olympus Mons và Valles Marineris là hai ví dụ nổi bật. Màu đỏ nhìn từ xa gắn với vật liệu chứa sắt bị oxy hóa và bụi.

Nếu thêm fact kích thước/độ cao, reviewer phải ghi rõ mốc đo. Map layer cần đủ hệ tọa độ, bbox, style, tile matrix và attribution.

## Khí quyển mỏng và nhiệt độ thay đổi

Khí quyển Sao Hỏa mỏng, với carbon dioxide là thành phần chủ yếu; nitrogen và argon cũng hiện diện. Bụi lơ lửng ảnh hưởng màu trời và có thể tạo những trận bão lớn. Nhiệt độ thay đổi theo vị trí, thời điểm và điều kiện quan sát, nên một giá trị đơn lẻ cần đi kèm bối cảnh.

Bài khởi đầu này chưa đưa số đo trọng lực hoặc áp suất. Muốn bổ sung, dùng nguồn thích hợp và fact có đơn vị/điều kiện. Không đổi kết quả Mars-GRAM `Modeled` thành phép đo `Observed`.

## Nước trong lịch sử Sao Hỏa

Các mạng thung lũng, châu thổ và trầm tích là những dấu vết được nghiên cứu để tái dựng lịch sử nước trên Sao Hỏa. Jezero là một địa điểm quan trọng cho nghiên cứu đó. Mục tiêu tìm dấu hiệu sự sống cổ cần được trình bày cùng giới hạn của bằng chứng và các cách giải thích khác.

Reviewer phải phân biệt bằng chứng môi trường từng có nước, khả năng từng thích hợp cho sự sống và kết luận phát hiện sự sống. Không tự nâng mức kết luận từ một dấu hiệu tiềm năng.

## Perseverance và nghiên cứu Jezero

Perseverance hạ cánh trên Sao Hỏa ngày 18/2/2021 và khảo sát Jezero. Các mục tiêu khoa học gồm tìm hiểu điều kiện có thể hỗ trợ sự sống trong quá khứ, tìm dấu hiệu vi sinh vật cổ, thu thập/lưu mẫu và chuẩn bị kiến thức cho thám hiểm tương lai. Lịch sử của hồ và châu thổ Jezero giúp định hướng các địa điểm khảo sát.

Khi tạo working mission, kiểm tra tình trạng nhiệm vụ tại thời điểm biên tập và ghi `statusVerifiedAt`. Trang NASA chứa ảnh có credit của nhiều tổ chức; phải đối chiếu từng ảnh, không gán toàn bộ credit thành NASA.

## Tạo bài qua API

Đăng ký nguồn bằng `/sources/manual` hoặc connector phù hợp, giữ metadata thật. Tạo draft với `sections`, `claims`, `sourceRevisionIds` đã nhận từ API; `locator` chỉ rõ phần tài liệu hỗ trợ từng claim. Không dùng GUID minh họa, checklist tự động hay notes mẫu để ghi nhận review. Quy trình và ETag xem [runbook](Discover_Mars_Implementation_Status.md).
