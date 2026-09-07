# Kế hoạch cập nhật Frontend (ToanHocHay.WebApp) theo thay đổi Backend

> **Bản nội bộ.** Đối chiếu với backend `ELearning_ToanHocHay` sau khi hoàn thành 8 giai đoạn
> P0–P7 + follow-up + chuẩn hoá A5 + workflow hoàn tiền (P8).
> Nguồn: `docs/Ra-soat-API-va-ke-hoach-kiem-soat.md`, `docs/Luong-thanh-toan.md`,
> `docs/Luong-hoan-tien.md` của repo backend.
>
> Ngày lập: 2026-09-04

---

## 0. Tóm tắt — vì sao phải cập nhật

Backend đã thay đổi **breaking** trên 4 trục lớn, ảnh hưởng gần như mọi service của WebApp:

| # | Thay đổi xuyên suốt | Hệ quả cho WebApp |
|---|---|---|
| **A** | **Route đổi sang kebab-case số nhiều** (`/api/User` → `/api/users`, `/api/Subscription` → `/api/subscriptions`, `/api/ExerciseAttempts` → `/api/exercise-attempts`, `/api/AIHint` → `/api/ai-hints`, `/api/Package` → `/api/packages`, `/api/Payment` → `/api/payments`, `/api/Parent` → `/api/parents`, `/api/Student` → `/api/students`…) | **Toàn bộ** `*ApiService.cs` + các chỗ gọi `HttpClient` thẳng trong Controller đang trỏ route cũ → **404 hàng loạt** |
| **B** | **Mọi endpoint trả vỏ `ApiResponse<T>`** + **đúng mã HTTP** (404 / 403 / 409 / 400+Errors thay vì `200 + Success=false`) | Chỗ deserialize thẳng (không qua `ApiResponse<T>`) trả `null`; chỗ chỉ check `IsSuccessStatusCode` giờ ném ở 4xx; cần đọc `403` để hiện "nâng gói", `409` để hiện "đã tồn tại/đã nộp" |
| **C** | **`JsonStringEnumConverter` bật ở backend** — enum serialize thành **chuỗi** (`"Student"`, `"Premium"`) chứ không phải số | `_jsonOptions` trong các service **chỉ** đặt `PropertyNameCaseInsensitive` → deserialize enum **ném exception**. `LoginResponseDto.PackageType` (int) không còn khớp `PackageTier` (enum chuỗi) |
| **D** | **Access token rút còn 30 phút** (trước 1440) + **refresh token thật** + **SecurityStamp** vô hiệu hoá token cũ ngay khi đổi mật khẩu / bị khoá / đổi vai trò | Cookie auth 7 ngày nhưng JWT chết sau 30′ → **mọi call API 401 sau nửa tiếng**. WebApp **chưa có** cơ chế refresh |

Ngoài ra có **tính năng mới** cần luồng UI mới: **Guest xem bài giảng** (tầng nội dung `learn`),
**thông báo** (`notifications`), **hoàn tiền** (`refunds`), **hạn mức AI hint**, **liên kết phụ huynh
bằng mã/lời mời**, **chatbot có lịch sử + chuyển nhân viên**.

---

## 1. Việc nền tảng (làm TRƯỚC, chặn mọi luồng khác)

### 1.1. Chuẩn hoá tầng gọi API

**Vấn đề hiện tại:** mỗi service tự `new HttpClient` cấu hình khác nhau, tự ghép
`ApiConstant.apiBaseUrl + "/api/..."`, tự gắn `Bearer` từ `Session["Token"]`, mỗi nơi một
kiểu `_jsonOptions`. Rất khó sửa 40 chỗ.

**Đề xuất:**

1. Tạo `Services/Http/ApiClient.cs` — một wrapper duy nhất:
   - `BaseAddress = {apiBaseUrl}/api/` (đã có sẵn `finalApiUrl` trong `Program.cs`).
   - `JsonSerializerOptions` dùng chung: `PropertyNameCaseInsensitive = true` **+
     `new JsonStringEnumConverter()`** (khắc phục trục **C**).
   - `DelegatingHandler` (`AuthTokenHandler`) tự gắn `Authorization: Bearer {access token}`
     từ session, **tự refresh khi 401** (xem 1.3), tự đọc header `X-Correlation-ID` để log.
   - Helper `Task<ApiResult<T>> GetAsync<T>/PostAsync<T>(...)` luôn giải mã `ApiResponse<T>`
     và ánh xạ mã HTTP → `ApiResult { StatusCode, Success, Data, Message, Errors }`.
2. Đăng ký tất cả `*ApiService` qua `AddHttpClient<T>().AddHttpMessageHandler<AuthTokenHandler>()`.
3. Bỏ hết `ApiConstant.apiBaseUrl + "/api/..."` rải rác — chỉ truyền path tương đối.

> Nếu không muốn refactor lớn ngay: tối thiểu **phải** thêm `JsonStringEnumConverter` vào **mọi**
> `_jsonOptions` và sửa route (mục 1.2), nếu không app hỏng ngay khi deploy backend mới.

### 1.2. Bảng ánh xạ route (sửa ở tất cả service + controller gọi thẳng)

