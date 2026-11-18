using System;

namespace LapTrinhWeb_pro.Models
{
    // Một đánh giá của khách về một loại phòng — khớp 6 cột của bảng dbo.REVIEW.
    // Class thường, KHÔNG nằm trong EDMX (không sửa HotelDb.edmx).
    // GuestFirstName / GuestLastName chỉ dùng để hiện tên người đánh giá
    // (lấy từ bảng GUEST khi truy vấn JOIN), không phải cột của bảng REVIEW.
    public class Review
    {
        public int review_id { get; set; }
        public int room_type_id { get; set; }
        public int guest_id { get; set; }
        public int rating { get; set; }
        public string comment { get; set; }
        public DateTime created_date { get; set; }
        public string GuestFirstName { get; set; }
        public string GuestLastName { get; set; }

        public string GuestName
        {
            get { return ((GuestFirstName ?? "") + " " + (GuestLastName ?? "")).Trim(); }
        }
    }
}
