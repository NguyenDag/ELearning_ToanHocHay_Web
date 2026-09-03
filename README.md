# ELearning_ToanHocHay_Web

Frontend (ASP.NET Core MVC) của dự án ToanHocHay — tiêu thụ Backend API `ELearning_ToanHocHay`.

## Cấu hình khi chạy lần đầu

`appsettings.json` **không được commit** (`.gitignore`). Tạo lại từ bản mẫu:

```bash
cp appsettings.Example.json appsettings.json
```

Rồi sửa giá trị cho phù hợp.

### Các khoá cấu hình

| Khoá (`appsettings.json`) | Biến môi trường tương đương | Ý nghĩa | Mặc định |
|---|---|---|---|
| `Api:BaseUrl` | `Api__BaseUrl` | URL gốc Backend API (không kèm `/api`) | `http://103.98.152.182` |
| `Api:WebBaseUrl` | `Api__WebBaseUrl` | URL công khai của WebApp | `https://www.toanhochay.com` |
| `Session:IdleTimeoutMinutes` | `Session__IdleTimeoutMinutes` | Thời gian sống session (phút) | `60` |
| `Auth:CookieExpireDays` | `Auth__CookieExpireDays` | Số ngày giữ cookie đăng nhập | `7` |
| — | `ASPNETCORE_ENVIRONMENT` | `Development` / `Staging` / `Production` | `Production` |

**Thứ tự ưu tiên** (theo cơ chế cấu hình mặc định của .NET):
biến môi trường thật → `appsettings.{Environment}.json` → `appsettings.json` → mặc định trong code.

Dấu phân cấp trong biến môi trường là `__` (hai gạch dưới): `Api__BaseUrl` ↔ `Api:BaseUrl`.

> Không dùng file `.env` — frontend không có bí mật nào, và .NET đã tự đọc biến môi trường.
> Trên server/CI/Railway/Docker, set biến môi trường thật là đủ để đè `appsettings.json`.

App vẫn chạy được nếu thiếu `appsettings.json` (dùng giá trị mặc định trong code).

## Chạy

```bash
dotnet run
```

> ⚠️ Backend `AppSettings:BaseUrl` phải trỏ về URL của WebApp này để liên kết
> `/reset-password` trong email đặt lại mật khẩu hoạt động.
