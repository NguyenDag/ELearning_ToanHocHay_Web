# ELearning_ToanHocHay_Web

Frontend (ASP.NET Core MVC) của dự án ToanHocHay — tiêu thụ Backend API `ELearning_ToanHocHay`.

## Cấu hình khi chạy lần đầu

Các file cấu hình chứa giá trị theo môi trường **không được commit** (`.gitignore`).
Tạo lại từ bản mẫu:

```bash
cp appsettings.Example.json appsettings.json
cp .env.example .env
```

Rồi sửa giá trị cho phù hợp.

### Các khoá cấu hình

| appsettings.json | .env (biến môi trường) | Ý nghĩa | Mặc định |
|---|---|---|---|
| `Api:BaseUrl` | `Api__BaseUrl` | URL gốc Backend API (không kèm `/api`) | `http://103.98.152.182` |
| `Api:WebBaseUrl` | `Api__WebBaseUrl` | URL công khai của WebApp | `https://www.toanhochay.com` |
| `Session:IdleTimeoutMinutes` | `Session__IdleTimeoutMinutes` | Thời gian sống session (phút) | `60` |
| `Auth:CookieExpireDays` | `Auth__CookieExpireDays` | Số ngày giữ cookie đăng nhập | `7` |
| — | `ASPNETCORE_ENVIRONMENT` | `Development` / `Staging` / `Production` | `Production` |

**Thứ tự ưu tiên:** biến môi trường thật của hệ điều hành/container → `.env` → `appsettings.json` → mặc định trong code.
Dấu phân cấp trong biến môi trường là `__` (hai gạch dưới): `Api__BaseUrl` ↔ `Api:BaseUrl`.

File `.env` được nạp tự động lúc khởi động (`Common/DotEnv.cs`, gọi trong `Program.cs`).

## Chạy

```bash
dotnet run
```

> ⚠️ Backend `AppSettings:BaseUrl` phải trỏ về URL của WebApp này để liên kết
> `/reset-password` trong email đặt lại mật khẩu hoạt động.