| Chỗ gọi hiện tại (WebApp) | Route mới đúng | File |
|---|---|---|
| `/api/auth/login`, `/register`, `/confirm-email` | *(giữ nguyên `api/auth`)* | `AuthApiService`, `AccountController` |
| `/api/auth/update-profile/{id}` | **`/api/users/update-profile/{id}`** | `AuthApiService.UpdateProfileAsync` |
| `/api/auth/change-password/{id}` | **`/api/auth/change-password`** (bỏ `{id}`, lấy từ token; body `ChangePasswordDto`) | `AuthApiService.ChangePasswordAsync` |
| `/api/auth/resend-confirmation-email` | **`/api/auth/resend-confirmation`** (body `{ email }`) | `AccountController.ResendConfirmationEmail` |
| `/api/user/{id}` | **`/api/users/{id}`** | `AuthApiService.GetProfileAsync` |
| `/api/Exercises`, `/api/exercises/{id}` | `/api/exercises` *(chỉ sửa hoa/thường + `/for-edit` nếu cần đáp án)* | `ExamApiService` |
| `/api/ExerciseAttempts/*` | **`/api/exercise-attempts/*`** | `ExamApiService` |
| `/api/AIHint` | **`/api/ai-hints`** (POST tạo hint) | `ExamApiService.GetAIHintAsync` |
| `/api/Subscription`, `/api/Subscription/status/{id}` | **`/api/subscriptions`**, `/api/subscriptions/status/{id}` | `SubscriptionApiService` |
| `/api/student/{id}/subscription/current` | **`/api/students/{id}/subscription/current`** | `SubscriptionApiService` |
| `/api/student/{id}/dashboard` | **`/api/students/{id}/dashboard/overview`** | `DashboardApiService`, `CourseApiService` |
| `/api/student/{id}/dashboard/chapter-score-comparison` \| `/ai-assessment` \| `/ai-roadmap` | **`/api/students/{id}/dashboard/...`** | `DashboardApiService` |
| `/api/students/dashboard-stats` (từ token) | *(mới — dùng thay cho các chỗ hard-code `studentId`)* | — |
| `/api/Package`, `/api/Package/{id}` | **`/api/packages`**, `/api/packages/{id}` (GET công khai) | `PackageApiService` |
| `/api/parent/{id}` | **`/api/parents/{id}`** | `ParentController` |
| `/api/StudentParent/connect` | **`/api/parents/link`** (body `LinkParentDto { Code, Relationship }`) | `StudentController.ConnectParent` |
| `student/{id}/parents` | **`/api/parents/{parentId}/children`** (đảo chiều — xem mục 7) | `StudentController.GetConnectedParentsAsync` |
| `/api/LessonProgress/student/{id}/completed` | **`/api/progress/versions/{courseVersionId}`** (đọc `NodeProgress`) | `CourseApiService` |
| `/api/LessonProgress/update-progress` | **`POST /api/progress/lessons/{nodeId}/complete`** (body `{ secondsViewed }`) | `CourseController.UpdateProgress` |
| `/api/Lesson/{id}`, `/api/Lesson/by-topic/{id}`, `Curriculum/*` | **`/api/learn/*`** + `/api/catalog/*` + `/api/courses/*` (xem mục 3) | `LessonApiService`, `CourseApiService` |
| `Chatbot/message`, `/quick-reply`, `/trigger` | *(giữ `api/chatbot`)* + endpoint mới (mục 9) | `ChatApiService` |
| `Sepay/IPN` (webhook, cấu hình ở cổng SePay) | **`/api/sepay/ipn`** — ⚠️ đổi URL trên dashboard SePay | *(devops)* |

> `LessonApiService` đang hard-code `https://localhost:7092/api/Lesson` — **xoá/viết lại toàn bộ**.

### 1.3. Refresh token (trục D — bắt buộc)

**Hiện tại:** login xong lưu `Session["Token"]` = access token; cookie auth 7 ngày. Sau 30′
access token hết hạn → mọi service nhận **401** → middleware trong `Program.cs` sign-out và
đá về `/Account/Login` (trải nghiệm: "đang học tự nhiên văng ra").

**Việc cần làm:**

1. `LoginResponseDto` (WebApp) thêm: `RefreshToken`, `RefreshTokenExpiration`, `TokenExpiration`.
   Đổi `int PackageType` → `PackageTier PackageTier` (enum `Free/Standard/Premium/Yearly`).
2. `AccountController.Login`: lưu thêm `Session["RefreshToken"]`, `Session["TokenExpiration"]`.
   Claim `PackageType` → đổi tên thành `PackageTier` mang giá trị chuỗi.
3. `AuthApiService` thêm:
   - `Task<TokenPairDto?> RefreshAsync(string refreshToken)` → `POST /api/auth/refresh-token`
     body `{ refreshToken }`. Trả 401 nghĩa là refresh token đã bị thu hồi/replay → **buộc
     đăng nhập lại**.
   - `Task LogoutAsync(string? refreshToken)` → `POST /api/auth/logout` body `{ refreshToken }`
     (để backend thu hồi). Gọi trong `AccountController.Logout` **trước khi** `SignOutAsync`.
4. `AuthTokenHandler` (DelegatingHandler): trước request kiểm tra `TokenExpiration`; sắp hết
   (còn < 2′) hoặc nhận `401` lần đầu → gọi `RefreshAsync`, cập nhật session, retry **1 lần**.
   Refresh fail → clear session + redirect login.
5. Xử lý **SecurityStamp**: đổi mật khẩu / bị admin khoá / đổi vai trò làm token **và refresh
   token** chết ngay. Sau `ChangePassword` thành công → **đăng xuất và yêu cầu đăng nhập lại**
   (đừng cố giữ phiên). Bất kỳ `401` nào mà refresh cũng fail → về login với thông báo
   "Phiên đăng nhập đã kết thúc, vui lòng đăng nhập lại".
6. Cân nhắc hạ `ExpireTimeSpan` cookie xuống gần với refresh token (30 ngày) và bật kiểm tra
   refresh token còn sống ở middleware thay cho check `Session["Token"]` rỗng.

### 1.4. Chuẩn hoá xử lý lỗi

- Tạo `ApiResult`/`ApiResult<T>` mang `int StatusCode`. Ở Controller:
  - `401` → refresh hoặc login.
  - `403` → tuỳ ngữ cảnh: nội dung/đề trả phí → CTA "Nâng gói"; dashboard con chưa liên kết →
    "Chưa liên kết học sinh này".
  - `404` → trang/thành phần "không tìm thấy" (đừng nuốt thành list rỗng ở chỗ quan trọng).
  - `409` → "đã tồn tại" / "đã nộp" / "đang có yêu cầu xử lý".
  - `429` → "Bạn đã dùng hết lượt hôm nay" (AI hint) / "Thao tác quá nhanh" (refund, auth).
  - `400` → hiện `Errors[]` (validation).
- Log kèm `X-Correlation-ID` từ response header để đối chiếu với log backend khi hỗ trợ.

---

## 2. Luồng Xác thực & Tài khoản (`auth`)

### Backend đã thay đổi

