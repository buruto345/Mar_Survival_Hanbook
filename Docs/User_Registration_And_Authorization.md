# Đăng ký và phân quyền Admin / Viewer

Cập nhật 05/11/2026. Hệ thống chỉ còn hai role `Admin` và `Viewer`. Tài liệu này thay thế các hướng dẫn phân quyền bốn role trước đây.

## API đăng ký

```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "anhkiet",
  "fullName": "Nguyễn Anh Kiệt",
  "email": "anhkiet@example.com",
  "password": "<mật khẩu từ 12 đến 128 ký tự>"
}
```

| Field | Quy tắc |
| --- | --- |
| `username` | Bắt buộc; 3–50 ký tự ASCII gồm chữ, số, `.`, `_`, `-`; bắt đầu bằng chữ hoặc số; duy nhất, không phân biệt hoa/thường |
| `fullName` | Bắt buộc; tối đa 150 ký tự; hỗ trợ Unicode; loại khoảng trắng đầu/cuối |
| `email` | Bắt buộc; email hợp lệ, tối đa 254 ký tự; loại khoảng trắng đầu/cuối; duy nhất, không phân biệt hoa/thường |
| `password` | Bắt buộc; 12–128 ký tự; không tự trim hoặc sửa mật khẩu; lưu bằng `PasswordHasher<User>` |

JSON dùng `fullName` thống nhất với property C# `FullName`. Đăng ký luôn tạo tài khoản `Viewer`, trạng thái `Active`. Body có `role`, `state`, `passwordHash` hoặc field ngoài DTO bị từ chối với HTTP 400. Không có API public tự cấp Admin.

Thành công trả **201 Created**, header `Location` trỏ tới `/api/auth/me`, body gồm `id`, `username`, `fullName`, `email`, `role`. Không trả mật khẩu/hash/token. Sau khi đăng ký, gọi login để nhận token.

Lỗi: `400` dữ liệu không hợp lệ; `409` username/email trùng; `429` vượt giới hạn đăng ký. Mặc định giới hạn 5 request/IP/phút; `Auth:RegistrationPermitLimit` cấu hình trong khoảng 1–1000. Unique indexes của database bảo vệ cả trường hợp đăng ký đồng thời.

## Đăng nhập và profile

`POST /api/auth/login` nhận `email` + `password` hoặc `username` + `password`. Chỉ truyền một trong hai trường `email` và `username`; thiếu cả hai hoặc truyền cả hai đều trả 400. Email/username được bỏ khoảng trắng đầu/cuối và so khớp không phân biệt hoa/thường; mật khẩu giữ nguyên. Request email cũ vẫn được hỗ trợ. Response chứa `accessToken`, `tokenType`, `expiresIn`, `profile`. Profile dùng cùng cấu trúc với response đăng ký, thay `displayName` cũ bằng `username`, `fullName`, `email`.

Ví dụ đăng nhập bằng username: `{"username":"mars_admin","password":"your-password"}`. Đăng nhập bằng email: `{"email":"admin@example.com","password":"your-password"}`. Sai thông tin đăng nhập, tài khoản bị khóa/vô hiệu hóa hoặc đã xóa mềm đều trả 401.

`GET /api/auth/me` cần `Authorization: Bearer <accessToken>` và trả profile của tài khoản đang đăng nhập. Admin và Viewer đều được gọi. Tài khoản bị khóa, lưu trữ hoặc đổi role sẽ không dùng được token cũ; cần đăng nhập lại sau thay đổi quyền.

## Phân quyền

| Thao tác | Viewer | Admin |
| --- | --- | --- |
| Xem nội dung công khai, tìm kiếm, bản đồ, ảnh đã xuất bản | Có | Có |
| Xem profile cá nhân | Có | Có |
| Truy cập `/api/admin/...`, xem bản nháp/raw/import jobs | Không | Có |
| Tạo/sửa nội dung, nguồn, tài nguyên, cấu hình connector | Không | Có |
| Import NASA, duyệt/thu hồi nguồn, nhận và quyết định review | Không | Có |
| Publish, unpublish, restore, archive | Không | Có |

Admin được quản lý nội dung do bất kỳ tài khoản nào tạo và được duyệt bài do chính mình tạo. Không còn yêu cầu một role ScientificReviewer riêng. Khi một Admin hoàn tất review đã được nhận, người quyết định thực tế được ghi vào task, phiên bản và audit.

Các kiểm tra nội dung khoa học, trạng thái workflow, snapshot bất biến và ETag vẫn áp dụng. Admin cần thực hiện đúng luồng submit → claim → decision → publish; quyền quản trị không tự tạo bằng chứng hay quyết định duyệt cho dữ liệu NASA. Guest vẫn đọc được các API public hiện có.

## Entity và migration

`User` chứa `Username`, `NormalizedUsername`, `FullName`, `Email`, `NormalizedEmail`, `PasswordHash`, `Role`, `State`, cùng các field hiện có như `PreferredLanguage`, `LastLoginAt`. Lớp cơ sở `Entity` tiếp tục quản lý `Id`, timestamps, archive và `RowVersion` cho mọi entity; các field tài khoản chỉ thuộc User.

