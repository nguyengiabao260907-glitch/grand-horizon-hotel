using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace LapTrinhWeb_pro.Models
{
    // Điểm đánh giá trung bình của một loại phòng.
    public class RatingInfo
    {
        public double Average { get; set; }
        public int Count { get; set; }
    }

    // Hàm dùng chung cho mọi chỗ hiện điểm đánh giá. Điểm trung bình luôn
    // tính trực tiếp từ bảng REVIEW (KHÔNG lưu sẵn vào cột nào của ROOM_TYPE).
    // QUY TẮC MẶC ĐỊNH: chưa có ai vote (Count = 0) thì điểm = 5.0.
    public static class RatingHelper
    {
        private class RatingRow
        {
            public double? AvgVal { get; set; }
            public int Cnt { get; set; }
        }

        // Nếu bảng REVIEW chưa được tạo (người dùng chưa chạy đoạn
        // database.sql của bản v6-v7) thì trả về điểm mặc định thay vì làm
        // hỏng cả trang — trang chủ và trang Phòng vẫn chạy được, điểm chỉ
        // hiện "5.0 (Chưa có đánh giá)" cho đến khi chạy database.sql.
        public static RatingInfo GetAverage(HotelDbEntities db, int roomTypeId)
        {
            try
            {
                var row = db.Database.SqlQuery<RatingRow>(
                    "SELECT AVG(CAST(rating AS FLOAT)) AS AvgVal, COUNT(*) AS Cnt " +
                    "FROM dbo.REVIEW WHERE room_type_id = @id",
                    new SqlParameter("@id", roomTypeId)).FirstOrDefault();
                if (row == null || row.Cnt == 0)
                    return new RatingInfo { Average = 5.0, Count = 0 };
                return new RatingInfo { Average = row.AvgVal ?? 5.0, Count = row.Cnt };
            }
            catch (Exception)
            {
                return new RatingInfo { Average = 5.0, Count = 0 };
            }
        }

        public static List<Review> GetReviews(HotelDbEntities db, int roomTypeId)
        {
            try
            {
                return db.Database.SqlQuery<Review>(
                    "SELECT r.review_id, r.room_type_id, r.guest_id, r.rating, r.comment, r.created_date, " +
                    "g.first_name AS GuestFirstName, g.last_name AS GuestLastName " +
                    "FROM dbo.REVIEW r INNER JOIN dbo.GUEST g ON g.guest_id = r.guest_id " +
                    "WHERE r.room_type_id = @id ORDER BY r.created_date DESC",
                    new SqlParameter("@id", roomTypeId)).ToList();
            }
            catch (Exception)
            {
                return new List<Review>();
            }
        }

        public static Review GetMyReview(HotelDbEntities db, int roomTypeId, int guestId)
        {
            try
            {
                return db.Database.SqlQuery<Review>(
                    "SELECT review_id, room_type_id, guest_id, rating, comment, created_date " +
                    "FROM dbo.REVIEW WHERE room_type_id = @id AND guest_id = @gid",
                    new SqlParameter("@id", roomTypeId),
                    new SqlParameter("@gid", guestId)).FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Số sao tô đầy = làm tròn điểm trung bình, tối đa 5 sao.
        public static string StarsText(double average)
        {
            int filled = (int)Math.Round(average, MidpointRounding.AwayFromZero);
            if (filled < 0) filled = 0;
            if (filled > 5) filled = 5;
            return new string('★', filled) + new string('☆', 5 - filled);
        }

        // Dòng hiện điểm đầy đủ, dùng chung ở thẻ phòng và trang chi tiết:
        // "★★★★☆ 4.5 (12 đánh giá)" hoặc "★★★★★ 5.0 (Chưa có đánh giá)".
        public static string Line(double average, int count)
        {
            string num = average.ToString("0.0");
            if (count == 0) return StarsText(average) + " " + num + " (Chưa có đánh giá)";
            return StarsText(average) + " " + num + " (" + count + " đánh giá)";
        }

        public static string Line(RatingInfo info)
        {
            if (info == null) return Line(5.0, 0);
            return Line(info.Average, info.Count);
        }
    }
}