| Việc | Chi tiết |
|---|---|
| Quên / đặt lại mật khẩu (MỚI) | `POST /api/auth/forgot-password` `{ email }` (không lộ email có tồn tại hay không) → email chứa link. `POST /api/auth/reset-password` `{ token, newPassword }`. Token sống 1 giờ, dùng 1 lần. |
| Gửi lại email xác nhận | `POST /api/auth/resend-confirmation` `{ email }` (route cũ WebApp gọi sai: `resend-confirmation-email`). |
| Xác nhận email | Link trong email trỏ trang WebApp `{BaseUrl}/Account/ConfirmEmail?token=…` (hoặc `/xac-thuc-email`). Action `Account/ConfirmEmail` gọi lại API `/api/auth/confirm-email` rồi render 1 trong 3 view: `ConfirmEmailSuccess` / `ConfirmEmailExpired` (message chứa "hết hạn") / `ConfirmEmailFailed`. |
| Chống kẹt khi không nhận được email | `AuthApiService.Login` trả thêm `emailNotConfirmed` (đọc `Errors:["EMAIL_NOT_CONFIRMED"]`). Login thất bại vì lý do này → `ViewBag.EmailNotConfirmed` → khối cảnh báo + nút "Gửi lại email xác nhận" (prefill email) ngay tại form login. Trang Login luôn có link "Gửi lại email xác nhận" cạnh "Quên mật khẩu?". `ResendConfirmationEmail` GET nhận `?email=` để prefill; POST bọc try/catch. |
| Đổi mật khẩu | `POST /api/auth/change-password` (userId lấy từ **token**, KHÔNG có `{id}` trên route). Thành công → **thu hồi toàn bộ refresh token** + bump SecurityStamp. |
| Đăng nhập | Rate-limit + khoá tạm: sai 5 lần → khoá 1→30 phút (tăng dần). Trả `401` kèm message kiểu "Tài khoản tạm khoá, thử lại sau N phút". |
| `/api/auth/me` | Đã sửa, trả `UserId/Email/FullName/UserType/StudentId/ParentId` đúng — dùng để đồng bộ lại claim khi cần. |
| `update-profile` | Chuyển sang `POST /api/users/update-profile/{id}`, chỉ patch trường cho phép (không còn bug đặt `IsActive=false`). |
| Enum | `UserType`, `PackageTier` trả **chuỗi**. `LoginResponse` có `PackageTier` (bỏ `PackageType`). |

### Việc cho Frontend

1. **Trang "Quên mật khẩu"** (mới):
   - `GET/POST Account/ForgotPassword` → form nhập email → gọi `forgot-password` → luôn hiện
     "Nếu email tồn tại, chúng tôi đã gửi hướng dẫn" (không tiết lộ).
   - `GET/POST Account/ResetPassword?token=…` → form mật khẩu mới → gọi `reset-password` →
     thành công về `Login` với TempData; token hỏng/hết hạn → hiện lỗi + link xin gửi lại.
   - Thêm link "Quên mật khẩu?" ở `Views/Account/Login.cshtml`.
2. **Sửa `AuthApiService.ChangePasswordAsync`**: bỏ `{userId}` khỏi URL → `POST /api/auth/change-password`.
   Sau khi đổi thành công: gọi `LogoutAsync` + `SignOutAsync` + redirect `Login` kèm thông báo
   "Đổi mật khẩu thành công, vui lòng đăng nhập lại".
3. **Sửa `ResendConfirmationEmail`**: route `resend-confirmation`, body `{ Email }`; xử lý
   rate-limit `429`.
4. **Sửa `UpdateProfileAsync`**: route `/api/users/update-profile/{id}`; đọc `ApiResponse`.
5. **Đăng nhập**: hiển thị message khoá tạm từ backend (đang có sẵn `ViewBag.Error`, chỉ cần
   không nuốt message). Cân nhắc đếm lần thử phía UI để cảnh báo sớm.
6. **`LoginResponseDto` + claim**: đổi `PackageType`→`PackageTier`; cập nhật `AccountController`
   (dòng tạo claim), `ExamController.DoExam` (đang so `packageType < 2`), `PackageController.Index`
   (đang dùng `sub.PackageType` như PackageId — sai, phải dùng `PackageTier` + `PackageName`).
7. **Refresh token**: xem 1.3.
8. **Logout**: gọi API `logout` để thu hồi refresh token.

**File:** `Controllers/AccountController.cs`, `Services/AuthApiService.cs`,
`Models/DTOs/LoginResponseDto.cs`, `Models/DTOs/PasswordReset*.cs` (mới),
`Views/Account/*` (thêm `ForgotPassword.cshtml`, `ResetPassword.cshtml`).

---

## 3. Luồng Xem bài giảng — có "Guest" + tầng nội dung mới (`learn` / `catalog` / `courses` / `enrollments` / `progress`)

### Backend đã thay đổi (P2 + P4)

Trước đây **không có API nội dung nào**. Nay:

| Endpoint | Auth | Ghi chú |
|---|---|---|
| `GET /api/catalog/subjects`, `/grade-levels`, `/frameworks` (+`/{id}`) | ẩn danh | bản active công khai |
| `GET /api/courses`, `/courses/{id}`, `/courses/by-slug/{slug}` | ẩn danh | course đã publish |
| `GET /api/learn/courses/{courseId}/content` | **ẩn danh (Guest)** | Trả **cây ContentNode**. Guest / chưa mua: **chỉ node `IsFree`**. Đã ghi danh / có gói phủ: **cây đầy đủ**. |
| `GET /api/learn/nodes/{nodeId}` | **ẩn danh (Guest)** | Chi tiết 1 node: `ContentBlock[]`, `LessonResource[]`, `FlashcardDeck`. Node bị khoá → **403**. Guest vượt rate-limit → **429**. |
| `GET /api/enrollments/me` | đăng nhập | các khoá học sinh đang học (`StudentCourse`) |
| `POST /api/enrollments/courses/{courseId}` | đăng nhập | ghi danh 1 khoá (dashboard phụ thuộc bảng này) |
| `POST /api/progress/lessons/{nodeId}/complete` | đăng nhập | body `{ secondsViewed }`. Backend chặn nếu **< 20s** hoặc **chưa ghi danh**. Bỏ hard-code `isCompleted=true`. |
| `GET /api/progress/versions/{courseVersionId}` | đăng nhập | tiến độ theo cây (`NodeProgress` + % roll-up theo chương) |
| `GET /api/progress/students/{studentId}/heatmap?days=90` | chủ sở hữu / phụ huynh | dữ liệu heatmap hoạt động |