Migration `20261104010741_AddUserRegistrationAndTwoRoles`:

- Rename cột `DisplayName` thành `FullName`, giữ nguyên dữ liệu.
- Thêm `Username`, `NormalizedUsername` và unique index cho `NormalizedUsername`.
- Tài khoản cũ nhận username `legacy_<Guid không dấu gạch>`, bảo đảm duy nhất và giữ nguyên User ID, email, password hash, trạng thái và mọi liên kết.
- Chuyển `RegisteredReader` → `Viewer`; `Editor`, `ScientificReviewer`, `Administrator` → `Admin`.
- Cập nhật CHECK constraint để database chỉ chấp nhận `Admin` hoặc `Viewer`.

Tài khoản import NASA tiếp tục bị deactivated; role được chuyển thành Admin phục vụ lệnh import cục bộ. Nó không trở thành tài khoản đăng nhập tương tác.

Áp dụng cho database khác:

```powershell
dotnet ef database update --project Backend/Mars_Survival_Hanbook
```

Rollback migration chuyển Admin thành Administrator và Viewer thành RegisteredReader; không thể suy ngược các role biên tập cũ sau khi đã gộp. Dùng backup nếu cần khôi phục chính xác trạng thái trước chuyển đổi.

## Tạo Admin cục bộ

Admin được tạo qua lệnh `provision` chỉ bắt buộc username và mật khẩu không rỗng; không áp dụng độ dài mật khẩu tối thiểu của đăng ký Viewer. Giới hạn lưu trữ/đăng nhập vẫn là username tối đa 50 ký tự, mật khẩu tối đa 1024 ký tự. Khi bỏ `--email` hoặc `--full-name`, các trường tương ứng được lưu `null` (bao gồm `NormalizedEmail`). Migration `AllowAdminWithoutProfile` cho phép các cột này nhận null và giữ unique index cho email có giá trị. Viewer vẫn phải có đầy đủ email/họ tên và mật khẩu theo quy tắc đăng ký. Các trường hệ thống như ID, role, state, password hash và thời gian tạo vẫn được sinh tự động.

Đăng ký public luôn là Viewer. Tạo Admin bằng lệnh quản trị sau, chạy tại thư mục gốc repository; thay thông tin tài khoản theo người sử dụng thực tế:

```powershell
$accountSecret = Read-Host 'Mật khẩu Admin' -AsSecureString
$accountCredential = New-Object System.Management.Automation.PSCredential('bootstrap', $accountSecret)
$env:MARS_BOOTSTRAP_PASSWORD = $accountCredential.GetNetworkCredential().Password
try {
    dotnet run --project Backend/Mars_Survival_Hanbook --no-launch-profile -- provision --username localadmin --full-name "Local Admin" --email admin@example.com --role Admin
} finally {
    Remove-Item Env:MARS_BOOTSTRAP_PASSWORD
}
```

Lệnh dùng chung validation và hashing với đăng ký. Nó từ chối tài khoản trùng và không ghi đè quyền/mật khẩu của tài khoản đã có. Có thể provision Viewer bằng `--role Viewer`; không nhận tên các role cũ.

## Kiểm thử

```powershell
.\scripts\test-discovery.ps1
```

Các test dùng database LocalDB riêng: đăng ký → login → me; Unicode FullName; mật khẩu được hash; email/username trùng không phân biệt hoa/thường; đăng ký đồng thời; thiếu field/password không hợp lệ; chặn body khai role/state/hash; rate limit; tài khoản bị khóa; Viewer bị chặn trên toàn bộ endpoint CMS; Admin tự tạo/duyệt/publish; migration giữ nguyên dữ liệu và liên kết cũ.

OpenAPI và collection sinh từ controllers nằm trong `postman/specs/discover-mars.openapi.json` và `postman/collections/Discover Mars API`. Collection có mẫu cho register/login/me; nhập dữ liệu thực phù hợp trước khi gửi.

Kết quả kiểm chứng ngày 05/11/2026 gồm 71 kiểm thử .NET đạt, build thành công với 0 cảnh báo và 0 lỗi, EF xác nhận model khớp migration. Bộ Authentication Smoke chạy trên backend thật với database LocalDB thử nghiệm riêng, đạt 6 request và 9 assertions. Các response lần lượt là 201 đăng ký, 200 đăng nhập, 200 profile, 403 khi Viewer tạo bài, 409 đăng ký trùng và 400 khi gửi role Admin trong body đăng ký.

Collection đầy đủ đã được xuất lại với 87 operations trên 70 paths. OpenAPI và cả hai collection đều qua kiểm tra cấu trúc. Chạy bộ đăng ký trên database thử nghiệm vì mỗi lượt tạo một tài khoản Viewer.

Migration đã được áp dụng vào database local `MarsSurvivalHandbook` ngày 04/11/2026. Database có 5 bài, 25 source revisions và 10 phiên bản bài sau cập nhật. Backup trước migration nằm tại `Backend/Mars_Survival_Hanbook/App_Data/backups/MarsSurvivalHandbook_before_auth_20261104_081648.bak`.
