using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using LapTrinhWeb_pro.Models;

namespace LapTrinhWeb_pro.Controllers
{
    public class RoomsController : Controller
    {
        private readonly HotelDbEntities db = new HotelDbEntities();

        // GET /Rooms/Index?property=2&minPrice=1000000&maxPrice=3000000&guests=3&checkin=...&checkout=...
        // "property" nhận id (vd: 2) hoặc tên khách sạn (vd: "Hội An").
        // Bản v6-v7: thêm tìm kiếm theo địa chỉ — từ khóa q (tên khách sạn
        // hoặc địa chỉ), tỉnh province, quận/huyện district. Các bộ lọc kết
        // hợp với nhau bằng AND. Lọc tỉnh/quận dùng bảng ánh xạ trong
        // LocationCatalog (địa chỉ seed hầu hết không ghi tên quận nên không
        // thể lọc bằng chuỗi địa chỉ).
        public ActionResult Index(string property, decimal? minPrice, decimal? maxPrice,
            int? guests, DateTime? checkin, DateTime? checkout,
            string q, string province, string district)
        {
            var types = db.ROOM_TYPE.Include(r => r.PROPERTY).ToList();

            if (!string.IsNullOrWhiteSpace(q))
            {
                string key = q.Trim().ToLower();
                types = types.Where(t =>
                    (t.PROPERTY != null &&
                        (((t.PROPERTY.property_name ?? "").ToLower().Contains(key)) ||
                         ((t.PROPERTY.address ?? "").ToLower().Contains(key)))) ||
                    t.name.ToLower().Contains(key)).ToList();
            }
            if (!string.IsNullOrWhiteSpace(province))
            {
                var propIds = LocationCatalog.PropertyIdsInProvince(province.Trim());
                types = types.Where(t => propIds.Contains(t.property_id)).ToList();
            }
            if (!string.IsNullOrWhiteSpace(district))
            {
                var propIds = LocationCatalog.PropertyIdsInDistrict(
                    province != null ? province.Trim() : null, district.Trim());
                types = types.Where(t => propIds.Contains(t.property_id)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(property))
            {
                int propertyId;
                if (int.TryParse(property.Trim(), out propertyId))
                    types = types.Where(t => t.property_id == propertyId).ToList();
                else
                    types = types.Where(t => t.PROPERTY != null &&
                        t.PROPERTY.property_name.ToLower().Contains(property.Trim().ToLower())).ToList();
            }
            if (minPrice.HasValue)
                types = types.Where(t => t.price_per_night >= minPrice.Value).ToList();
            if (maxPrice.HasValue)
                types = types.Where(t => t.price_per_night <= maxPrice.Value).ToList();
            if (guests.HasValue)
                types = types.Where(t => RoomCatalog.ByName(t.name).MaxGuests >= guests.Value).ToList();
            // Bản v4: khoảng ngày hiệu lực — ngày user chọn (nếu hợp lệ),
            // nếu không thì hôm nay -> ngày mai. Đếm số phòng còn trống của
            // từng loại để View hiện "Còn X/Y phòng trống" (xem quy tắc chung
            // trong Models/RoomAvailability.cs).
            DateTime effCheckin = DateTime.Today;
            DateTime effCheckout = DateTime.Today.AddDays(1);
            if (checkin.HasValue && checkout.HasValue && checkout.Value.Date > checkin.Value.Date)
            {
                effCheckin = checkin.Value.Date;
                effCheckout = checkout.Value.Date;
                types = types.Where(t => RoomAvailability.IsTypeAvailable(db, t.room_type_id, effCheckin, effCheckout)).ToList();
            }
            var availCounts = new Dictionary<int, int>();
            var totalCounts = new Dictionary<int, int>();
            // Bản v6-v7: điểm đánh giá trung bình của từng loại phòng
            // (chưa ai vote thì mặc định 5.0 — xem Models/RatingHelper.cs).
            var ratings = new Dictionary<int, RatingInfo>();
            foreach (var t in types)
            {
                availCounts[t.room_type_id] = RoomAvailability.CountAvailableRooms(db, t.room_type_id, effCheckin, effCheckout);
                totalCounts[t.room_type_id] = RoomAvailability.CountRooms(db, t.room_type_id);
                ratings[t.room_type_id] = RatingHelper.GetAverage(db, t.room_type_id);
            }
            ViewBag.AvailCounts = availCounts;
            ViewBag.TotalCounts = totalCounts;
            ViewBag.Ratings = ratings;
            ViewBag.Provinces = LocationCatalog.Provinces.Select(p => p.Name).ToList();
            ViewBag.Q = q;
            ViewBag.Province = province;
            ViewBag.District = district;
            ViewBag.EffCheckin = effCheckin.ToString("yyyy-MM-dd");
            ViewBag.EffCheckout = effCheckout.ToString("yyyy-MM-dd");

            ViewBag.Properties = db.PROPERTies.OrderBy(p => p.property_id).ToList();
            ViewBag.Property = property;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Guests = guests;
            ViewBag.Checkin = checkin.HasValue ? checkin.Value.ToString("yyyy-MM-dd") : "";
            ViewBag.Checkout = checkout.HasValue ? checkout.Value.ToString("yyyy-MM-dd") : "";
            return View(types.OrderBy(t => t.property_id).ThenBy(t => t.price_per_night).ToList());
        }

        // GET /Rooms/Details/1  hoặc  /Rooms/Details?name=Deluxe
        // Có thể kèm ?checkin=yyyy-MM-dd&checkout=yyyy-MM-dd để xem phòng
        // trống trong khoảng ngày đó; không kèm thì tính cho hôm nay.
        // Fallback theo tên: nếu id không tìm thấy thì thử tìm theo tên trong
        // query string, vì id 4-6 của loại phòng mới phụ thuộc thứ tự seed.
        public ActionResult Details(int? id, string name, DateTime? checkin, DateTime? checkout)
        {
            ROOM_TYPE type = null;
            if (id.HasValue)
                type = db.ROOM_TYPE.Include(r => r.PROPERTY)
                    .FirstOrDefault(r => r.room_type_id == id.Value);
            if (type == null && !string.IsNullOrWhiteSpace(name))
            {
                string key = RoomCatalog.KeyForName(name);
                type = db.ROOM_TYPE.Include(r => r.PROPERTY)
                    .FirstOrDefault(r => r.name == RoomCatalog.ByKey[key].Name ||
                                         r.room_type1 == RoomCatalog.ByKey[key].Name);
            }
            if (type == null) return HttpNotFound();

            ViewBag.Info = RoomCatalog.ByName(type.name);

            // Bản v4: chỉ liệt kê phòng CÒN TRỐNG (tính theo lịch đặt, xem
            // Models/RoomAvailability.cs). Trước đây chỉ lọc status ==
            // "Available" nên phòng đã có người đặt vẫn hiện là còn trống.
            DateTime effCheckin = DateTime.Today;
            DateTime effCheckout = DateTime.Today.AddDays(1);
            if (checkin.HasValue && checkout.HasValue && checkout.Value.Date > checkin.Value.Date)
            {
                effCheckin = checkin.Value.Date;
                effCheckout = checkout.Value.Date;
            }
            ViewBag.Checkin = effCheckin.ToString("yyyy-MM-dd");
            ViewBag.Checkout = effCheckout.ToString("yyyy-MM-dd");
            var roomsOfType = db.ROOMs
                .Where(r => r.room_type_id == type.room_type_id)
                .OrderBy(r => r.room_number).ToList();
            var roomIds = roomsOfType.Select(r => r.room_id).ToList();
            var bookedUntil = db.BOOKING_DETAIL
                .Where(d => roomIds.Contains(d.room_id) &&
                    d.BOOKING.check_in_date < effCheckout && effCheckin < d.BOOKING.check_out_date)
                .Select(d => new { d.room_id, d.BOOKING.check_out_date })
                .ToList()
                .GroupBy(x => x.room_id)
                .ToDictionary(g => g.Key, g => g.Max(x => x.check_out_date));
            ViewBag.Rooms = roomsOfType
                .Where(r => r.status == "Available" && !bookedUntil.ContainsKey(r.room_id)).ToList();
            ViewBag.BookedRooms = roomsOfType
                .Where(r => r.status == "Available" && bookedUntil.ContainsKey(r.room_id)).ToList();
            ViewBag.MaintenanceRooms = roomsOfType
                .Where(r => r.status == "Maintenance").ToList();
            ViewBag.BookedUntil = bookedUntil;
            ViewBag.Similar = db.ROOM_TYPE
                .Where(r => r.property_id == type.property_id && r.room_type_id != type.room_type_id)
                .OrderBy(r => r.price_per_night).Take(3).ToList();
            // Bản v6-v7: điểm đánh giá + danh sách bình luận của loại phòng này.
            ViewBag.Rating = RatingHelper.GetAverage(db, type.room_type_id);
            ViewBag.Reviews = RatingHelper.GetReviews(db, type.room_type_id);
            ViewBag.LoggedIn = Session["GuestId"] != null;
            ViewBag.IsAdmin = IsAdmin();
            if (Session["GuestId"] != null)
                ViewBag.MyReview = RatingHelper.GetMyReview(db, type.room_type_id, (int)Session["GuestId"]);
            return View(type);
        }

        // Bản v6-v7: kiểm tra admin giống BookingController (bản v5) — dùng
        // để ẩn form đánh giá và hiện nút Xóa bình luận cho admin.
        private bool IsAdmin()
        {
            return ((Session["GuestEmail"] as string ?? "").Trim()
                .Equals("admin@grandhorizon.vn", StringComparison.OrdinalIgnoreCase));
        }

        // GET /Rooms/Districts?province=Đà Nẵng — trả JSON danh sách
        // quận/huyện của tỉnh đã chọn (cho dropdown tìm kiếm).
        [HttpGet]
        public JsonResult Districts(string province)
        {
            var list = LocationCatalog.DistrictsOf(province ?? "");
            return Json(list.Select(d => new { name = d }).ToList(), JsonRequestBehavior.AllowGet);
        }

        // Bản v4: logic trùng lịch chỉ còn 1 chỗ duy nhất trong
        // Models/RoomAvailability.cs — hàm này chỉ gọi lại để code cũ
        // (nếu còn chỗ nào dùng) không bị vỡ.
        private bool IsTypeAvailable(int roomTypeId, DateTime checkin, DateTime checkout)
        {
            return RoomAvailability.IsTypeAvailable(db, roomTypeId, checkin, checkout);
        }

        // GET /Rooms/Available?checkin=yyyy-MM-dd&checkout=yyyy-MM-dd
        // Trả JSON số phòng còn trống của từng loại phòng — form đặt phòng
        // dùng để khóa các lựa chọn đã hết phòng khi user đổi ngày. Đây chỉ
        // là tiện ích hiển thị; lúc submit, BookingController.Create vẫn
        // kiểm tra lại ở server.
        [HttpGet]
        public JsonResult Available(DateTime? checkin, DateTime? checkout)
        {
            DateTime effCheckin = DateTime.Today;
            DateTime effCheckout = DateTime.Today.AddDays(1);
            if (checkin.HasValue && checkout.HasValue && checkout.Value.Date > checkin.Value.Date)
            {
                effCheckin = checkin.Value.Date;
                effCheckout = checkout.Value.Date;
            }
            var types = db.ROOM_TYPE.OrderBy(t => t.property_id).ThenBy(t => t.price_per_night).ToList();
            var data = types.Select(t => new
            {
                key = RoomCatalog.KeyForName(t.name),
                name = t.name,
                available = RoomAvailability.CountAvailableRooms(db, t.room_type_id, effCheckin, effCheckout),
                total = RoomAvailability.CountRooms(db, t.room_type_id)
            }).ToList();
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