**Mô hình "3 bậc truy cập" (`IContentAccessService`):**
`ẩn danh (rate-limited, chỉ node free)` → `đã đăng nhập + ghi danh` → `có entitlement (gói phủ khoá)`.

### Việc cho Frontend

1. **Xoá code chết**: `LessonApiService` (hard-code localhost), các hàm
   `CourseApiService.GetAllLessonsAsync/GetLessonDetailAsync/GetLessonsByTopicAsync/GetCurriculumDetailAsync/GetFullMenuTreeAsync/GetExercisesByTopicAsync/GetCompletedLessonIdsAsync`
   đang gọi `Lesson*` / `Curriculum*` / `Exercise/by-topic` / `LessonProgress` — **không endpoint
   nào còn tồn tại**.
2. **`ContentApiService` mới** bọc `catalog` + `courses` + `learn` + `enrollments` + `progress`.
   DTO mới: `CourseDto`, `ContentNodeDto` (đệ quy: `NodeType` = Chapter/Topic/Lesson,
   `IsFree`, `IsLocked`, `Children`), `ContentBlockDto`, `LessonResourceDto`, `FlashcardDeckDto`,
   `NodeProgressDto`.
3. **`CourseController` viết lại**:
   - `Index` / `Chapter` / `Topic`: dựng cây từ `GET /api/learn/courses/{courseId}/content`.
     Bỏ hằng `DEFAULT_CURRICULUM_ID = 3`; chọn khoá theo môn×lớp×bộ sách qua `catalog` +
     `courses` (hoặc `by-slug`).
   - `Learning(nodeId)`: gọi `GET /api/learn/nodes/{nodeId}`.
     - **200** → render block/resource/flashcard.
     - **403** → trang/panel "Bài học này cần gói … hoặc ghi danh khoá" + CTA (`/Package` hoặc
       nút "Học thử/Ghi danh").
     - **429** (guest) → "Bạn đã xem hết lượt học thử, vui lòng đăng nhập".
   - `UpdateProgress`: đổi sang `POST /api/progress/lessons/{nodeId}/complete` body
     `{ secondsViewed }`. Chỉ gọi khi **≥ 20s** (đo phía client, cho khớp ngưỡng backend);
     xử lý `403` "chưa ghi danh" → hiện nút ghi danh.
4. **Trải nghiệm Guest (chưa đăng nhập)**:
   - Bỏ `[Authorize]` ở `CourseController` cho các action duyệt cây + xem node; **giữ**
     `[Authorize]` cho ghi tiến độ / ghi danh.
   - Với node khoá: hiện badge 🔒, click → modal "Đăng nhập / Nâng gói" thay vì để backend 403.
   - Menu/handbook chỉ hiện các mục `IsFree` khi chưa đăng nhập.
   - **Không gắn `Authorization` header** khi user ẩn danh (một số service đang luôn gắn — sẽ
     gửi token rỗng/hỏng).
5. **Ghi danh**: nút "Ghi danh khoá" gọi `POST /api/enrollments/courses/{courseId}`; sau đó cây
   mở khoá phần miễn phí→đầy đủ (tuỳ entitlement). Trang "Khoá của tôi" từ `GET /api/enrollments/me`.
6. **Tiến độ trong sidebar bài học**: `GET /api/progress/versions/{courseVersionId}` thay cho
   `GetCompletedLessonIdsAsync`. Dùng `Status`/`CompletionPercent` của từng node.

**File:** `Controllers/CourseController.cs`, `Controllers/LessonController.cs`,
`Services/CourseApiService.cs` → tách `ContentApiService.cs`, `Services/LessonApiService.cs` (xoá),
`Models/DTOs/Content/*` (mới), `Views/Course/*`, `Views/Lesson/*`.

---

## 4. Luồng Làm bài tập / Thi (`exercise-attempts` + `exercises`)

### Backend đã thay đổi (P3)

| Việc | Chi tiết |
|---|---|
| Route | `/api/exercise-attempts/*` (kebab). |
| Một đường nộp bài | `/submit` + `/complete` gộp làm một; **`/submit-answer` đã xoá**. Chỉ còn: `POST start`, `POST start-random`, `POST save-answer`, `POST complete`, `GET {id}/result`, `GET student/{id}/history`, `POST {id}/report-tab-switch`, `GET {id}/feedback-status` (MỚI), `GET {id}/tab-switch-logs`. |
| `start` | Body là `StartExerciseDto`; **`StudentId` lấy từ token** (client gửi cũng bị ghi đè). Nếu không phải Student → **403**. |
| MaxAttempts / trạng thái | Thực thi `Exercise.MaxAttempts` + `Published/IsActive`. Hết lượt → lỗi (409/400) — cần hiện "Bạn đã hết lượt làm". |
| Tier gating | `start` **từ chối 403** khi tier gói của học sinh `< Exercise.RequiredTier` (bài free được miễn). |
| Bài ngẫu nhiên | Đã sửa bug `PlannedEndTime` — `start-random` giờ lưu & chấm bình thường. |
| AI feedback | Không còn chờ AI trong `complete`. `complete` **trả ngay** kết quả chấm; feedback chạy nền → **poll `GET {id}/feedback-status`** (hoặc refetch `result`) đến khi xong. |
| Chống spam chuyển tab | `report-tab-switch` cần auth + debounce 15s + ngừng gửi email sau 5 lần/attempt (log vẫn ghi). Client cứ gửi, đừng dựa vào phản hồi để suy hành vi. |
| Chấm điểm | Chuẩn hoá mọi `QuestionType`: `Essay` → "chờ chấm tay"; `FillBlank` chấp nhận phân số/thập phân/khoảng trắng; `TrueFalse` dùng option. UI kết quả cần thể hiện trạng thái "chờ chấm tay". |
| Vỏ response | `start` giờ trả `ApiResponse<ExerciseAttemptDto>` đúng chuẩn; `409` khi gọi `complete` 2 lần (đã nộp). |

### Việc cho Frontend

