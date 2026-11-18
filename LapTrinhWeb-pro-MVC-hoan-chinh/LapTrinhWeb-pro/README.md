# Grand Horizon Hotel — ASP.NET MVC 5

Dự án được giữ theo công nghệ gốc: **ASP.NET MVC 5, C#, .NET Framework 4.7.2, Entity Framework 6 Database First, SQL Server**.

## Chức năng đã hoàn thiện

- Đăng ký tài khoản, kiểm tra email đã tồn tại, xác nhận mật khẩu.
- Đăng nhập / đăng xuất bằng Session.
- Mật khẩu mới được băm bằng PBKDF2 + salt; tài khoản cũ dùng SHA-256 vẫn đăng nhập được và tự nâng cấp hash sau lần đăng nhập thành công.
- Hiển thị giao diện khách sạn Grand Horizon.
- Đặt phòng theo loại phòng và ngày nhận/trả.
- Kiểm tra ngày hợp lệ, kiểm tra phòng đã có booking trùng thời gian.
- Tính tiền tại server, không tin giá gửi từ JavaScript.
- Lưu một booking vào `BOOKING`, phòng vào `BOOKING_DETAIL`, dịch vụ vào `SERVICE_BOOKING`.
- Ghi nhận khách mới nếu đặt phòng không đăng nhập.
- Xem lịch sử đặt phòng của tài khoản đang đăng nhập.
- Có kiểm tra Anti-forgery token cho các form POST.
- Có script SQL để tạo Database, các bảng, khóa ngoại và dữ liệu phòng/dịch vụ mẫu.

## Cấu trúc quan trọng

- `LapTrinhWeb-pro.sln`: mở bằng Visual Studio.
- `LapTrinhWeb-pro/LapTrinhWeb-pro.csproj`: project MVC.
- `LapTrinhWeb-pro/Controllers`: xử lý tài khoản, đặt phòng, trang chủ.
- `LapTrinhWeb-pro/Models/HotelDb.edmx`: Entity Framework Database First.
- `LapTrinhWeb-pro/Views`: giao diện Razor.
- `LapTrinhWeb-pro/Content/Script/Script.js`: tính tiền, kiểm tra form và gửi yêu cầu đặt phòng.
- `database.sql`: tạo cấu trúc database và dữ liệu nền.

## Cách chạy lần đầu

### 1. Yêu cầu

- Windows.
- Visual Studio 2019/2022 có workload **ASP.NET and web development**.
- .NET Framework 4.7.2 Developer Pack.
- SQL Server Express hoặc SQL Server Developer.
- SQL Server Management Studio (SSMS).

### 2. Tạo Database

1. Mở SSMS và kết nối SQL Server của bạn.
2. Mở file `database.sql` ở thư mục gốc.
3. Nhấn **Execute**. Script sẽ tạo `HotelDb`, các bảng và dữ liệu nền nếu chưa có.
4. Script không xóa dữ liệu hiện có.

### 3. Cấu hình kết nối SQL Server

Mở `LapTrinhWeb-pro/Web.config`, tìm connection string `HotelDbEntities`.

Mặc định hiện tại:

```xml
data source=.\SQLEXPRESS;initial catalog=HotelDb;integrated security=True;trustservercertificate=True;MultipleActiveResultSets=True;App=EntityFramework
```

Nếu SQL Server trên máy bạn có tên khác, chỉ sửa phần `data source`, ví dụ:

- `.\SQLEXPRESS`
- `(localdb)\MSSQLLocalDB`
- `TENMAY\SQLEXPRESS`
- `localhost`

Giữ nguyên phần `metadata=...` và `provider=System.Data.SqlClient`.

### 4. Mở và chạy website

1. Mở `LapTrinhWeb-pro.sln` bằng Visual Studio.
2. Chọn **Restore NuGet Packages** nếu Visual Studio yêu cầu.
3. Đặt `LapTrinhWeb-pro` làm Startup Project.
4. Chạy bằng IIS Express (F5).
5. Trang chủ mở ở `/`.

## Dữ liệu mẫu

Sau khi chạy `database.sql`, các loại phòng và giá mặc định:

| Loại phòng | Giá / đêm |
|---|---:|
| Standard | 1.200.000 đ |
| Deluxe | 2.200.000 đ |
| Executive Suite | 4.500.000 đ |

Dịch vụ mẫu:

| ID | Dịch vụ | Giá tính ở server |
|---:|---|---:|
| 1 | Buffet Sáng | 250.000 đ / đêm |
| 2 | Đưa Đón Sân Bay | 400.000 đ / chuyến |
| 3 | Spa & Massage | 600.000 đ / gói |
| 4 | Giặt Ủi Nhanh | 150.000 đ / đêm |

