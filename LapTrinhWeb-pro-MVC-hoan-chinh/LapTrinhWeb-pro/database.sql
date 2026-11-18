/*
  GRAND HORIZON HOTEL - DATABASE SETUP
  Chạy toàn bộ file này trong SQL Server Management Studio (SSMS).
  Script tạo database, bảng, khóa ngoại và dữ liệu mẫu nếu chưa tồn tại.
  Không xóa dữ liệu đang có.
*/
IF DB_ID(N'HotelDb') IS NULL
    CREATE DATABASE HotelDb;
GO
USE HotelDb;
GO

IF OBJECT_ID(N'dbo.PROPERTY', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PROPERTY (
        property_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        property_name NVARCHAR(150) NOT NULL,
        address NVARCHAR(250) NULL,
        phone NVARCHAR(20) NULL,
        membership_tier NVARCHAR(30) NULL
    );
END
GO

IF OBJECT_ID(N'dbo.GUEST', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GUEST (
        guest_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        first_name NVARCHAR(50) NOT NULL,
        last_name NVARCHAR(50) NOT NULL,
        email NVARCHAR(150) NOT NULL,
        phone NVARCHAR(20) NULL,
        membership_tier NVARCHAR(30) NULL,
        password_hash NVARCHAR(200) NULL
    );
END
ELSE IF COL_LENGTH('dbo.GUEST', 'password_hash') IS NULL
    ALTER TABLE dbo.GUEST ADD password_hash NVARCHAR(200) NULL;
GO

IF OBJECT_ID(N'dbo.ROOM_TYPE', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ROOM_TYPE (
        room_type_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        property_id INT NOT NULL,
        name NVARCHAR(100) NOT NULL,
        room_type NVARCHAR(50) NULL,
        price_per_night DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_ROOM_TYPE_PROPERTY FOREIGN KEY(property_id) REFERENCES dbo.PROPERTY(property_id)
    );
END
GO

IF OBJECT_ID(N'dbo.ROOM', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ROOM (
        room_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        room_type_id INT NOT NULL,
        room_number NVARCHAR(20) NOT NULL,
        floor INT NOT NULL,
        status NVARCHAR(20) NOT NULL,
        CONSTRAINT FK_ROOM_ROOM_TYPE FOREIGN KEY(room_type_id) REFERENCES dbo.ROOM_TYPE(room_type_id)
    );
END
GO

IF OBJECT_ID(N'dbo.BOOKING', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BOOKING (
        book_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        guest_id INT NOT NULL,
        property_id INT NOT NULL,
        check_in_date DATE NOT NULL,
        check_out_date DATE NOT NULL,
        total_amount DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_BOOKING_GUEST FOREIGN KEY(guest_id) REFERENCES dbo.GUEST(guest_id),
        CONSTRAINT FK_BOOKING_PROPERTY FOREIGN KEY(property_id) REFERENCES dbo.PROPERTY(property_id),
        CONSTRAINT CK_BOOKING_DATES CHECK(check_out_date > check_in_date)
    );
END
GO

IF OBJECT_ID(N'dbo.BOOKING_DETAIL', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BOOKING_DETAIL (
        booking_detail_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        book_id INT NOT NULL,
        room_id INT NOT NULL,
        CONSTRAINT FK_BOOKING_DETAIL_BOOKING FOREIGN KEY(book_id) REFERENCES dbo.BOOKING(book_id),
        CONSTRAINT FK_BOOKING_DETAIL_ROOM FOREIGN KEY(room_id) REFERENCES dbo.ROOM(room_id)
    );
END
GO

IF OBJECT_ID(N'dbo.GUEST_PREFERENCE', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GUEST_PREFERENCE (
        preference_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        guest_id INT NOT NULL,
        preference_category NVARCHAR(100) NOT NULL,
        CONSTRAINT FK_GUEST_PREFERENCE_GUEST FOREIGN KEY(guest_id) REFERENCES dbo.GUEST(guest_id)
    );
END
GO

IF OBJECT_ID(N'dbo.SERVICE', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SERVICE (
        service_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        service_name NVARCHAR(100) NOT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.SERVICE_BOOKING', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SERVICE_BOOKING (
        service_booking_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        book_id INT NOT NULL,
        service_id INT NOT NULL,
        CONSTRAINT FK_SERVICE_BOOKING_BOOKING FOREIGN KEY(book_id) REFERENCES dbo.BOOKING(book_id),
        CONSTRAINT FK_SERVICE_BOOKING_SERVICE FOREIGN KEY(service_id) REFERENCES dbo.SERVICE(service_id)
    );
END
GO

IF OBJECT_ID(N'dbo.PAYMENT', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PAYMENT (
        payment_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        book_id INT NOT NULL,
        amount DECIMAL(18,2) NOT NULL,
        payment_date DATETIME2 NOT NULL,
        payment_method NVARCHAR(30) NULL,
        CONSTRAINT FK_PAYMENT_BOOKING FOREIGN KEY(book_id) REFERENCES dbo.BOOKING(book_id)
    );
END
GO

/* Dữ liệu nền để trang đặt phòng có thể hoạt động ngay */
IF NOT EXISTS (SELECT 1 FROM dbo.PROPERTY WHERE property_id = 1)
BEGIN
    SET IDENTITY_INSERT dbo.PROPERTY ON;
    INSERT dbo.PROPERTY(property_id, property_name, address, phone, membership_tier)
    VALUES(1, N'Grand Horizon Hotel', N'123 Đường Biển, Đà Nẵng', N'0369436165', NULL);
    SET IDENTITY_INSERT dbo.PROPERTY OFF;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ROOM_TYPE WHERE name = N'Standard')
    INSERT dbo.ROOM_TYPE(property_id, name, room_type, price_per_night)
    VALUES(1, N'Standard', N'Standard', 1200000);
IF NOT EXISTS (SELECT 1 FROM dbo.ROOM_TYPE WHERE name = N'Deluxe')
    INSERT dbo.ROOM_TYPE(property_id, name, room_type, price_per_night)
    VALUES(1, N'Deluxe', N'Deluxe', 2200000);
IF NOT EXISTS (SELECT 1 FROM dbo.ROOM_TYPE WHERE name = N'Executive Suite')
    INSERT dbo.ROOM_TYPE(property_id, name, room_type, price_per_night)
    VALUES(1, N'Executive Suite', N'Executive Suite', 4500000);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ROOM WHERE room_number = N'101')
    INSERT dbo.ROOM(room_type_id, room_number, floor, status)
    SELECT TOP 1 room_type_id, N'101', 1, N'Available' FROM dbo.ROOM_TYPE WHERE name = N'Standard';
IF NOT EXISTS (SELECT 1 FROM dbo.ROOM WHERE room_number = N'102')
    INSERT dbo.ROOM(room_type_id, room_number, floor, status)
    SELECT TOP 1 room_type_id, N'102', 1, N'Available' FROM dbo.ROOM_TYPE WHERE name = N'Standard';
IF NOT EXISTS (SELECT 1 FROM dbo.ROOM WHERE room_number = N'201')
    INSERT dbo.ROOM(room_type_id, room_number, floor, status)
    SELECT TOP 1 room_type_id, N'201', 2, N'Available' FROM dbo.ROOM_TYPE WHERE name = N'Deluxe';
IF NOT EXISTS (SELECT 1 FROM dbo.ROOM WHERE room_number = N'202')
    INSERT dbo.ROOM(room_type_id, room_number, floor, status)
    SELECT TOP 1 room_type_id, N'202', 2, N'Available' FROM dbo.ROOM_TYPE WHERE name = N'Deluxe';
IF NOT EXISTS (SELECT 1 FROM dbo.ROOM WHERE room_number = N'301')
    INSERT dbo.ROOM(room_type_id, room_number, floor, status)
    SELECT TOP 1 room_type_id, N'301', 3, N'Available' FROM dbo.ROOM_TYPE WHERE name = N'Executive Suite';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SERVICE WHERE service_id = 1)
BEGIN
    SET IDENTITY_INSERT dbo.SERVICE ON;
    INSERT dbo.SERVICE(service_id, service_name) VALUES(1, N'Buffet Sáng');
    SET IDENTITY_INSERT dbo.SERVICE OFF;
END
IF NOT EXISTS (SELECT 1 FROM dbo.SERVICE WHERE service_id = 2)
BEGIN
    SET IDENTITY_INSERT dbo.SERVICE ON;
    INSERT dbo.SERVICE(service_id, service_name) VALUES(2, N'Đưa Đón Sân Bay');
    SET IDENTITY_INSERT dbo.SERVICE OFF;
END
IF NOT EXISTS (SELECT 1 FROM dbo.SERVICE WHERE service_id = 3)
BEGIN
    SET IDENTITY_INSERT dbo.SERVICE ON;
    INSERT dbo.SERVICE(service_id, service_name) VALUES(3, N'Spa & Massage');
    SET IDENTITY_INSERT dbo.SERVICE OFF;
END
IF NOT EXISTS (SELECT 1 FROM dbo.SERVICE WHERE service_id = 4)
BEGIN
    SET IDENTITY_INSERT dbo.SERVICE ON;
    INSERT dbo.SERVICE(service_id, service_name) VALUES(4, N'Giặt Ủi Nhanh');
    SET IDENTITY_INSERT dbo.SERVICE OFF;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GUEST_EMAIL' AND object_id = OBJECT_ID(N'dbo.GUEST'))
    CREATE INDEX IX_GUEST_EMAIL ON dbo.GUEST(email);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BOOKING_GUEST_DATES' AND object_id = OBJECT_ID(N'dbo.BOOKING'))
    CREATE INDEX IX_BOOKING_GUEST_DATES ON dbo.BOOKING(guest_id, check_in_date, check_out_date);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BOOKING_DETAIL_ROOM' AND object_id = OBJECT_ID(N'dbo.BOOKING_DETAIL'))
    CREATE INDEX IX_BOOKING_DETAIL_ROOM ON dbo.BOOKING_DETAIL(room_id);
GO


/* =====================================================================
   BẢN V2: thêm địa điểm, loại phòng, tài khoản demo / admin.
   Tất cả đều có IF NOT EXISTS / NOT EXISTS nên chạy lại nhiều lần
   cũng không tạo trùng dữ liệu. Không sửa các CREATE TABLE / seed cũ.
   ===================================================================== */

/* 6 khách sạn mới, id cố định 2-7 */
IF NOT EXISTS (SELECT 1 FROM dbo.PROPERTY WHERE property_id = 2)
BEGIN
    SET IDENTITY_INSERT dbo.PROPERTY ON;
    INSERT dbo.PROPERTY(property_id, property_name, address, phone, membership_tier)
    VALUES(2, N'Grand Horizon Sài Gòn', N'123 Nguyễn Huệ, Quận 1, TP.HCM', N'02838223344', NULL);
    SET IDENTITY_INSERT dbo.PROPERTY OFF;
END
IF NOT EXISTS (SELECT 1 FROM dbo.PROPERTY WHERE property_id = 3)
BEGIN
    SET IDENTITY_INSERT dbo.PROPERTY ON;
    INSERT dbo.PROPERTY(property_id, property_name, address, phone, membership_tier)
    VALUES(3, N'Grand Horizon Hội An', N'45 Trần Phú, Hội An', N'02353991122', NULL);
    SET IDENTITY_INSERT dbo.PROPERTY OFF;
END
IF NOT EXISTS (SELECT 1 FROM dbo.PROPERTY WHERE property_id = 4)
BEGIN
    SET IDENTITY_INSERT dbo.PROPERTY ON;
    INSERT dbo.PROPERTY(property_id, property_name, address, phone, membership_tier)
    VALUES(4, N'Grand Horizon Đà Lạt', N'12 Trần Hưng Đạo, Đà Lạt', N'02633554466', NULL);
    SET IDENTITY_INSERT dbo.PROPERTY OFF;
END
IF NOT EXISTS (SELECT 1 FROM dbo.PROPERTY WHERE property_id = 5)
BEGIN
    SET IDENTITY_INSERT dbo.PROPERTY ON;
    INSERT dbo.PROPERTY(property_id, property_name, address, phone, membership_tier)
    VALUES(5, N'Grand Horizon Nha Trang', N'78 Trần Phú, Nha Trang', N'02583556677', NULL);
    SET IDENTITY_INSERT dbo.PROPERTY OFF;
END
IF NOT EXISTS (SELECT 1 FROM dbo.PROPERTY WHERE property_id = 6)
BEGIN
    SET IDENTITY_INSERT dbo.PROPERTY ON;
    INSERT dbo.PROPERTY(property_id, property_name, address, phone, membership_tier)
    VALUES(6, N'Grand Horizon Phú Quốc', N'56 Trần Hưng Đạo, Phú Quốc', N'02973889900', NULL);
    SET IDENTITY_INSERT dbo.PROPERTY OFF;
END
IF NOT EXISTS (SELECT 1 FROM dbo.PROPERTY WHERE property_id = 7)
BEGIN
    SET IDENTITY_INSERT dbo.PROPERTY ON;
    INSERT dbo.PROPERTY(property_id, property_name, address, phone, membership_tier)
    VALUES(7, N'Grand Horizon Hà Nội', N'89 Tràng Tiền, Hà Nội', N'02438221133', NULL);
    SET IDENTITY_INSERT dbo.PROPERTY OFF;
END
GO

/* 3 loại phòng mới cho khách sạn Đà Nẵng (property 1).
   Trên database mới, 3 loại này sẽ có room_type_id 4, 5, 6 theo thứ tự
   insert; nếu database đã có loại phòng khác thì id có thể khác, trang
   chi tiết phòng vẫn tìm được bằng tên (fallback ?name=...). */
IF NOT EXISTS (SELECT 1 FROM dbo.ROOM_TYPE WHERE property_id = 1 AND name = N'Family Suite')
    INSERT dbo.ROOM_TYPE(property_id, name, room_type, price_per_night)
    VALUES(1, N'Family Suite', N'Family Suite', 3800000);
IF NOT EXISTS (SELECT 1 FROM dbo.ROOM_TYPE WHERE property_id = 1 AND name = N'Ocean View Deluxe')
    INSERT dbo.ROOM_TYPE(property_id, name, room_type, price_per_night)
    VALUES(1, N'Ocean View Deluxe', N'Ocean View Deluxe', 2800000);
IF NOT EXISTS (SELECT 1 FROM dbo.ROOM_TYPE WHERE property_id = 1 AND name = N'Presidential Suite')
    INSERT dbo.ROOM_TYPE(property_id, name, room_type, price_per_night)
    VALUES(1, N'Presidential Suite', N'Presidential Suite', 8500000);
GO

/* Đủ 6 loại phòng cho mỗi khách sạn mới (property 2-7).
   Giá giống khách sạn Đà Nẵng để dễ đối chiếu. */
DECLARE @p INT = 2;
WHILE @p <= 7
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.ROOM_TYPE WHERE property_id = @p AND name = N'Standard')
        INSERT dbo.ROOM_TYPE(property_id, name, room_type, price_per_night)
        VALUES(@p, N'Standard', N'Standard', 1200000);
    IF NOT EXISTS (SELECT 1 FROM dbo.ROOM_TYPE WHERE property_id = @p AND name = N'Deluxe')
        INSERT dbo.ROOM_TYPE(property_id, name, room_type, price_per_night)
        VALUES(@p, N'Deluxe', N'Deluxe', 2200000);
    IF NOT EXISTS (SELECT 1 FROM dbo.ROOM_TYPE WHERE property_id = @p AND name = N'Executive Suite')
        INSERT dbo.ROOM_TYPE(property_id, name, room_type, price_per_night)
        VALUES(@p, N'Executive Suite', N'Executive Suite', 4500000);
    IF NOT EXISTS (SELECT 1 FROM dbo.ROOM_TYPE WHERE property_id = @p AND name = N'Family Suite')
        INSERT dbo.ROOM_TYPE(property_id, name, room_type, price_per_night)
        VALUES(@p, N'Family Suite', N'Family Suite', 3800000);
    IF NOT EXISTS (SELECT 1 FROM dbo.ROOM_TYPE WHERE property_id = @p AND name = N'Ocean View Deluxe')
        INSERT dbo.ROOM_TYPE(property_id, name, room_type, price_per_night)
        VALUES(@p, N'Ocean View Deluxe', N'Ocean View Deluxe', 2800000);
    IF NOT EXISTS (SELECT 1 FROM dbo.ROOM_TYPE WHERE property_id = @p AND name = N'Presidential Suite')
        INSERT dbo.ROOM_TYPE(property_id, name, room_type, price_per_night)
        VALUES(@p, N'Presidential Suite', N'Presidential Suite', 8500000);
    SET @p = @p + 1;
END
GO

/* Tạo 2 phòng Available cho mỗi loại phòng chưa có phòng nào.
   Số phòng theo loại: Standard 101/102, Deluxe 201/202,
   Executive Suite 301/302, Family Suite 401/402,
   Ocean View Deluxe 501/502, Presidential Suite 601/602.
   Số phòng được phép trùng giữa các khách sạn vì ROOM không có UNIQUE
   trên room_number; điều kiện NOT EXISTS kiểm tra theo (room_type_id,
   room_number) nên chạy lại không tạo trùng. */
INSERT INTO dbo.ROOM(room_type_id, room_number, floor, status)
SELECT rt.room_type_id,
       CAST(CASE rt.name
            WHEN N'Standard' THEN 100 WHEN N'Deluxe' THEN 200
            WHEN N'Executive Suite' THEN 300 WHEN N'Family Suite' THEN 400
            WHEN N'Ocean View Deluxe' THEN 500 ELSE 600 END + v.n AS NVARCHAR(10)),
       CASE rt.name
            WHEN N'Standard' THEN 1 WHEN N'Deluxe' THEN 2
            WHEN N'Executive Suite' THEN 3 WHEN N'Family Suite' THEN 4
            WHEN N'Ocean View Deluxe' THEN 5 ELSE 6 END,
       N'Available'
FROM dbo.ROOM_TYPE rt
CROSS JOIN (VALUES(1),(2)) AS v(n)
WHERE NOT EXISTS (SELECT 1 FROM dbo.ROOM r WHERE r.room_type_id = rt.room_type_id);
GO

/* Tài khoản demo + admin. password_hash là SHA256 của "123456":
   8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92
   AccountController sẽ tự nâng cấp lên PBKDF2 ở lần đăng nhập đầu tiên. */
IF NOT EXISTS (SELECT 1 FROM dbo.GUEST WHERE email = N'demo@grandhorizon.vn')
    INSERT dbo.GUEST(first_name, last_name, email, phone, membership_tier, password_hash)
    VALUES(N'Nguyễn Gia', N'Bảo', N'demo@grandhorizon.vn', N'0369436165', N'Standard',
           N'8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92');
IF NOT EXISTS (SELECT 1 FROM dbo.GUEST WHERE email = N'admin@grandhorizon.vn')
    INSERT dbo.GUEST(first_name, last_name, email, phone, membership_tier, password_hash)
    VALUES(N'Quản', N'Trị', N'admin@grandhorizon.vn', N'0900000000', N'Standard',
           N'8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92');
GO

PRINT N'Grand Horizon Hotel database is ready.';
GO

/* ================= BẢN V6-V7: THỰC ĐƠN BUFFET + ĐÁNH GIÁ =================
   Hai bảng mới DUY NHẤT của bản v6-v7. Mọi đoạn dưới đây đều có guard
   IF NOT EXISTS nên chạy lại file này nhiều lần cũng không lỗi.
   Truy cập 2 bảng này bằng POCO + SqlQuery/ExecuteSqlCommand (KHÔNG sửa EDMX). */

/* --- Bảng BUFFET_ITEM: món trong thực đơn buffet ---
   category chỉ nhận: 'Món chính', 'Món tráng miệng', 'Đồ uống'.
   price = 0 nghĩa là món đã bao gồm trong giá buffet. */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'BUFFET_ITEM')
BEGIN
    CREATE TABLE dbo.BUFFET_ITEM (
        item_id INT IDENTITY(1,1) PRIMARY KEY,
        name NVARCHAR(100) NOT NULL,
        category NVARCHAR(30) NOT NULL,
        description NVARCHAR(500) NULL,
        price DECIMAL(18,2) NOT NULL DEFAULT 0,
        is_available BIT NOT NULL DEFAULT 1
    );
END
GO

/* Món chính (đã bao gồm trong giá buffet) */
IF NOT EXISTS (SELECT 1 FROM dbo.BUFFET_ITEM WHERE name = N'Phở Bò Tái Nạm')
    INSERT dbo.BUFFET_ITEM(name, category, description, price, is_available)
    VALUES(N'Phở Bò Tái Nạm', N'Món chính', N'Phở bò truyền thống, nước dùng ninh từ xương bò, thịt tái nạm mềm.', 0, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.BUFFET_ITEM WHERE name = N'Bún Chả Hà Nội')
    INSERT dbo.BUFFET_ITEM(name, category, description, price, is_available)
    VALUES(N'Bún Chả Hà Nội', N'Món chính', N'Chả nướng than hoa, nước chấm chua ngọt, ăn kèm bún và rau sống.', 0, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.BUFFET_ITEM WHERE name = N'Cơm Gà Hội An')
    INSERT dbo.BUFFET_ITEM(name, category, description, price, is_available)
    VALUES(N'Cơm Gà Hội An', N'Món chính', N'Cơm nấu nước luộc gà, gà xé phay, hành phi và đu đủ chua.', 0, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.BUFFET_ITEM WHERE name = N'Bánh Mì Thịt Nướng')
    INSERT dbo.BUFFET_ITEM(name, category, description, price, is_available)
    VALUES(N'Bánh Mì Thịt Nướng', N'Món chính', N'Bánh mì giòn, thịt nướng, pate, dưa chua và ngò rí.', 0, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.BUFFET_ITEM WHERE name = N'Gỏi Cuốn Tôm Thịt')
    INSERT dbo.BUFFET_ITEM(name, category, description, price, is_available)
    VALUES(N'Gỏi Cuốn Tôm Thịt', N'Món chính', N'Gỏi cuốn tôm thịt, bún, rau thơm, chấm tương đậu phộng.', 0, 1);
/* Món tráng miệng (đã bao gồm trong giá buffet) */
IF NOT EXISTS (SELECT 1 FROM dbo.BUFFET_ITEM WHERE name = N'Chè Ba Màu')
    INSERT dbo.BUFFET_ITEM(name, category, description, price, is_available)
    VALUES(N'Chè Ba Màu', N'Món tráng miệng', N'Chè đậu xanh, đậu đỏ, thạch dừa, nước cốt dừa béo ngậy.', 0, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.BUFFET_ITEM WHERE name = N'Bánh Flan Caramel')
    INSERT dbo.BUFFET_ITEM(name, category, description, price, is_available)
    VALUES(N'Bánh Flan Caramel', N'Món tráng miệng', N'Bánh flan mềm mịn, caramel đắng nhẹ, ăn lạnh.', 0, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.BUFFET_ITEM WHERE name = N'Trái Cây Theo Mùa')
    INSERT dbo.BUFFET_ITEM(name, category, description, price, is_available)
    VALUES(N'Trái Cây Theo Mùa', N'Món tráng miệng', N'Trái cây tươi theo mùa: xoài, thanh long, dưa hấu, chôm chôm.', 0, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.BUFFET_ITEM WHERE name = N'Bánh Ngọt Pháp')
    INSERT dbo.BUFFET_ITEM(name, category, description, price, is_available)
    VALUES(N'Bánh Ngọt Pháp', N'Món tráng miệng', N'Bánh ngọt kiểu Pháp: su kem, tart trứng, bánh sừng bò mini.', 0, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.BUFFET_ITEM WHERE name = N'Chè Đậu Xanh')
    INSERT dbo.BUFFET_ITEM(name, category, description, price, is_available)
    VALUES(N'Chè Đậu Xanh', N'Món tráng miệng', N'Chè đậu xanh đánh nhuyễn, nước cốt dừa, ăn nóng hoặc lạnh.', 0, 1);
/* Đồ uống (tính giá riêng) */
IF NOT EXISTS (SELECT 1 FROM dbo.BUFFET_ITEM WHERE name = N'Cà Phê Phin')
    INSERT dbo.BUFFET_ITEM(name, category, description, price, is_available)
    VALUES(N'Cà Phê Phin', N'Đồ uống', N'Cà phê phin truyền thống, đậm vị, uống nóng hoặc đá.', 25000, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.BUFFET_ITEM WHERE name = N'Nước Cam Ép')
    INSERT dbo.BUFFET_ITEM(name, category, description, price, is_available)
    VALUES(N'Nước Cam Ép', N'Đồ uống', N'Cam tươi ép mỗi sáng, không đường, giàu vitamin C.', 35000, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.BUFFET_ITEM WHERE name = N'Trà Sen')
    INSERT dbo.BUFFET_ITEM(name, category, description, price, is_available)
    VALUES(N'Trà Sen', N'Đồ uống', N'Trà ướp sen thơm nhẹ, uống nóng, thanh mát.', 30000, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.BUFFET_ITEM WHERE name = N'Sinh Tố Bơ')
    INSERT dbo.BUFFET_ITEM(name, category, description, price, is_available)
    VALUES(N'Sinh Tố Bơ', N'Đồ uống', N'Sinh tố bơ sáp, sữa tươi, đá xay — béo ngậy, no lâu.', 45000, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.BUFFET_ITEM WHERE name = N'Nước Dừa Tươi')
    INSERT dbo.BUFFET_ITEM(name, category, description, price, is_available)
    VALUES(N'Nước Dừa Tươi', N'Đồ uống', N'Dừa xiêm tươi chặt tại chỗ, ngọt thanh, giải khát.', 30000, 1);
GO

/* --- Bảng REVIEW: đánh giá của khách về loại phòng ---
   rating chỉ nhận 1–5 (CHECK). Mỗi user chỉ có 1 đánh giá cho mỗi loại
   phòng — code ReviewsController kiểm tra trước khi insert. */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'REVIEW')
BEGIN
    CREATE TABLE dbo.REVIEW (
        review_id INT IDENTITY(1,1) PRIMARY KEY,
        room_type_id INT NOT NULL,
        guest_id INT NOT NULL,
        rating INT NOT NULL CHECK (rating BETWEEN 1 AND 5),
        comment NVARCHAR(1000) NULL,
        created_date DATETIME NOT NULL DEFAULT GETDATE(),
        FOREIGN KEY (room_type_id) REFERENCES dbo.ROOM_TYPE(room_type_id),
        FOREIGN KEY (guest_id) REFERENCES dbo.GUEST(guest_id)
    );
END
GO

/* Khách mẫu cho các đánh giá seed (chỉ là khách đã từng đặt phòng, chưa có
   tài khoản — password_hash NULL nên không đăng nhập được, giống khách vãng
   lai trong các bản trước). */
IF NOT EXISTS (SELECT 1 FROM dbo.GUEST WHERE email = N'minhanh@gmail.com')
    INSERT dbo.GUEST(first_name, last_name, email, phone, membership_tier, password_hash)
    VALUES(N'Minh', N'Anh', N'minhanh@gmail.com', N'0901234567', N'Standard', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.GUEST WHERE email = N'quocbao@gmail.com')
    INSERT dbo.GUEST(first_name, last_name, email, phone, membership_tier, password_hash)
    VALUES(N'Quốc', N'Bảo', N'quocbao@gmail.com', N'0907654321', N'Standard', NULL);
GO

/* Đánh giá mẫu 4–5 sao. Tìm room_type_id theo TÊN loại phòng (không ghi cứng
   id, vì id 4–6 phụ thuộc thứ tự seed). Mỗi khách mỗi loại phòng chỉ 1 dòng. */
IF NOT EXISTS (SELECT 1 FROM dbo.REVIEW r INNER JOIN dbo.GUEST g ON g.guest_id = r.guest_id INNER JOIN dbo.ROOM_TYPE rt ON rt.room_type_id = r.room_type_id WHERE rt.name = N'Standard' AND g.email = N'demo@grandhorizon.vn')
    INSERT INTO dbo.REVIEW(room_type_id, guest_id, rating, comment)
    SELECT rt.room_type_id, g.guest_id, 5, N'Phòng sạch, view biển đẹp, nhân viên rất nhiệt tình.'
    FROM dbo.ROOM_TYPE rt CROSS JOIN dbo.GUEST g
    WHERE rt.name = N'Standard' AND g.email = N'demo@grandhorizon.vn';
IF NOT EXISTS (SELECT 1 FROM dbo.REVIEW r INNER JOIN dbo.GUEST g ON g.guest_id = r.guest_id INNER JOIN dbo.ROOM_TYPE rt ON rt.room_type_id = r.room_type_id WHERE rt.name = N'Deluxe' AND g.email = N'demo@grandhorizon.vn')
    INSERT INTO dbo.REVIEW(room_type_id, guest_id, rating, comment)
    SELECT rt.room_type_id, g.guest_id, 5, N'Giường êm, yên tĩnh, đáng tiền cho kỳ nghỉ gia đình.'
    FROM dbo.ROOM_TYPE rt CROSS JOIN dbo.GUEST g
    WHERE rt.name = N'Deluxe' AND g.email = N'demo@grandhorizon.vn';
IF NOT EXISTS (SELECT 1 FROM dbo.REVIEW r INNER JOIN dbo.GUEST g ON g.guest_id = r.guest_id INNER JOIN dbo.ROOM_TYPE rt ON rt.room_type_id = r.room_type_id WHERE rt.name = N'Deluxe' AND g.email = N'minhanh@gmail.com')
    INSERT INTO dbo.REVIEW(room_type_id, guest_id, rating, comment)
    SELECT rt.room_type_id, g.guest_id, 4, N'Phòng đẹp, buffet sáng ngon, sẽ quay lại.'
    FROM dbo.ROOM_TYPE rt CROSS JOIN dbo.GUEST g
    WHERE rt.name = N'Deluxe' AND g.email = N'minhanh@gmail.com';
IF NOT EXISTS (SELECT 1 FROM dbo.REVIEW r INNER JOIN dbo.GUEST g ON g.guest_id = r.guest_id INNER JOIN dbo.ROOM_TYPE rt ON rt.room_type_id = r.room_type_id WHERE rt.name = N'Executive Suite' AND g.email = N'quocbao@gmail.com')
    INSERT INTO dbo.REVIEW(room_type_id, guest_id, rating, comment)
    SELECT rt.room_type_id, g.guest_id, 5, N'Suite rộng, view toàn cảnh tuyệt vời, đáng đồng tiền.'
    FROM dbo.ROOM_TYPE rt CROSS JOIN dbo.GUEST g
    WHERE rt.name = N'Executive Suite' AND g.email = N'quocbao@gmail.com';
IF NOT EXISTS (SELECT 1 FROM dbo.REVIEW r INNER JOIN dbo.GUEST g ON g.guest_id = r.guest_id INNER JOIN dbo.ROOM_TYPE rt ON rt.room_type_id = r.room_type_id WHERE rt.name = N'Family Suite' AND g.email = N'minhanh@gmail.com')
    INSERT INTO dbo.REVIEW(room_type_id, guest_id, rating, comment)
    SELECT rt.room_type_id, g.guest_id, 5, N'Gia đình mình rất thích, trẻ con chơi cả ngày ở hồ bơi.'
    FROM dbo.ROOM_TYPE rt CROSS JOIN dbo.GUEST g
    WHERE rt.name = N'Family Suite' AND g.email = N'minhanh@gmail.com';
IF NOT EXISTS (SELECT 1 FROM dbo.REVIEW r INNER JOIN dbo.GUEST g ON g.guest_id = r.guest_id INNER JOIN dbo.ROOM_TYPE rt ON rt.room_type_id = r.room_type_id WHERE rt.name = N'Ocean View Deluxe' AND g.email = N'demo@grandhorizon.vn')
    INSERT INTO dbo.REVIEW(room_type_id, guest_id, rating, comment)
    SELECT rt.room_type_id, g.guest_id, 4, N'View biển trực diện, mở cửa là thấy biển. Bữa sáng hơi đông.'
    FROM dbo.ROOM_TYPE rt CROSS JOIN dbo.GUEST g
    WHERE rt.name = N'Ocean View Deluxe' AND g.email = N'demo@grandhorizon.vn';
IF NOT EXISTS (SELECT 1 FROM dbo.REVIEW r INNER JOIN dbo.GUEST g ON g.guest_id = r.guest_id INNER JOIN dbo.ROOM_TYPE rt ON rt.room_type_id = r.room_type_id WHERE rt.name = N'Presidential Suite' AND g.email = N'quocbao@gmail.com')
    INSERT INTO dbo.REVIEW(room_type_id, guest_id, rating, comment)
    SELECT rt.room_type_id, g.guest_id, 5, N'Dịch vụ quản gia chu đáo, xứng đáng 5 sao.'
    FROM dbo.ROOM_TYPE rt CROSS JOIN dbo.GUEST g
    WHERE rt.name = N'Presidential Suite' AND g.email = N'quocbao@gmail.com';
GO

PRINT N'Grand Horizon Hotel database v6-v7 is ready (BUFFET_ITEM + REVIEW).';