1. **`ExamApiService`**: đổi hết route `ExerciseAttempts` → `exercise-attempts`; xoá hàm
   `SubmitSingleAnswer` (đã comment sẵn); `AIHint` → `ai-hints` (mục 5).
2. **`ExamController.DoExam`**:
   - Bỏ gate phía client `packageType < 2`. Cứ gọi `start`; nếu **403** → đọc message →
     `TempData["UpgradeMsg"]` + redirect `/Package`. (Có thể vẫn hiện badge "Premium" trên
     danh sách để đỡ bấm nhầm, nhưng **quyết định cuối** là ở backend.)
   - Xử lý **409/400 hết lượt** → hiện "Bạn đã dùng hết N lượt cho đề này" + link xem lại kết quả cũ.
3. **`ExamController.Submit`**: giữ pattern "save từng câu rồi `complete`". Sau `complete`:
   - Đọc `ApiResponse` (không chỉ `IsSuccessStatusCode`).
   - Nếu có câu tự luận / feedback nền: chuyển sang trang `Result` và ở đó **poll
     `feedback-status`** mỗi 3–5s (tối đa ~1–2 phút), cập nhật phần "Nhận xét AI" khi sẵn sàng.
   - `409` (đã nộp) → điều hướng thẳng tới `Result`.
4. **Trang `Result`**: thêm khối "Đang tạo nhận xét…" + trạng thái "Câu tự luận chờ giáo viên chấm".
5. **Bài ngẫu nhiên**: nếu WebApp có nút "Luyện ngẫu nhiên" → dùng `start-random`
   (`StartRandomExerciseDto`: questionBankId/số câu/thời lượng). Nếu chưa có, cân nhắc bổ sung.
6. **`GetCompletedExerciseIdsAsync`**: `history` giờ trả `ApiResponse<...>` — vẫn OK, chỉ đảm
   bảo route mới + enum chuỗi.
7. **Chuyển tab**: giữ nguyên logic gửi; bỏ mọi phụ thuộc vào việc "gửi thành công = đã cảnh báo".

**File:** `Controllers/ExamController.cs`, `Services/ExamApiService.cs`,
`Models/DTOs/ExerciseAttemptDto.cs`, `ExerciseResultDto.cs` (thêm field feedback status),
`Views/Exam/*` (Result poll).

---

## 5. Luồng AI Gợi ý (`ai-hints`)

### Backend đã thay đổi (P6)

- Route `POST /api/ai-hints` (tạo hint). `GET /api/ai-hints/quota`,
  `GET /api/ai-hints/by-attempt/{attemptId}`, `GET /api/ai-hints/by-attempt-question?...`.
- **Hạn mức theo gói**: mỗi hint AI tốn 1 lượt/ngày. Free = 3/ngày (`AI:FreeDailyHintLimit`).
  Gói có `UnlimitedAiHint` → không chặn. Vượt → **429**.
- Reset theo ngày.

### Việc cho Frontend

1. `ExamApiService.GetAIHintAsync` → route `ai-hints`; xử lý **429** → trả về UI thông điệp
   "Bạn đã dùng hết N lượt gợi ý hôm nay. Nâng gói để dùng không giới hạn." (đừng để nút "Gợi ý"
   im lặng không phản hồi).
2. Thêm `GetHintQuotaAsync()` → `GET /api/ai-hints/quota`; hiển thị "Còn X/Y lượt gợi ý" cạnh
   nút Gợi ý trong trang làm bài; disable nút khi hết (Free) / hiện ∞ (Premium).
3. Khi mở lại 1 attempt: `by-attempt/{id}` để lấy lại các hint đã xin (đỡ tốn lượt).

**File:** `Services/ExamApiService.cs`, `Controllers/ExamController.cs`, view trang làm bài.

---

## 6. Luồng Dashboard học sinh & Phụ huynh xem con (`students/{id}/dashboard`)

### Backend đã thay đổi (P4)

| Việc | Chi tiết |
|---|---|
| Route | `GET /api/students/{studentId}/dashboard/overview` \| `/chapter-score-comparison` \| `/ai-assessment` \| `/ai-roadmap`. |
| Vỏ | **Tất cả** giờ bọc `ApiResponse<T>` (trước đây `chapter-score-comparison`/`ai-*` trả thẳng — code WebApp đang deserialize thẳng sẽ **null**). |
| Dữ liệu thật | `NodeProgress` đã được ghi sau mỗi lần submit → "bài hoàn thành", "% chương", "bài gần đây", "chủ đề yếu" **không còn rỗng**. `ai-assessment` / `ai-roadmap` chạy nhánh AI thật. |
| Tier gating | `chapter-score-comparison` cần **Standard+**, `ai-assessment` + `ai-roadmap` cần **Premium+**, nếu không → **403** "Tính năng này cần gói …". |
| `PackageTier` | Thay hết so khớp chuỗi tên gói. DTO trả `PackageTier` enum chuỗi. |
| Streak / heatmap | `GET /api/progress/students/{id}/heatmap?days=90`. |
| Bản dashboard rút gọn | `GET /api/students/dashboard-stats` (từ token) vẫn tồn tại song song — chọn 1 để tránh lệch số. |

### Việc cho Frontend

1. **`DashboardApiService`**: sửa route (`students`, thêm `/overview`); **bọc lại tất cả** trong
   `ApiResponse<T>` (bỏ mấy comment "backend returns Ok(result) without ApiResponse wrapper" —
   không còn đúng).
2. **Tier gating**: `DashboardController.ChartData/AIAssessment/AIRoadmap` — khi nhận **403**,
   render panel "Nâng cấp để xem" thay vì khối rỗng. Ẩn/disable link Charts & AI Insights ở menu
   khi `PackageTier` < ngưỡng (đọc từ claim `PackageTier`).
3. **`CourseApiService.GetStudentDashboardStatsAsync`** đang gọi `student/{id}/dashboard/overview`
   thiếu `s` → sửa; hoặc gộp về `DashboardApiService`.
4. **Bỏ studentId hard-code**: `StudentController.Dashboard` đang `?? 5`. Dùng `dashboard-stats`
   (token) hoặc lấy `StudentId` từ claim, không fallback bừa.