Đăng ký một tài khoản mới trên trang web để thử đăng nhập và đặt phòng. Không cần tài khoản demo có mật khẩu cố định.

## Luồng lưu dữ liệu đặt phòng

1. Trình duyệt gửi thông tin form đến `BookingController.Create`.
2. Server kiểm tra dữ liệu, loại phòng, dịch vụ, ngày và phòng còn trống.
3. Server tự tính tổng tiền.
4. Server mở transaction SQL và lưu `BOOKING`.
5. Lưu phòng vào `BOOKING_DETAIL`.
6. Lưu từng dịch vụ vào `SERVICE_BOOKING`.
7. Commit transaction; nếu lỗi thì rollback.

## Lưu ý

- Môi trường hiện tại không có Visual Studio/MSBuild và SQL Server để chạy build hoặc kiểm thử kết nối thực tế. Vì vậy bạn cần thực hiện các bước Restore NuGet, cấu hình đúng tên SQL Server và chạy `database.sql` trên máy Windows của bạn.
- Dữ liệu phòng/dịch vụ trong script là dữ liệu mẫu. Bạn có thể bổ sung phòng, giá và dịch vụ trong SQL Server.
- Giá dịch vụ hiện được quy định trong `BookingController.ServicePrices`; nếu thay đổi giá hoặc ID dịch vụ trong database, hãy cập nhật dictionary tương ứng.

## Tính năng mới trong bản v2

### Route mới
| Route | Chức năng |
|---|---|
| `/Rooms/Index` | Danh sách phòng, lọc theo địa điểm / giá / số khách / ngày còn trống |
| `/Rooms/Details/{id}` | Chi tiết 1 loại phòng (ảnh, giá, diện tích, giường, tiện nghi, đánh giá) |
| `/Booking/Create` | Trang đặt phòng riêng (đầy đủ form, mã giảm giá, phương thức thanh toán) |
| `/Booking/Confirmation?id={bookId}` | Trang xác nhận sau khi đặt phòng thành công |
| `/Services/Index` | Đầy đủ 11 dịch vụ của khách sạn |
| `/Destinations/Index` | 7 điểm đến (Đà Nẵng, Sài Gòn, Hội An, Đà Lạt, Nha Trang, Phú Quốc, Hà Nội) |
| `/Destinations/Details/{id}` | 1 khách sạn + các loại phòng của khách sạn đó |
| `/Admin/Index` | Quản trị: tất cả booking, trạng thái phòng, sửa giá phòng |
| `/Booking/MyBookings` | Thêm nút Hủy đặt phòng (chỉ khi chưa tới ngày nhận phòng) |

### Tài khoản demo (mật khẩu `123456`, chạy `database.sql` bản v2 để tạo)
- Khách: `demo@grandhorizon.vn` / `123456` (Nguyễn Gia Bảo, 0369436165)
- Quản trị: `admin@grandhorizon.vn` / `123456` (Quản Trị) — chỉ tài khoản này mới vào được `/Admin/Index`
- Mật khẩu trong DB là SHA256 của `123456`; `AccountController` tự nâng cấp lên PBKDF2 ở lần đăng nhập đầu tiên.

### Mã giảm giá
- `GRAND10` (không phân biệt hoa thường): giảm 10% tổng tiền, nhập ở trang `/Booking/Create`. Tổng tiền sau giảm được lưu vào `BOOKING.total_amount`; nếu khách chọn phương thức thanh toán thì có thêm 1 dòng `PAYMENT` với số tiền sau giảm.

### Dữ liệu mới trong `database.sql` (bản v2, đều có `IF NOT EXISTS`)
- 6 khách sạn mới (id 2-7): Sài Gòn, Hội An, Đà Lạt, Nha Trang, Phú Quốc, Hà Nội.
- 3 loại phòng mới cho Đà Nẵng: Family Suite 3.800.000đ, Ocean View Deluxe 2.800.000đ, Presidential Suite 8.500.000đ (trên database mới sẽ có id 4, 5, 6; nếu database đã có loại phòng khác thì id có thể khác — trang chi tiết phòng vẫn tìm được bằng tên qua `?name=...`).
- Đủ 6 loại phòng cho mỗi khách sạn mới, mỗi loại 2 phòng Available.
- Chi tiết phòng (diện tích, giường, tiện nghi...) không có cột trong bảng `ROOM_TYPE` nên được để trong `Models/RoomCatalog.cs`, không sửa EDMX.

