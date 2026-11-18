using System;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using LapTrinhWeb_pro.Models;

namespace LapTrinhWeb_pro.Controllers
{
    // Đánh giá của khách về loại phòng. Bảng REVIEW không nằm trong EDMX nên
    // mọi truy cập đều dùng db.Database.SqlQuery / ExecuteSqlCommand với
    // SqlParameter. Mỗi user chỉ có ĐÚNG MỘT đánh giá cho mỗi loại phòng:
    // gửi lần nữa thì cập nhật đánh giá cũ, không tạo dòng mới.
    public class ReviewsController : Controller
    {
        private readonly HotelDbEntities db = new HotelDbEntities();

        // Bản v6-v7: kiểm tra admin giống BookingController (bản v5).
        private bool IsAdmin()
        {
            return ((Session["GuestEmail"] as string ?? "").Trim()
                .Equals("admin@grandhorizon.vn", StringComparison.OrdinalIgnoreCase));
        }

        // POST /Reviews/Create — user đã đăng nhập gửi/cập nhật đánh giá.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(int roomTypeId, int? rating, string comment)
        {
            // Chưa đăng nhập thì về Login, giữ returnUrl về trang chi tiết phòng.
            if (Session["GuestId"] == null)
            {
                string backUrl = Url.Action("Details", "Rooms", new { id = roomTypeId });
                return RedirectToAction("Login", "Account", new { returnUrl = backUrl });
            }
            // Admin không được đánh giá (admin không đặt phòng từ bản v5).
            if (IsAdmin())
                return RedirectToAction("Details", "Rooms", new { id = roomTypeId });

            var type = db.ROOM_TYPE.FirstOrDefault(r => r.room_type_id == roomTypeId);
            if (type == null) return HttpNotFound();

            comment = (comment ?? "").Trim();
            if (!rating.HasValue || rating.Value < 1 || rating.Value > 5)
            {
                TempData["ReviewError"] = "Vui lòng chọn số sao.";
                return RedirectToAction("Details", "Rooms", new { id = roomTypeId });
            }
            if (comment.Length > 1000)
            {
                TempData["ReviewError"] = "Bình luận tối đa 1000 ký tự.";
                return RedirectToAction("Details", "Rooms", new { id = roomTypeId });
            }

            int guestId = (int)Session["GuestId"];
            var existing = db.Database.SqlQuery<Review>(
                "SELECT review_id, room_type_id, guest_id, rating, comment, created_date " +
                "FROM dbo.REVIEW WHERE room_type_id = @id AND guest_id = @gid",
                new SqlParameter("@id", roomTypeId),
                new SqlParameter("@gid", guestId)).FirstOrDefault();
            if (existing == null)
            {
                db.Database.ExecuteSqlCommand(
                    "INSERT INTO dbo.REVIEW(room_type_id, guest_id, rating, comment, created_date) " +
                    "VALUES(@id, @gid, @rating, @comment, GETDATE())",
                    new SqlParameter("@id", roomTypeId),
                    new SqlParameter("@gid", guestId),
                    new SqlParameter("@rating", rating.Value),
                    new SqlParameter("@comment", (object)comment ?? DBNull.Value));
                TempData["ReviewMsg"] = "Cảm ơn bạn đã đánh giá!";
            }
            else
            {
                db.Database.ExecuteSqlCommand(
                    "UPDATE dbo.REVIEW SET rating = @rating, comment = @comment, created_date = GETDATE() " +
                    "WHERE review_id = @rid",
                    new SqlParameter("@rating", rating.Value),
                    new SqlParameter("@comment", (object)comment ?? DBNull.Value),
                    new SqlParameter("@rid", existing.review_id));
                TempData["ReviewMsg"] = "Đã cập nhật đánh giá của bạn.";
            }
            return RedirectToAction("Details", "Rooms", new { id = roomTypeId });
        }

        // POST /Reviews/Delete — chỉ admin xóa được bình luận.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, int roomTypeId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var existing = db.Database.SqlQuery<Review>(
                "SELECT review_id, room_type_id, guest_id, rating, comment, created_date " +
                "FROM dbo.REVIEW WHERE review_id = @id",
                new SqlParameter("@id", id)).FirstOrDefault();
            if (existing == null) return HttpNotFound();
            db.Database.ExecuteSqlCommand(
                "DELETE FROM dbo.REVIEW WHERE review_id = @id",
                new SqlParameter("@id", id));
            return RedirectToAction("Details", "Rooms", new { id = roomTypeId });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