5. **Heatmap/streak**: nối endpoint mới nếu view có lịch hoạt động.
6. Enum chuỗi: `MasteryLevel`, `ProgressStatus`, `PackageTier`, `NodeType` trong DTO dashboard.

**File:** `Services/DashboardApiService.cs`, `Controllers/DashboardController.cs`,
`Controllers/StudentController.cs`, `Models/DTOs/CoreDashboardDto.cs`, `AIInsightResponse.cs`,
`Views/Dashboard/*`, `Views/Student/Dashboard.cshtml`.

---

## 7. Luồng Liên kết Phụ huynh – Học sinh (`parents`)

### Backend đã thay đổi (P6)

Route `api/parents`. Mô hình mới `ParentLink` (Pending → Active → Revoked) + `ParentInvite`:

| Endpoint | Ai gọi | Ghi chú |
|---|---|---|
| `GET /api/parents/{id}` | phụ huynh đó / admin | thông tin + `ConnectionCode` |
| `POST /api/parents/{id}/invites` | phụ huynh | body `CreateParentInviteDto { inviteeEmail?, relationship, expiresInDays }` → tạo token mời |
| `POST /api/parents/link` | **học sinh hoặc phụ huynh** | body `LinkParentDto { Code, Relationship }` — `Code` = `ConnectionCode` của phụ huynh **hoặc** token invite |
| `GET /api/parents/{id}/children` | phụ huynh | danh sách `ParentLink` (kèm `Status`) |
| `GET /api/parents/{id}/children/overview` | phụ huynh | tổng hợp nhiều con: `ChildOverviewDto { PackageTier, WeeklyStudyMinutes, WeeklyAverageScore, CurrentStreak, StudiedToday, … }` |
| `DELETE /api/parents/{id}/children/{studentId}` | phụ huynh | revoke → **mất quyền xem dashboard con ngay** (403) |

- `enum` `ParentRelationship { Father, Mother, Guardian, Other }`, `LinkStatus`,
  `ParentInviteStatus` — **chuỗi**.
- Revoke / chưa `Active` → mọi call dashboard của con đó trả **403**.

### Việc cho Frontend

1. **`StudentController.ConnectParent`**: đổi `POST /api/StudentParent/connect` →
   `POST /api/parents/link` body `LinkParentDto { Code = model.ConnectionCode, Relationship }`.
   `Relationship` gửi **chuỗi** (`"Guardian"`), không phải số `2`.
   Xử lý `409` "đã liên kết", `404` "mã không đúng", `410`/`400` "mã hết hạn".
2. **`StudentController.GetConnectedParentsAsync`**: không còn `student/{id}/parents`. Danh sách
   phụ huynh của học sinh hiện **không có endpoint trực tiếp** — hoặc (a) đề nghị backend thêm
   `GET /api/students/{id}/parents`, hoặc (b) tạm ẩn mục này ở trang Profile học sinh.
3. **`ParentController` (WebApp)**:
   - `Connection`: `GET /api/parents/{parentId}` để lấy `ConnectionCode` hiển thị cho phụ huynh
     đọc cho con nhập. Bỏ parse `Data.Children` kiểu cũ — dùng `GET /api/parents/{id}/children`.
   - Thêm **"Mời qua email"**: form → `POST /api/parents/{id}/invites`.
   - `Report` / `GetStudentReport`: `GET /api/students/{studentId}/dashboard/overview`
     (sửa `student`→`students`); nếu **403** → "Bạn chưa liên kết (hoặc đã huỷ liên kết) học
     sinh này".
   - **Trang tổng quan nhiều con** (mới/nâng cấp): `GET /api/parents/{id}/children/overview`
     cho `Parent/Dashboard`.
   - Nút "Huỷ liên kết": `DELETE /api/parents/{id}/children/{studentId}`.
4. Bỏ giả định response PascalCase `Data`/`Children` — đọc qua `ApiResponse<T>` + DTO thật.

**File:** `Controllers/ParentController.cs`, `Controllers/StudentController.cs`,
`Models/DTOs/Parent/*` (mới), `Views/Parent/{Dashboard,Connection,Report}.cshtml`,
`Views/Student/Profile.cshtml`.

---

## 8. Luồng Gói & Thanh toán (`subscriptions` / `payments` / `packages` / SePay)

### Backend đã thay đổi (P5 + luồng thanh toán)

| Việc | Chi tiết |
|---|---|
| Route | `api/subscriptions`, `api/payments`, `api/packages`, webhook `POST /api/sepay/ipn`. |
| Tạo subscription | `POST /api/subscriptions` body **`CreateSubscriptionDto { StudentId, PackageId }`** — **KHÔNG còn `AmountPaid`** (server lấy `Package.Price`). Response `ApiResponse<object>` `{ subscriptionId, amount, qrUrl }`. |
| "Gói của tôi" | `GET /api/subscriptions/me`; `GET /api/students/{id}/subscription/current` → `SubscriptionInfoDto` (giờ có **`PackageTier`** + feature flags `UnlimitedAiHint`, `MistakeRetry`, …; **bỏ `PackageType`**). |
| "Lịch sử thanh toán" | `GET /api/payments/me` (payer hoặc beneficiary), có **phân trang** (`PagedResult`). |
| Trạng thái | `GET /api/subscriptions/status/{id}` → `ApiResponse<object>` `{ status, endDate }` (bọc envelope — code WebApp đang deserialize thẳng sẽ hỏng). |
| Huỷ | `PUT /api/subscriptions/cancel/{id}`. |
| Check premium | `GET /api/subscriptions/check-premium/{studentId}`. |
| Vòng đời tự động | Job nền: `Active` quá hạn → `Expired`; `Pending` quá 30′ → `Cancelled`. WebApp nên poll `status` khi ở màn QR và xử lý mọi trạng thái cuối. |
| Nhiều gói Active | Mua gói mới khi đang có gói → gói cũ tự `Expired`. |
| Kích hoạt tay | `PATCH /api/subscriptions/{id}/status` chỉ Finance/Admin. |
| Danh sách toàn bộ sub/payment | chỉ Finance/Admin (`403` cho học sinh) — bỏ nếu WebApp học sinh đang gọi. |

### Việc cho Frontend

