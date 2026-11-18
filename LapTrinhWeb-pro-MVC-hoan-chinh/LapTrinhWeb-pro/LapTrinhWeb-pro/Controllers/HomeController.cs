using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LapTrinhWeb_pro.Models;

namespace LapTrinhWeb_pro.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            // Bản v4: số phòng còn trống hôm nay của từng loại phòng, để
            // trang chủ (layout TrangChung) hiện "Còn X/Y phòng trống" và
            // khóa nút chọn phòng khi loại đó đã hết phòng.
            using (var db = new HotelDbEntities())
            {
                DateTime today = DateTime.Today;
                DateTime tomorrow = today.AddDays(1);
                var avail = new Dictionary<int, int>();
                var total = new Dictionary<int, int>();
                var ratings = new Dictionary<int, RatingInfo>();
                foreach (var t in db.ROOM_TYPE.ToList())
                {
                    total[t.room_type_id] = Models.RoomAvailability.CountRooms(db, t.room_type_id);
                    avail[t.room_type_id] = Models.RoomAvailability.CountAvailableRooms(db, t.room_type_id, today, tomorrow);
                    // Bản v6-v7: điểm đánh giá trung bình của từng loại phòng
                    // (chưa ai vote thì mặc định 5.0 — xem Models/RatingHelper.cs).
                    ratings[t.room_type_id] = Models.RatingHelper.GetAverage(db, t.room_type_id);
                }
                ViewBag.RoomAvail = avail;
                ViewBag.RoomTotal = total;
                ViewBag.RoomRatings = ratings;
                // Bản v6-v7: danh sách tỉnh cho thanh tìm kiếm ở trang chủ.
                ViewBag.Provinces = Models.LocationCatalog.Provinces.Select(p => p.Name).ToList();
            }
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            // Bản v6-v7: trang liên hệ lễ tân — truyền danh sách khách sạn
            // từ database xuống View; danh sách lễ tân lấy từ StaffCatalog
            // (dữ liệu mẫu trong code, xem Models/StaffCatalog.cs).
            using (var db = new HotelDbEntities())
            {
                ViewBag.Properties = db.PROPERTies.OrderBy(p => p.property_id).ToList();
            }
            return View();
        }
    }
}