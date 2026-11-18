-- KIEM TRA DATABASE cho loi "Khong the luu dat phong" o trang /Booking/Create
-- Chay file nay trong SSMS, ket noi BAOBABY\SQLEXPRESS, database HotelDb.
-- Copy toan bo ket qua (Messages + Results) gui lai cho tro ly.
USE HotelDb;
GO
PRINT N'--- 1. Cac bang co ton tai khong? (1 = co, 0 = khong) ---';
SELECT 'BOOKING' AS Bang, CASE WHEN OBJECT_ID(N'dbo.BOOKING', N'U') IS NULL THEN 0 ELSE 1 END AS TonTai
UNION ALL SELECT 'BOOKING_DETAIL', CASE WHEN OBJECT_ID(N'dbo.BOOKING_DETAIL', N'U') IS NULL THEN 0 ELSE 1 END
UNION ALL SELECT 'SERVICE_BOOKING', CASE WHEN OBJECT_ID(N'dbo.SERVICE_BOOKING', N'U') IS NULL THEN 0 ELSE 1 END
UNION ALL SELECT 'PAYMENT', CASE WHEN OBJECT_ID(N'dbo.PAYMENT', N'U') IS NULL THEN 0 ELSE 1 END
UNION ALL SELECT 'ROOM', CASE WHEN OBJECT_ID(N'dbo.ROOM', N'U') IS NULL THEN 0 ELSE 1 END
UNION ALL SELECT 'ROOM_TYPE', CASE WHEN OBJECT_ID(N'dbo.ROOM_TYPE', N'U') IS NULL THEN 0 ELSE 1 END
UNION ALL SELECT 'GUEST', CASE WHEN OBJECT_ID(N'dbo.GUEST', N'U') IS NULL THEN 0 ELSE 1 END;
GO
PRINT N'--- 2. Cot payment_method cua PAYMENT dai bao nhieu ky tu? (NULL = khong co bang/cot) ---';
SELECT COL_LENGTH(N'dbo.PAYMENT', N'payment_method') AS DoDaiToiDa_PaymentMethod;
GO
PRINT N'--- 3. Loai phong va phong hien co ---';
SELECT room_type_id, property_id, name, price_per_night FROM dbo.ROOM_TYPE ORDER BY room_type_id;
SELECT room_id, room_type_id, room_number, floor, status FROM dbo.ROOM ORDER BY room_type_id, room_number;
GO
PRINT N'--- 4. Dich vu hien co ---';
SELECT service_id, service_name FROM dbo.SERVICE ORDER BY service_id;
GO
PRINT N'--- 5. Tai khoan dang nhap hien tai (doi email thanh email cua ban) ---';
SELECT guest_id, first_name, last_name, email, phone FROM dbo.GUEST WHERE email = N'nguyengiabao260907@gmail.com';
GO
PRINT N'--- 6. Booking da co trong khoang ngay nay -> +2 ngay (neu phong Standard da het thi se bao het phong, khong phai loi luu) ---';
SELECT b.book_id, b.check_in_date, b.check_out_date, d.room_id
FROM dbo.BOOKING b JOIN dbo.BOOKING_DETAIL d ON d.book_id = b.book_id
WHERE b.check_in_date < DATEADD(day, 2, CAST(GETDATE() AS date))
  AND CAST(GETDATE() AS date) < b.check_out_date;
GO