1. **`SubscriptionApiService.CreateSubscriptionAsync`**: bỏ tham số `amount` + field `AmountPaid`
   khỏi payload. Deserialize `ApiResponse<CreateSubscriptionResultDto>` (không phải thẳng).
   `PackageController.Checkout` bỏ truyền `package.Price`.
2. **`GetSubscriptionStatusAsync`**: parse `ApiResponse<...>`; `PackageController.CheckStatus`
   poll cho tới `Active`/`Expired`/`Cancelled`, hiện đúng thông điệp (kể cả "Hết hạn thanh
   toán, vui lòng tạo lại" khi `Cancelled` do timeout 30′).
3. **`GetCurrentSubscriptionAsync`**: route `students`; DTO đổi `PackageType` → `PackageTier`.
   `PackageController.Index`: bỏ đoạn "map PackageType → PackageId"; so khớp bằng
   `PackageTier`/`PackageName`. Dùng feature flags từ `SubscriptionInfoDto` để hiện "gói của
   bạn gồm: …".
4. **`PackageApiService`**: route `packages` (GET công khai — có thể bỏ `Bearer` cho khách xem
   bảng giá).
5. **Trang "Gói của tôi" / "Lịch sử thanh toán"** (mới hoặc nâng cấp): `subscriptions/me` +
   `payments/me` (nhớ `PagedResult`: `items` + `totalCount` + `page`).
6. **Claim `PackageTier`**: sau khi thanh toán thành công, gói mới chỉ vào **access token mới**
   → cần **refresh token** hoặc buộc re-login để claim `PackageTier` cập nhật (nếu không, UI
   vẫn tưởng Free). Ghi chú rõ trong màn "Thanh toán thành công".
7. **DevOps**: đổi URL webhook trên dashboard SePay sang `/api/sepay/ipn`.

**File:** `Services/SubscriptionApiService.cs`, `Services/PackageApiService.cs`,
`Controllers/PackageController.cs` (`PackageControllercs.cs`),
`Models/DTOs/SubscriptionInfoDto.cs`, `CurrentSubscriptionDto.cs`, `PaymentDTOs.cs`,
`Views/Package/*`.

---

## 9. Luồng Hoàn tiền (`refunds`) — MỚI

### Backend (P8 — semi-automatic refund)

- **`POST /api/payments/{id}/refund` cũ đã XOÁ.**
- Người dùng:
  - `POST /api/refunds` — body `CreateRefundRequestDto` (lý do + thông tin TK ngân hàng người
    nhận: số TK, tên chủ TK, mã ngân hàng). Caller **phải sở hữu** giao dịch. `EnableRateLimiting("refund")`.
  - `GET /api/refunds/me` — phân trang, các yêu cầu của mình.
  - `GET /api/refunds/{id}` — chi tiết + timeline `Events`.
- Ràng buộc: chỉ hoàn giao dịch **Completed**; không quá 180 ngày; đang có yêu cầu xử lý → `409`;
  quá 3 yêu cầu/30 ngày → `409`; quá nhanh → `429`.
- Vòng đời: `PendingReview → Approved → Batched → Disbursed → Completed` (Finance xử lý tay,
  ngoài phạm vi WebApp học sinh). Số TK trả về **chỉ 4 số cuối**.

### Việc cho Frontend

1. **`RefundApiService` mới** + DTO `CreateRefundRequestDto`, `RefundRequestDto` (Status enum
   chuỗi, `BankAccountLast4`, `Events[]`).
2. **UI "Yêu cầu hoàn tiền"** trong trang "Lịch sử thanh toán": nút ở mỗi `Payment` đủ điều kiện
   → form (lý do, số TK, tên chủ TK, ngân hàng) → `POST /api/refunds`.
   Xử lý `400` (điều kiện), `409` (đã có/hết hạn mức), `429`.
3. **Trang "Yêu cầu hoàn tiền của tôi"**: `GET /api/refunds/me` + xem timeline trạng thái từ
   `GET /api/refunds/{id}`.
4. Nếu WebApp **không** phục vụ Finance thì **không** cần các endpoint `/api/finance/refunds/*`.

**File:** `Services/RefundApiService.cs` (mới), `Models/DTOs/Refund/*` (mới),
`Controllers/PaymentController` hoặc `AccountController` (thêm action), `Views/...`.

---

## 10. Luồng Thông báo (`notifications`) — MỚI

### Backend (P6)

| Endpoint | Ghi chú |
|---|---|
| `GET /api/notifications?page=&pageSize=` | phân trang; sinh theo luật: chuyển tab, điểm < 5, nghỉ 3 ngày |
| `GET /api/notifications/unread-count` | badge |
| `POST /api/notifications/{id}/read`, `POST /api/notifications/read-all` | |
| `GET /api/notifications/preferences`, `PUT /api/notifications/preferences` | opt-out theo từng loại luật (`SetNotificationPreferenceDto`) |

- `Audience` Student/Parent/Both — phụ huynh cũng nhận (nếu link Active).

### Việc cho Frontend

1. **`NotificationApiService` mới**.
2. **Chuông thông báo** trên layout (`_Layout.cshtml`): poll `unread-count` mỗi ~60s; dropdown
   danh sách; click → `{id}/read`; nút "đánh dấu tất cả đã đọc".
3. **Trang "Cài đặt thông báo"** trong Profile: `GET/PUT preferences`.
4. Hiển thị cho **cả** Student và Parent layout.

**File:** `Services/NotificationApiService.cs` (mới), `Models/DTOs/Notification/*` (mới),
`Views/Shared/_Layout.cshtml`, `ViewComponents/NotificationBellViewComponent.cs` (mới).

---

## 11. Luồng Chatbot (`chatbot`)

### Backend đã thay đổi (P6)

- Vẫn `POST /api/chatbot/message` `/quick-reply` `/trigger` (WebApp đã có).
- **Mới**: `GET /api/chatbot/conversations`, `GET /api/chatbot/conversations/{id}/messages`
  (lịch sử đã lưu phía C# — sống sót khi Python restart), `GET /api/chatbot/health` (503 khi
  AI down), `POST /api/chatbot/request-human` (chuyển nhân viên),
  `POST /api/chatbot/conversations/{id}/close`.
- AI down → API **vẫn 200**, message hệ thống "chưa phản hồi" (không vỡ luồng).
- `[Authorize]` — cần token (trước có thể ẩn danh).

### Việc cho Frontend

1. **`ChatApiService`**: đảm bảo gắn `Bearer` (giờ bắt buộc auth); giữ `message/quick-reply/trigger`.
2. Thêm: tải **lịch sử hội thoại** khi mở widget (`conversations` + `messages`); nút **"Gặp nhân
   viên hỗ trợ"** → `request-human`; ẩn/disable input khi `health` = 503 và hiện "Trợ lý AI tạm
   nghỉ".
3. Bỏ mọi giả định "payload dạng Flask" — đọc theo `ApiResponse`/DTO của C#.

**File:** `Services/ChatApiService.cs`, `Controllers/ChatController.cs`, `Views/Shared` (widget).

---

## 12. Thứ tự triển khai đề xuất

| Đợt | Nội dung | Vì sao trước |
|---|---|---|
| **Đợt 0 — Hạ tầng (bắt buộc, 1 PR)** | 1.1 `ApiClient` + `JsonStringEnumConverter` toàn cục · 1.2 sửa **toàn bộ route** · 1.3 refresh token · 1.4 chuẩn hoá lỗi · sửa `LoginResponseDto`/claim `PackageTier` | Không có bước này, deploy backend mới là **app chết ngay** (404 + lỗi enum + văng phiên sau 30′) |
| **Đợt 1 — Auth** | Mục 2: quên/đặt lại mật khẩu, đổi mật khẩu, resend, logout thu hồi | Ít phụ thuộc, giá trị cao, dễ test |
| **Đợt 2 — Xem bài giảng + Guest** | Mục 3: `ContentApiService`, viết lại `CourseController`, gating 403/429, tiến độ mới | Tính năng lõi; nhiều code chết cần dọn |
| **Đợt 3 — Làm bài + AI hint** | Mục 4 + 5: một đường nộp bài, feedback nền (poll), hạn mức hint 429, tier 403 | Phụ thuộc Đợt 0; ảnh hưởng người dùng nhiều nhất |
| **Đợt 4 — Dashboard + Phụ huynh** | Mục 6 + 7: envelope + tier gating + liên kết bằng mã/lời mời + overview nhiều con | Phụ thuộc dữ liệu tiến độ (Đợt 2) |
| **Đợt 5 — Thanh toán + Hoàn tiền** | Mục 8 + 9: bỏ `AmountPaid`, `me` endpoints, refresh claim sau mua, refund workflow | Cần refresh token (Đợt 0) để claim gói cập nhật |
| **Đợt 6 — Thông báo + Chatbot** | Mục 10 + 11: chuông thông báo, preferences, lịch sử chat, request-human | Tính năng bổ sung, không chặn |

**Kiểm thử hồi quy tối thiểu mỗi đợt:** đăng nhập → để 31 phút → gọi 1 API (phải auto-refresh,
không văng) · Guest mở cây khoá học (chỉ thấy bài free) · học sinh Free mở bài Premium (403 →
CTA nâng gói) · làm bài có câu tự luận (kết quả hiện ngay, nhận xét AI hiện sau) · phụ huynh xem
con chưa liên kết (403) · mua gói → màn "thành công" nhắc đăng nhập lại để mở khoá Premium.

---

## 13. Phụ lục — Bảng đổi DTO chính (WebApp)

| DTO WebApp | Sửa gì |
|---|---|
| `LoginResponseDto` | `+RefreshToken`, `+RefreshTokenExpiration`, `+TokenExpiration`; `int PackageType` → `PackageTier PackageTier` (enum) |
| `SubscriptionInfoDto` / `CurrentSubscriptionDto` | `PackageType` → `PackageTier`; `+UnlimitedAiHint, AiHintLimitDaily, PersonalizedPath, MistakeRetry, SmartReminder, PrioritySupport` |
| `UserType` (enum) | Giữ tên; đảm bảo deserialize từ **chuỗi** (thêm converter) |
| `ExerciseDetailDto` / `ExerciseDto` | `+RequiredTier` (enum); dùng để hiện badge, KHÔNG để gate cứng |
| `ExerciseResultDto` | `+FeedbackStatus` / trạng thái "chờ chấm tay" cho câu Essay |
| `CreateSubscription*` payload | Bỏ `AmountPaid` |
| Mới | `Content/*` (ContentNodeDto, ContentBlockDto, LessonResourceDto, FlashcardDeckDto, NodeProgressDto), `Parent/*` (LinkParentDto, CreateParentInviteDto, ChildOverviewDto, ParentLinkDto), `Refund/*`, `Notification/*`, `FeedbackStatusDto`, `PagedResult<T>` |
| Mọi enum trong DTO | `MasteryLevel, ProgressStatus, NodeType, LinkStatus, ParentRelationship, SubscriptionStatus, PaymentStatus, RefundStatus, NotificationType` — deserialize dạng chuỗi |

---

## 14. Rủi ro & lưu ý

- **`ApiConstant.apiBaseUrl` đang trỏ IP `http://103.98.152.182`** (HTTP thuần) — refresh token
  đi qua đây; cân nhắc HTTPS trước khi lên thật.
- **Session-based token store**: nếu chạy nhiều instance WebApp cần distributed session thật
  (hiện `AddDistributedMemoryCache` = in-memory, mất khi restart → đã có middleware đá về login).
  Refresh token nên lưu cùng chỗ với access token.
- **`PagedResult<T>`**: các list `users / subscriptions / payments / notifications / question-banks /
  refunds/me / payments/me` giờ **phân trang** — chỗ nào đang `foreach` thẳng mảng sẽ vỡ.
- **Guest rate-limit**: `learn/*` có giới hạn cho ẩn danh — nếu WebApp proxy tất cả request qua
  1 IP server, khách sẽ **chung hạn mức**. Cân nhắc chuyển tải nội dung công khai sang gọi
  trực tiếp từ trình duyệt hoặc gắn IP thật của client (`X-Forwarded-For`).
- **Correlation ID**: nên hiển thị/log để đối chiếu khi báo lỗi cho team backend.
- Backend còn 1 số việc ⏳ (JWT SecretKey ra khỏi repo, contract test, load test) — không chặn
  WebApp nhưng cần biết khi lên production.
