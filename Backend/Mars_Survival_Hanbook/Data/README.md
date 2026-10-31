# EF Core migrations

Backend đã cấu hình `MarsDbContext` cho SQL Server, migration `InitialCreate` và design-time factory để dùng được cả Visual Studio Package Manager Console lẫn .NET CLI.

Kết nối mặc định trong `appsettings.json`:

- Server: `(localdb)\MSSQLLocalDB`
- Database: `MarsSurvivalHandbook`
- Authentication: Windows / Trusted Connection
- Không cần username hoặc password.

Ứng dụng không tự tạo hay migrate database khi khởi động. Chạy một trong hai cách dưới đây để áp dụng migration và tạo database nếu chưa tồn tại.

## Visual Studio

Mở solution `Mars_Survival_Hanbook.slnx`. Trong **Tools → NuGet Package Manager → Package Manager Console**, chạy:

```powershell
Update-Database -Context MarsDbContext -Project Mars_Survival_Hanbook -StartupProject Mars_Survival_Hanbook
```

Package `Microsoft.EntityFrameworkCore.Tools` đã được thêm vào project. Nếu Visual Studio chưa nhận lệnh sau khi mở project đang có sẵn, restore NuGet rồi mở lại Package Manager Console.

## Terminal / PowerShell

Chạy từ thư mục backend:

```powershell
cd D:\Mar_Survival\Backend\Mars_Survival_Hanbook
dotnet tool restore
dotnet ef database update --context MarsDbContext
```

Repository có `dotnet-tools.json` ở thư mục gốc, khóa `dotnet-ef` tại phiên bản `8.0.33`, cùng phiên bản EF Core trong project. Không cần thay đổi tool toàn cục.

Nếu chạy từ thư mục gốc repository, chỉ rõ project và startup project:

```powershell
dotnet ef database update --context MarsDbContext --project Backend/Mars_Survival_Hanbook --startup-project Backend/Mars_Survival_Hanbook
```

Sau khi cập nhật, trong SSMS hoặc SQL Server Object Explorer, kết nối `(localdb)\MSSQLLocalDB` bằng Windows Authentication. Database có các bảng của model và `__EFMigrationsHistory`.

## Khi sửa model

Chạy từ thư mục backend, thay `DescribeYourChange` bằng tên thay đổi:

```powershell
dotnet ef migrations add DescribeYourChange --context MarsDbContext --output-dir Data/Migrations
dotnet ef database update --context MarsDbContext
```

Tương đương trong Package Manager Console:

```powershell
Add-Migration DescribeYourChange -Context MarsDbContext -OutputDir Data/Migrations -Project Mars_Survival_Hanbook -StartupProject Mars_Survival_Hanbook
Update-Database -Context MarsDbContext -Project Mars_Survival_Hanbook -StartupProject Mars_Survival_Hanbook
```

Không cần tạo lại `InitialCreate`. Kiểm tra migration được sinh ra trước khi update database đang có dữ liệu. Dùng migrations để quản lý schema; không gọi `EnsureCreated` trên database ứng dụng.

## Cấu hình môi trường khác

Runtime và design-time đọc cùng khóa `ConnectionStrings:MarsDatabase`. Có thể ghi đè qua `appsettings.{Environment}.json`, user secrets khi ở Development, hoặc biến môi trường `ConnectionStrings__MarsDatabase`. Không đưa mật khẩu SQL Server vào source control.

LocalDB chỉ dùng trên Windows; khi chạy backend trong Linux Docker, cấu hình connection string tới SQL Server có thể truy cập từ container.

`Data/Migrations` chứa migration và model snapshot, cần được lưu cùng source code. Factory chỉ khởi tạo context; nó không tự kết nối hay cập nhật database.