### Cách test bản v2
1. Chạy `database.sql` trong SSMS (chạy lại trên database cũ cũng được, script v2 không tạo trùng).
2. Restore NuGet Packages, Build (Ctrl+Shift+B), F5.
3. Đăng nhập `demo@grandhorizon.vn` / `123456`.
4. Click 1 phòng ở trang chủ -> xem chi tiết -> "Đặt phòng này" -> trang `/Booking/Create` -> nhập mã `GRAND10` -> Xác nhận -> trang xác nhận -> xem `/Booking/MyBookings` và thử Hủy.
5. Đăng nhập `admin@grandhorizon.vn` / `123456` -> mở `/Admin/Index` -> thử sửa giá 1 loại phòng.
6. Kiểm tra trong SSMS: `BOOKING`, `BOOKING_DETAIL`, `SERVICE_BOOKING`, `PAYMENT` có dòng mới.

### Lưu ý
- Môi trường hiện tại không có Visual Studio/MSBuild và SQL Server để chạy build hoặc kiểm thử kết nối thực tế. Vì vậy bạn cần thực hiện các bước Restore NuGet, cấu hình đúng tên SQL Server và chạy `database.sql` trên máy Windows của bạn.
- Dữ liệu phòng/dịch vụ trong script là dữ liệu mẫu. Bạn có thể bổ sung phòng, giá và dịch vụ trong SQL Server.
- Giá dịch vụ hiện được quy định trong `BookingController.ServicePrices`; nếu thay đổi giá hoặc ID dịch vụ trong database, hãy cập nhật dictionary tương ứng.

### Bản v3 — Đặt phòng phải đăng nhập + OTP + Đơn hàng của tôi

#### 1. Đặt phòng phải đăng nhập
- Chưa đăng nhập mà mở `/Booking/Create` hoặc submit form đặt phòng (trang chủ
  hoặc `/Booking/Index`) thì bị chuyển tới `/Account/Login` kèm `returnUrl`.
  Sau khi đăng nhập xong (và qua OTP, xem mục 2) sẽ quay lại đúng trang đặt
  phòng đã chọn, không phải chọn lại.
- Khách vãng lai KHÔNG đặt được nữa: nhánh tự tạo GUEST theo email trong
  `BookingController.Create` đã bị xóa. Chỉ tài khoản đang đăng nhập mới đặt được.
- Trang Login có link "Chưa có tài khoản? Đăng ký ngay" (giữ `returnUrl`),
  trang Register có link ngược "Đã có tài khoản? Đăng nhập". Đăng ký xong cũng
  quay lại đúng trang đặt phòng (đăng ký không cần OTP).

#### 2. Đăng nhập có xác minh mã OTP (6 số, gửi qua email)
- Luồng: nhập email + mật khẩu đúng → hệ thống gửi mã OTP 6 số tới email đó →
  nhập đúng mã ở trang `/Account/VerifyOtp` mới được đăng nhập. Mã hết hạn sau
  5 phút, nhập sai 3 lần thì phải đăng nhập lại từ đầu. Có nút "Gửi lại mã".
- Tài khoản demo `demo@grandhorizon.vn / 123456` và admin
  `admin@grandhorizon.vn / 123456` cũng phải qua OTP như tài khoản thường.
  Đăng ký, đặt phòng và hủy phòng KHÔNG cần OTP.
- Mã OTP chỉ lưu trong `Session` (server-side), không tạo thêm bảng database,
  không lưu cookie. Mã sinh bằng `RandomNumberGenerator` (không dùng `Random`).

