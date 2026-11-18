using System;
using System.Collections.Generic;
using System.Linq;

namespace LapTrinhWeb_pro.Models
{
    // Thống kê 1 loại phòng trong 1 khách sạn, dùng ở trang quản trị.
    public class RoomTypeStat
    {
        public string PropertyName { get; set; }
        public string TypeName { get; set; }
        public int Total { get; set; }
        public int Available { get; set; }
        public int Booked { get; set; }
        // Ví dụ: "phòng 102 đến 20/11/2026; phòng 201 đến 22/11/2026"
        public string BookedDetails { get; set; }
    }

    // Bản v4: MỘT chỗ duy nhất tính phòng còn trống, dùng chung cho mọi nơi
    // (trang xem phòng, trang chi tiết, trang đặt phòng, trang chủ, trang quản trị).
    //
    // Quy tắc (không đổi ở bất kỳ chỗ nào):
    // - Phòng đã có người đặt trong khoảng [nhận, trả) khi tồn tại 1
    //   BOOKING_DETAIL trỏ tới phòng đó mà BOOKING thỏa:
    //   check_in_date < ngày_trả AND ngày_nhận < check_out_date.
    // - Phòng còn trống khi status == "Available" VÀ không có booking trùng
    //   lịch như trên. Cột status KHÔNG bao giờ bị code tự đổi khi đặt/hủy
    //   phòng; nó chỉ có 2 giá trị do admin đặt: "Available" và "Maintenance".
    //
    // Lưu ý EF6: mọi truy vấn dưới đây chỉ dùng cột của bảng + biến cục bộ
    // DateTime đã tính sẵn — KHÔNG dùng .Date trong truy vấn (lỗi này đã làm
    // hỏng đặt phòng ở bản v2.1). Class này là static nhưng KHÔNG đụng tới
    // HttpContext nên không dính lỗi CS0120 như bản v3.
    public static class RoomAvailability
    {
        // Phòng còn trống trong khoảng ngày đã cho không?
        public static bool IsRoomAvailable(HotelDbEntities db, int roomId,
            DateTime checkin, DateTime checkout)
        {
            return db.ROOMs.Any(r => r.room_id == roomId && r.status == "Available" &&
                !db.BOOKING_DETAIL.Any(d => d.room_id == r.room_id &&
                    d.BOOKING.check_in_date < checkout && checkin < d.BOOKING.check_out_date));
        }

        // Loại phòng còn ít nhất 1 phòng trống trong khoảng ngày đã cho không?
        public static bool IsTypeAvailable(HotelDbEntities db, int roomTypeId,
            DateTime checkin, DateTime checkout)
        {
            return CountAvailableRooms(db, roomTypeId, checkin, checkout) > 0;
        }

        // Đếm số phòng còn trống của 1 loại phòng trong khoảng ngày đã cho.
        public static int CountAvailableRooms(HotelDbEntities db, int roomTypeId,
            DateTime checkin, DateTime checkout)
        {
            return db.ROOMs.Count(r => r.room_type_id == roomTypeId && r.status == "Available" &&
                !db.BOOKING_DETAIL.Any(d => d.room_id == r.room_id &&
                    d.BOOKING.check_in_date < checkout && checkin < d.BOOKING.check_out_date));
        }

        // Tổng số phòng của 1 loại phòng (kể cả phòng đang bảo trì).
        public static int CountRooms(HotelDbEntities db, int roomTypeId)
        {
            return db.ROOMs.Count(r => r.room_type_id == roomTypeId);
        }

        // Ngày trả phòng muộn nhất của các booking đang trùng với hôm nay.
        // Trả về null khi phòng đang trống (hoặc đang bảo trì — hàm này chỉ
        // kiểm tra lịch đặt, không kiểm tra status).
        public static DateTime? GetRoomBookedUntil(HotelDbEntities db, int roomId)
        {
            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);
            return db.BOOKING_DETAIL
                .Where(d => d.room_id == roomId &&
                    d.BOOKING.check_in_date < tomorrow && today < d.BOOKING.check_out_date)
                .Select(d => (DateTime?)d.BOOKING.check_out_date)
                .Max();
        }
    }
}