#### 3. Chế độ demo OTP (mặc định BẬT) và cách gửi mail thật
- Mặc định `OtpDemoMode = true` trong `Web.config` → **không gửi mail thật**,
  mã OTP hiện ngay trên trang VerifyOtp (dòng "Chế độ demo — mã OTP của bạn
  là: ..."). Chế độ này để demo/chấm bài trên lớp không cần mạng hay Gmail.
- Muốn gửi mail thật, sửa trong `Web.config` mục `<appSettings>`:
  1. Đổi `OtpDemoMode` thành `false`.
  2. Điền `OtpSmtpUser` = email Gmail của bạn (ví dụ `tenban@gmail.com`).
  3. Điền `OtpSmtpPass` = **Mật khẩu ứng dụng (App Password) của Gmail** —
     KHÔNG phải mật khẩu đăng nhập Gmail thường.
  4. `OtpSmtpHost` mặc định `smtp.gmail.com`, `OtpSmtpPort` mặc định `587`.
- Cách lấy Gmail App Password (làm 1 lần trên trình duyệt):
  1. Vào tài khoản Google → Bảo mật → bật **Xác minh 2 bước** (2-Step Verification).
  2. Vẫn ở mục Bảo mật → **Mật khẩu ứng dụng** (App Passwords) → chọn ứng dụng
     "Mail", thiết bị "Windows Computer" → bấm Tạo.
  3. Google hiện 1 mã gồm 16 ký tự (dạng `xxxx xxxx xxxx xxxx`) — copy mã này
     (bỏ dấu cách cũng được) dán vào `OtpSmtpPass` trong `Web.config`.
  4. Không bao giờ gửi mã này cho ai, không paste vào khung chat hay commit
     lên git công khai. Nếu gửi mail lỗi (sai App Password, mất mạng...),
     trang VerifyOtp sẽ báo lỗi gửi mail nhưng KHÔNG crash và KHÔNG đăng nhập
     luôn; mã OTP được ghi ra cửa sổ Output/Debug của Visual Studio để demo.

#### 4. Trang "Đơn hàng của tôi"
- Menu khi đã đăng nhập đổi "Đặt phòng của tôi" thành **"Đơn hàng của tôi"**,
  trang `/Booking/MyBookings` đổi tên tương tự.
- Bảng đơn hàng thêm 2 cột sau cột "Phòng": **Khách sạn** (tên + địa chỉ,
  ví dụ "Grand Horizon Đà Nẵng — 123 Đường Biển, Đà Nẵng") và **Trạng thái**
  (Sắp tới / Đang ở / Đã trả phòng, tính từ ngày nhận/trả phòng).
- Nút Hủy chỉ hiện khi đơn "Sắp tới" (giữ nguyên logic cũ). Sau khi đặt phòng
  thành công, trang xác nhận có link "Xem đơn hàng của tôi →".
- Menu ở mọi trang đã có thêm mục **"Trang chủ"** đầu tiên, logo
  GRAND HORIZON ở mọi trang đều về trang chủ (`/`).

#### 5. Cách test bản v3
1. Chạy `database.sql` trong SSMS (nếu chưa chạy bản v2), Restore NuGet Packages, Build (Ctrl+Shift+B), F5.
2. Chưa đăng nhập, bấm "Đặt phòng" ở trang chủ → phải về trang Login. Bấm "Chưa có tài khoản? Đăng ký ngay" → sang Register, đăng ký xong → quay lại trang đặt phòng.
3. Đăng nhập `demo@grandhorizon.vn / 123456` → sang trang VerifyOtp, mã OTP hiện ngay trên trang (chế độ demo) → nhập đúng mã → vào trang chủ, menu hiện "Xin chào, Nguyễn Gia Bảo" và "Đơn hàng của tôi". Nhập sai mã 3 lần → bắt đăng nhập lại. Để quá 5 phút → báo hết hạn, bấm "Gửi lại mã" để nhận mã mới.
4. Bấm logo và mục "Trang chủ" ở các trang Trang chủ, Phòng, Đặt phòng, Đơn hàng, Đăng nhập → đều về trang chủ.
5. Đặt 1 phòng ở Đà Nẵng rồi 1 phòng ở Hội An (chọn khách sạn khác nhau khi đặt) → vào "Đơn hàng của tôi" → 2 đơn hiện đúng 2 khách sạn khác nhau, có cột Trạng thái. Thử Hủy 1 đơn "Sắp tới".
6. Kiểm tra trong SSMS: `BOOKING`, `BOOKING_DETAIL`, `SERVICE_BOOKING`, `PAYMENT` có dòng mới; không có bảng OTP nào mới (OTP chỉ lưu Session).

---

## Bản v4 — Hủy đơn + Quản lý phòng + Phân quyền User/Admin

### 1. Phòng "còn trống" được tính như thế nào (quy tắc chung)
- Tình trạng phòng KHÔNG lưu trong cột `ROOM.status` mà **tính từ lịch đặt**:
  phòng đã có người đặt trong khoảng [nhận, trả) khi tồn tại 1 `BOOKING_DETAIL`
  mà `BOOKING` thỏa `check_in_date < ngày_trả AND ngày_nhận < check_out_date`.
- Phòng còn trống khi `status == "Available"` VÀ không có booking trùng lịch.
  Hủy đơn xong phòng tự hiện lại "Còn trống" (không cần chạy lại SQL).
- Cột `status` chỉ còn 2 giá trị do admin đặt: `Available` (cho thuê) và
  `Maintenance` (bảo trì — không bao giờ được đặt). Code không tự đổi `status`
  khi đặt/hủy phòng.
- Công thức trùng lịch chỉ viết 1 lần trong `Models/RoomAvailability.cs`
  (`IsRoomAvailable`, `IsTypeAvailable`, `CountAvailableRooms`,
  `GetRoomBookedUntil`) và dùng chung ở mọi chỗ.

### 2. Hủy đơn đặt phòng
- **User:** vào "Đơn hàng của tôi", nút **Hủy** chỉ hiện khi đơn "Sắp tới"
  (chưa tới ngày nhận phòng), bấm → hỏi xác nhận → hủy. Giữ nguyên từ bản v2.
- **Admin:** trang Quản trị có nút **Hủy đơn** ở mỗi dòng đặt phòng, hủy được
  đơn của **bất kỳ user nào**, kể cả đơn đã tới ngày nhận phòng. Sau khi hủy,
  phòng hiện lại "Còn trống" ngay.

### 3. Trang quản trị (`/Admin/Index`, chỉ admin)
- **Thống kê theo từng loại phòng:** Tổng số phòng / Còn trống / Đang có khách
  (tính theo hôm nay), kèm ghi chú "phòng 102 đến 20/11/2026".
- **Bảng tất cả phòng:** số phòng, loại phòng, khách sạn, tầng, trạng thái
  ("Còn trống" / "Đang có khách đến dd/MM/yyyy" / "Bảo trì"), nút **Sửa** /
  **Xóa**, bộ lọc theo khách sạn / loại phòng / trạng thái, nút **+ Thêm phòng mới**.
- **Danh sách đặt phòng:** mã đơn, tên khách, email, khách sạn, loại phòng +
  số phòng, ngày nhận/trả, trạng thái đơn (Sắp tới / Đang ở / Đã trả phòng),
  tổng tiền, nút **Hủy đơn**.
- **Thêm phòng:** chọn loại phòng, nhập số phòng + tầng. Số phòng không được
  trùng trong cùng 1 khách sạn (được trùng ở khách sạn khác).
- **Sửa phòng:** đổi loại phòng, số phòng, tầng, trạng thái (chỉ `Available` /
  `Maintenance`). **Xóa phòng:** bị chặn khi phòng đang có khách đặt chưa tới
  ngày trả ("Không thể xóa phòng X vì đang có khách đặt đến ngày ...").
- Sửa giá loại phòng (`EditPrice`) giữ nguyên như cũ.

### 4. Trang xem phòng của user — chỉ hiện phòng còn trống
- Trang `/Rooms/Index`: mỗi loại phòng hiện **"Còn X/Y phòng trống"** (X = còn
  trống, Y = tổng số phòng). Loại nào hết phòng thì hiện mờ kèm chữ **"Hết phòng"**,
  không bấm được. Chọn ngày nhận/trả thì chỉ hiện loại còn trống trong khoảng
  ngày đó.
- Trang `/Rooms/Details`: chỉ liệt kê phòng còn trống; phòng đã có khách thì
  hiện ghi chú "Phòng 102 đã có khách đến 20/11/2026". Hết phòng thì nút
  "Đặt phòng này" biến thành chữ "Hết phòng trong khoảng ngày đã chọn."
- Trang chủ: mỗi thẻ phòng hiện "Còn X/Y phòng trống" (tính theo hôm nay);
  loại hết phòng thì khóa nút "Chọn phòng" và "Xem chi tiết".
- Form đặt phòng: đổi ngày thì các lựa chọn loại phòng đã hết sẽ bị khóa
  (hỏi server `/Rooms/Available`); lúc submit, server vẫn kiểm tra lại và báo
  "Hiện không còn phòng ... trong khoảng ngày bạn chọn." nếu đã hết phòng.

### 5. Phân quyền User / Admin
- **User (`demo@grandhorizon.vn / 123456`):** chỉ xem phòng còn trống, đặt
  phòng, xem "Đơn hàng của tôi", hủy đơn của mình (từ bản v5: hủy được đến
  khi trả phòng, kể cả đơn nhận hôm nay). Menu không có link "Quản trị".
- **Admin (`admin@grandhorizon.vn / 123456`):** CHỈ quản lý phòng (xem thống kê,
  thêm/sửa/xóa phòng, sửa giá, hủy đơn bất kỳ ai, xem danh sách đặt phòng) —
  từ bản v5 admin KHÔNG đặt phòng được nữa (menu không còn "Đặt phòng",
  "Đặt phòng ngay", "Đơn hàng của tôi"; gõ thẳng `/Booking/Create` cũng bị
  chặn về trang Quản trị). Menu hiện thêm link **"Quản trị"** (tới `/Admin/Index`).
- Phân biệt bằng email đăng nhập (`Session["GuestEmail"] ==
  admin@grandhorizon.vn`, đúng như code cũ trong `AdminController.IsAdmin()`).
  Mọi Action trong `AdminController` đều kiểm tra lại ở server — user gõ thẳng
  URL `/Admin/...` hoặc gửi POST tới `/Admin/DeleteRoom` cũng bị chặn về
  trang Login. Admin vẫn đăng nhập + OTP như tài khoản thường.

### 6. Cách test bản v4
1. Chạy `database.sql` trong SSMS (nếu chưa chạy bản v2), Restore NuGet Packages, Build (Ctrl+Shift+B), F5.
2. Đăng nhập user demo, đặt phòng Standard ngày mai → 2 ngày: ghi lại số phòng
   được giao (ví dụ 101). Vào `/Rooms/Index` và `/Rooms/Details/1` — phòng 101
   phải biến mất khỏi danh sách phòng trống; loại Standard phải giảm 1 ở số
   "Còn trống". Vào trang chủ — thẻ Standard phải giảm 1.
3. Vào "Đơn hàng của tôi" bấm Hủy đơn vừa đặt → đơn biến mất, phòng 101 hiện
   lại "Còn trống" ngay (không cần chạy lại SQL).
4. Đăng nhập admin, vào `/Admin/Index`: có bảng thống kê, bảng tất cả phòng
   (Sửa/Xóa), danh sách đặt phòng (Hủy đơn). Thêm phòng 103 (Standard, tầng 1)
   → hiện trong bảng quản lý và trong trang xem phòng của user. Sửa phòng 103
   thành "Bảo trì" → biến mất khỏi trang user. Xóa phòng 101 đang có khách đặt
   → bị chặn với thông báo rõ ràng. Xóa phòng trống → xóa được.
5. Admin hủy 1 đơn của user demo → đơn biến mất khỏi "Đơn hàng của tôi" của
   user demo, phòng hiện lại "Còn trống".
6. Đăng nhập user demo (hoặc chưa đăng nhập), gõ thẳng `/Admin/Index` → bị
   chặn về trang Login; menu của user demo không có link "Quản trị", menu của
   admin có.

### 7. Bản v5 — user hủy đơn đến khi trả phòng, sửa lỗi đăng ký, admin không đặt phòng

- **Hủy đơn (user):** đổi quy tắc từ "chỉ hủy trước ngày nhận phòng" thành
  "hủy được đến khi trả phòng" (`check_out_date > hôm nay`). Đơn nhận hôm nay
  giờ hủy được (trước đây form mặc định nhận hôm nay nên user không bao giờ
  thấy nút Hủy). Nút Hủy trong "Đơn hàng của tôi" hiện đúng điều kiện mới,
  đơn đã trả phòng hiện chữ "Đã trả phòng". Thêm nút "Hủy đơn này" ở trang
  xác nhận sau khi đặt phòng. Hủy xong phòng hiện lại trống ngay (tính từ
  lịch đặt, không đổi cột status).
- **Đăng ký:** chuẩn hóa email (Trim + ToLower) trước khi kiểm tra trùng;
  chỉ báo "Email này đã có tài khoản" khi email đã có mật khẩu (email mới
  đặt phòng chưa có tài khoản thì vẫn gắn mật khẩu vào hồ sơ cũ). Sửa khối
  catch quanh `SaveChanges()`: bắt `DbUpdateException` và báo đúng lý do thật
  (trùng khóa → báo email đã có tài khoản; lỗi khác → báo kèm thông báo lỗi
  thật), không đổ mọi lỗi cho "email đã tồn tại" nữa.
- **Admin không đặt phòng:** menu của admin không còn "Đặt phòng",
  "Đặt phòng ngay", "Đơn hàng của tôi"; nút "Chọn phòng" ở trang chủ và
  "Đặt phòng này" ở chi tiết phòng cũng ẩn với admin; form đặt phòng trong
  TrangChung/TrangBooking ẩn với admin. Chặn ở server: mở `/Booking/Create`
  bằng admin → về `/Admin/Index` kèm thông báo; gửi POST đặt phòng bằng
  admin → trả về lỗi, không tạo đơn nào. Admin vẫn xem được trang Phòng /
  Dịch vụ / Điểm đến và giữ nguyên mọi quyền quản lý.
- Tài khoản chấm bài: user `demo@grandhorizon.vn / 123456`, admin
  `admin@grandhorizon.vn / 123456` (cả hai đều qua OTP khi đăng nhập).

### 8. Cách test bản v5

1. Chạy `database.sql` trong SSMS (nếu chưa chạy bản v2), Restore NuGet Packages, Build (Ctrl+Shift+B), F5.
2. Đăng nhập user demo, đặt phòng nhận HÔM NAY, trả ngày mai → vào "Đơn hàng
   của tôi" phải thấy nút Hủy (bản v4.1 không có) → bấm Hủy → đơn biến mất,
   phòng hiện lại "Còn trống" ngay. Đặt thêm 1 đơn nữa, mở trang xác nhận →
   có nút "Hủy đơn này", bấm thử cũng hủy được.
3. Đăng ký bằng email hoàn toàn mới (chưa từng đặt phòng, chưa từng đăng ký)
   → tạo được tài khoản và tự đăng nhập. Đăng ký lại đúng email đó lần nữa
   → báo "Email này đã có tài khoản. Vui lòng đăng nhập."
4. Đăng nhập admin → menu không còn "Đặt phòng", "Đặt phòng ngay",
   "Đơn hàng của tôi"; mở `/Rooms/Details/1` → không có nút "Đặt phòng này";
   gõ thẳng `/Booking/Create` → về `/Admin/Index` kèm thông báo "Tài khoản
   quản trị chỉ quản lý phòng, không đặt phòng."; trang Quản trị vẫn hiện
   thống kê, bảng phòng, danh sách đặt phòng như cũ.
5. Đăng nhập user demo → menu và nút đặt phòng hiện đầy đủ như cũ; đặt phòng,
   hủy đơn, xem "Đơn hàng của tôi" vẫn chạy.

### 9. Bản v6-v7 — Liên hệ lễ tân, thực đơn buffet, tìm kiếm theo địa chỉ, đánh giá 5 sao

- **Liên hệ lễ tân (`/Home/Contact`):** trang Liên hệ cũ vẫn là mẫu mặc định
  của MVC (One Microsoft Way, Redmond) nên được viết lại hoàn toàn. Trang mới
  lấy danh sách 7 khách sạn (tên, địa chỉ, điện thoại) từ bảng PROPERTY, và
  danh sách lễ tân từ `Models/StaffCatalog.cs` — dữ liệu mẫu cho đồ án (tên
  lễ tân, ca làm, số điện thoại), Đà Nẵng và Hà Nội đủ cả 3 ca, các khách sạn
  còn lại 1–2 ca. 3 ca cố định: Sáng 06:00–14:00, Chiều 14:00–22:00,
  Đêm 22:00–06:00 (ca đêm qua ngày, tính bằng `IsOnDuty` so sánh TimeSpan).
  Trang hiện "Lễ tân đang trực lúc HH:mm" (tính từ giờ hiện tại) rồi mới đến
  từng khách sạn với lễ tân của khách sạn đó, kèm huy hiệu "Đang trực" (xanh)
  hoặc "Ngoài giờ làm việc" (xám). Menu có thêm "Liên hệ", footer "Liên hệ"
  thành link về trang này. Chưa có chức năng admin quản lý nhân viên.
- **Thực đơn buffet (`/Buffet/Index`, quản lý `/Buffet/Manage`):** bảng mới
  `BUFFET_ITEM` (item_id, name, category, description, price, is_available)
  — KHÔNG sửa EDMX, truy cập bằng POCO `Models/BuffetItem.cs` +
  `SqlQuery`/`ExecuteSqlCommand` với `SqlParameter`. Seed 15 món thật:
  5 món chính + 5 món tráng miệng giá 0 (đã bao gồm trong giá buffet),
  5 đồ uống giá 25.000–45.000đ. Trang khách lọc theo nhóm món + tìm tên món,
  ghi rõ phục vụ 6:00–10:00 và link sang dịch vụ Buffet Sáng 4★.
  Admin thêm/sửa/xóa món tại `/Buffet/Manage` (Thêm/Sửa có checkbox
  "Đang phục vụ"; xóa hỏi confirm, xóa đúng 1 dòng). Trang Dịch vụ có link
  "Xem thực đơn Buffet →" trên thẻ Buffet Sáng. Món giá 0 hiện "Đã bao gồm".
- **Tìm kiếm theo địa chỉ:** thanh tìm kiếm (từ khóa + Tỉnh + Quận/Huyện)
  ở trang chủ (dưới thanh đặt phòng) và đầu trang Phòng (chung 1 form với
  các bộ lọc giá/khách/ngày cũ, không tách form riêng). Tỉnh chọn xong thì
  Quận/Huyện mới mở, nạp qua JSON `/Rooms/Districts?province=...` từ
  `Models/LocationCatalog.cs` (7 tỉnh của 7 khách sạn + 4–8 quận/huyện thật
  mỗi tỉnh). Địa chỉ seed hầu hết không ghi tên quận nên lọc tỉnh/quận dùng
  bảng ánh xạ khách sạn → (tỉnh, quận) trong catalog, KHÔNG lọc bằng chuỗi
  địa chỉ; từ khóa khớp tên khách sạn, địa chỉ hoặc tên loại phòng
  (không phân biệt hoa/thường). Các bộ lọc kết hợp bằng AND. Khi đang lọc
  hiện dòng "Đang lọc: ..." + link "Xóa bộ lọc"; không có kết quả thì báo
  "Không tìm thấy khách sạn/phòng phù hợp, thử đổi tỉnh/quận hoặc từ khóa
  khác." + nút "Xem tất cả phòng". Catalog chỉ gồm 7 tỉnh có khách sạn,
  không phải đủ 63 tỉnh/thành.
- **Đánh giá 5 sao:** bảng mới `REVIEW` (review_id, room_type_id, guest_id,
  rating 1–5, comment, created_date) — KHÔNG sửa EDMX, truy cập bằng POCO
  `Models/Review.cs` + `Models/RatingHelper.cs` (SQL `AVG(CAST(rating AS
  FLOAT))`, `COUNT(*)` với `SqlParameter`). Chưa ai vote thì mặc định
  **5.0 (Chưa có đánh giá)**. Điểm hiện ở thẻ phòng trang chủ, thẻ phòng
  trang Phòng, và khối lớn ở trang chi tiết phòng (trung bình 1 số lẻ,
  sao ★/☆, tổng số đánh giá). Trang chi tiết liệt kê bình luận mới nhất
  trước (chỉ họ tên, sao, ngày dd/MM/yyyy, nội dung). Form đánh giá ở cuối
  trang chi tiết: chỉ user đã đăng nhập thấy (chưa đăng nhập thấy link
  "Đăng nhập để đánh giá" quay lại đúng trang đó); admin không thấy form,
  chỉ thấy nút "Xóa bình luận" trên từng đánh giá. Form có 5 nút radio sao
  (bắt buộc — gửi không chọn sao thì báo "Vui lòng chọn số sao."), bình luận
  tùy chọn tối đa 1000 ký tự. Mỗi user chỉ có 1 đánh giá cho mỗi loại phòng:
  gửi lại thì CẬP NHẬT đánh giá cũ (sao + bình luận + ngày), không tạo dòng
  mới. Seed 7 đánh giá mẫu 4–5 sao của 3 khách (demo, Minh Anh, Quốc Bảo —
  2 khách mẫu chỉ có password_hash NULL nên không đăng nhập được).
- **Lưu ý:** điểm đánh giá đọc bảng REVIEW trong `try/catch` — nếu chưa chạy
  đoạn `database.sql` của bản v6-v7 thì trang chủ/trang Phòng vẫn chạy, điểm
  chỉ hiện mặc định 5.0 cho đến khi chạy script. Trang Buffet thì cần bảng
  `BUFFET_ITEM` đã tạo, nếu chưa chạy script sẽ báo lỗi database (chạy
  `database.sql` là hết).
- Tài khoản chấm bài: user `demo@grandhorizon.vn / 123456`, admin
  `admin@grandhorizon.vn / 123456` (cả hai đều qua OTP khi đăng nhập).

### 10. Cách test bản v6-v7

1. **Bắt buộc chạy `database.sql` trong SSMS** (tạo 2 bảng mới BUFFET_ITEM,
   REVIEW + seed 15 món + 7 đánh giá; chạy lại nhiều lần cũng không lỗi),
   Restore NuGet Packages, Build (Ctrl+Shift+B), F5.
2. Mở trang chủ → thanh tìm kiếm dưới thanh đặt phòng: chọn tỉnh "Đà Nẵng"
   → Quận/Huyện mở ra, chọn "Sơn Trà" → bấm Tìm phòng → chỉ còn phòng của
   khách sạn Đà Nẵng; thử từ khóa "Hội An" → chỉ còn phòng Hội An.
   Gõ từ khóa linh tinh "xyzabc" → báo "Không tìm thấy khách sạn/phòng phù
   hợp..." + nút "Xem tất cả phòng".
3. Mở `/Home/Contact` → thấy "Lễ tân đang trực lúc HH:mm" đúng giờ hiện tại,
   đủ 7 khách sạn, mỗi lễ tân có ca làm + số điện thoại + huy hiệu
   "Đang trực"/"Ngoài giờ làm việc".
4. Mở `/Buffet/Index` → đủ 3 nhóm món (5 món chính, 5 tráng miệng, 5 đồ
   uống); món giá 0 hiện "Đã bao gồm"; lọc nhóm "Đồ uống" và tìm "cà phê"
   đều chạy. Đăng nhập admin → `/Buffet/Manage` → Thêm món mới → Sửa giá
   → bỏ tick "Đang phục vụ" → món biến mất khỏi trang khách; Xóa món →
   hỏi confirm rồi mất khỏi bảng.
5. Mở `/Rooms/Details/1` → khối đánh giá hiện điểm trung bình (vd 5.0)
   + danh sách bình luận mẫu. Đăng nhập user demo → cuối trang có form
   5 sao: gửi không chọn sao → báo "Vui lòng chọn số sao."; chọn 4 sao +
   bình luận → gửi xong điểm trung bình đổi, bình luận của mình lên đầu
   danh sách; gửi lại lần nữa → chỉ cập nhật (không nhân đôi). Đăng nhập
   admin → không thấy form, chỉ thấy nút "Xóa bình luận" trên từng đánh giá.
6. Kiểm tra lại các chức năng cũ vẫn chạy: đăng nhập OTP (demo hiện mã trên
   trang), đặt phòng, hủy đơn đến khi trả phòng, admin không đặt phòng
   (menu admin không có "Đặt phòng").
