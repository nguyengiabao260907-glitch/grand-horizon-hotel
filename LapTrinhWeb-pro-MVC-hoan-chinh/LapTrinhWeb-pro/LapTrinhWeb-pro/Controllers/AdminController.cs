using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using LapTrinhWeb_pro.Models;

namespace LapTrinhWeb_pro.Controllers
{
    public class AdminController : Controller
    {
        private readonly HotelDbEntities db = new HotelDbEntities();
        private const string AdminEmail = "admin@grandhorizon.vn";

        private bool IsAdmin()
        {
            string email = Session["GuestEmail"] as string;
            return !string.IsNullOrWhiteSpace(email) &&
                email.Trim().Equals(AdminEmail, StringComparison.OrdinalIgnoreCase);
        }

        // GET /Admin/Index?property=1&roomTypeId=2&status=booked
        // Trang quản trị: thống kê phòng + bảng phòng + danh sách đặt phòng.
        // Chỉ admin mới xem được.
        public ActionResult Index(int? property, int? roomTypeId, string status)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var bookings = db.BOOKINGs
                .Include(b => b.GUEST)
                .Include(b => b.PROPERTY)
                .Include(b => b.BOOKING_DETAIL.Select(d => d.ROOM.ROOM_TYPE))
                .OrderByDescending(b => b.check_in_date).ToList();

            // Bản v4: tính trạng thái phòng theo lịch đặt hôm nay (xem quy tắc
            // chung trong Models/RoomAvailability.cs). Lấy 1 lần danh sách
            // booking đang trùng với hôm nay rồi tính trong bộ nhớ — dữ liệu
            // ít nên không cần truy vấn phức tạp.
            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);
            var bookedUntil = db.BOOKING_DETAIL
                .Where(d => d.BOOKING.check_in_date < tomorrow && today < d.BOOKING.check_out_date)
                .Select(d => new { d.room_id, d.BOOKING.check_out_date })
                .ToList()
                .GroupBy(x => x.room_id)
                .ToDictionary(g => g.Key, g => g.Max(x => x.check_out_date));

            var allRooms = db.ROOMs.Include(r => r.ROOM_TYPE)
                .OrderBy(r => r.room_type_id).ThenBy(r => r.room_number).ToList();
            var properties = db.PROPERTies.OrderBy(pr => pr.property_id).ToList();
            var propertyNames = properties.ToDictionary(pr => pr.property_id, pr => pr.property_name);

            // Thống kê theo từng loại phòng trong từng khách sạn.
            var stats = new List<RoomTypeStat>();
            foreach (var type in db.ROOM_TYPE.OrderBy(r => r.property_id).ThenBy(r => r.price_per_night).ToList())
            {
                var roomsOfType = allRooms.Where(r => r.room_type_id == type.room_type_id).ToList();
                int booked = roomsOfType.Count(r => r.status == "Available" && bookedUntil.ContainsKey(r.room_id));
                var bookedDetails = roomsOfType
                    .Where(r => r.status == "Available" && bookedUntil.ContainsKey(r.room_id))
                    .Select(r => "phòng " + r.room_number + " đến " + bookedUntil[r.room_id].ToString("dd/MM/yyyy"));
                stats.Add(new RoomTypeStat
                {
                    PropertyName = propertyNames.ContainsKey(type.property_id) ? propertyNames[type.property_id] : "—",
                    TypeName = type.name,
                    Total = roomsOfType.Count,
                    Available = roomsOfType.Count(r => r.status == "Available" && !bookedUntil.ContainsKey(r.room_id)),
                    Booked = booked,
                    BookedDetails = string.Join("; ", bookedDetails)
                });
            }

            // Bộ lọc bảng phòng: khách sạn / loại phòng / trạng thái.
            IEnumerable<ROOM> rooms = allRooms;
            if (property.HasValue)
                rooms = rooms.Where(r => r.ROOM_TYPE != null && r.ROOM_TYPE.property_id == property.Value);
            if (roomTypeId.HasValue)
                rooms = rooms.Where(r => r.room_type_id == roomTypeId.Value);
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "available")
                    rooms = rooms.Where(r => r.status == "Available" && !bookedUntil.ContainsKey(r.room_id));
                else if (status == "booked")
                    rooms = rooms.Where(r => r.status == "Available" && bookedUntil.ContainsKey(r.room_id));
                else if (status == "maintenance")
                    rooms = rooms.Where(r => r.status == "Maintenance");
            }

            ViewBag.PropertyNames = propertyNames;
            ViewBag.Rooms = rooms.ToList();
            ViewBag.RoomTypes = db.ROOM_TYPE.OrderBy(r => r.property_id)
                .ThenBy(r => r.price_per_night).ToList();
            ViewBag.Properties = properties;
            ViewBag.TypeStats = stats;
            ViewBag.BookedUntil = bookedUntil;
            ViewBag.SelectedProperty = property;
            ViewBag.SelectedRoomType = roomTypeId;
            ViewBag.SelectedStatus = status ?? "";
            return View(bookings);
        }

        // POST /Admin/CancelBooking — admin hủy đơn của BẤT KỲ user nào,
        // không giới hạn điều kiện ngày (kể cả đơn đã tới ngày nhận phòng).
        // Xóa SERVICE_BOOKING, BOOKING_DETAIL, PAYMENT rồi mới xóa BOOKING,
        // trong 1 transaction — giống hệt Cancel của user. Sau khi hủy,
        // phòng tự hiện lại "Còn trống" vì tình trạng phòng tính từ lịch đặt.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelBooking(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var booking = db.BOOKINGs.FirstOrDefault(b => b.book_id == id);
            if (booking == null) return HttpNotFound();

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    db.SERVICE_BOOKING.RemoveRange(db.SERVICE_BOOKING.Where(s => s.book_id == id));
                    db.BOOKING_DETAIL.RemoveRange(db.BOOKING_DETAIL.Where(d => d.book_id == id));
                    db.PAYMENTs.RemoveRange(db.PAYMENTs.Where(p => p.book_id == id));
                    db.BOOKINGs.Remove(booking);
                    db.SaveChanges();
                    transaction.Commit();
                    TempData["AdminMsg"] = "Đã hủy đơn #" + id + ".";
                }
                catch
                {
                    transaction.Rollback();
                    TempData["AdminError"] = "Không thể hủy đơn #" + id + ". Vui lòng thử lại.";
                }
            }
            return RedirectToAction("Index");
        }

        // GET /Admin/AddRoom — form thêm phòng mới (phòng cụ thể, vd phòng 103).
        public ActionResult AddRoom()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.RoomTypes = db.ROOM_TYPE.OrderBy(r => r.property_id)
                .ThenBy(r => r.price_per_night).ToList();
            return View();
        }

        // POST /Admin/AddRoom
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddRoom(int roomTypeId, string roomNumber, int floor)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            roomNumber = (roomNumber ?? "").Trim();
            var type = db.ROOM_TYPE.FirstOrDefault(r => r.room_type_id == roomTypeId);
            if (type == null)
                ViewBag.Error = "Loại phòng không tồn tại.";
            else if (roomNumber.Length == 0)
                ViewBag.Error = "Vui lòng nhập số phòng.";
            else if (floor < 1)
                ViewBag.Error = "Tầng phải lớn hơn hoặc bằng 1.";
            // Số phòng không được trùng trong cùng 1 khách sạn (được trùng
            // ở khách sạn khác nhau).
            else if (db.ROOMs.Any(r => r.ROOM_TYPE.property_id == type.property_id &&
                r.room_number.Trim() == roomNumber))
                ViewBag.Error = "Phòng số " + roomNumber + " đã tồn tại ở khách sạn này.";

            if (ViewBag.Error != null)
            {
                ViewBag.RoomTypes = db.ROOM_TYPE.OrderBy(r => r.property_id)
                    .ThenBy(r => r.price_per_night).ToList();
                return View();
            }

            db.ROOMs.Add(new ROOM
            {
                room_type_id = roomTypeId,
                room_number = roomNumber,
                floor = floor,
                status = "Available"
            });
            db.SaveChanges();
            TempData["AdminMsg"] = "Đã thêm phòng " + roomNumber + ".";
            return RedirectToAction("Index");
        }

        // GET /Admin/EditRoom/5 — form sửa phòng cụ thể.
        public ActionResult EditRoom(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var room = db.ROOMs.Include(r => r.ROOM_TYPE).FirstOrDefault(r => r.room_id == id);
            if (room == null) return HttpNotFound();
            ViewBag.RoomTypes = db.ROOM_TYPE.OrderBy(r => r.property_id)
                .ThenBy(r => r.price_per_night).ToList();
            return View(room);
        }

        // POST /Admin/EditRoom
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditRoom(int id, int roomTypeId, string roomNumber, int floor, string status)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var room = db.ROOMs.FirstOrDefault(r => r.room_id == id);
            if (room == null) return HttpNotFound();

            roomNumber = (roomNumber ?? "").Trim();
            status = (status ?? "").Trim();
            var type = db.ROOM_TYPE.FirstOrDefault(r => r.room_type_id == roomTypeId);
            if (type == null)
                ViewBag.Error = "Loại phòng không tồn tại.";
            else if (roomNumber.Length == 0)
                ViewBag.Error = "Vui lòng nhập số phòng.";
            else if (floor < 1)
                ViewBag.Error = "Tầng phải lớn hơn hoặc bằng 1.";
            else if (status != "Available" && status != "Maintenance")
                ViewBag.Error = "Trạng thái chỉ được là Available hoặc Maintenance.";
            else if (db.ROOMs.Any(r => r.room_id != id &&
                r.ROOM_TYPE.property_id == type.property_id && r.room_number.Trim() == roomNumber))
                ViewBag.Error = "Phòng số " + roomNumber + " đã tồn tại ở khách sạn này.";

            if (ViewBag.Error != null)
            {
                ViewBag.RoomTypes = db.ROOM_TYPE.OrderBy(r => r.property_id)
                    .ThenBy(r => r.price_per_night).ToList();
                return View(room);
            }

            room.room_type_id = roomTypeId;
            room.room_number = roomNumber;
            room.floor = floor;
            room.status = status;
            db.SaveChanges();
            TempData["AdminMsg"] = "Đã cập nhật phòng " + roomNumber + ".";
            return RedirectToAction("Index");
        }

        // POST /Admin/DeleteRoom — chỉ xóa được phòng không còn booking
        // tương lai. Phòng đang có khách đặt (chưa tới ngày trả) thì chặn.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteRoom(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var room = db.ROOMs.FirstOrDefault(r => r.room_id == id);
            if (room == null) return HttpNotFound();

            DateTime today = DateTime.Today;
            DateTime? latestCheckout = db.BOOKING_DETAIL
                .Where(d => d.room_id == id && today < d.BOOKING.check_out_date)
                .Select(d => (DateTime?)d.BOOKING.check_out_date)
                .Max();
            if (latestCheckout.HasValue)
            {
                TempData["AdminError"] = "Không thể xóa phòng " + room.room_number +
                    " vì đang có khách đặt đến ngày " + latestCheckout.Value.ToString("dd/MM/yyyy") + ".";
                return RedirectToAction("Index");
            }

            db.ROOMs.Remove(room);
            db.SaveChanges();
            TempData["AdminMsg"] = "Đã xóa phòng " + room.room_number + ".";
            return RedirectToAction("Index");
        }

        // GET /Admin/EditPrice/2
        public ActionResult EditPrice(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var type = db.ROOM_TYPE.FirstOrDefault(r => r.room_type_id == id);
            if (type == null) return HttpNotFound();
            return View(type);
        }

        // POST /Admin/EditPrice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPrice(int id, decimal price)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var type = db.ROOM_TYPE.FirstOrDefault(r => r.room_type_id == id);
            if (type == null) return HttpNotFound();
            if (price <= 0)
            {
                ViewBag.Error = "Giá phòng phải lớn hơn 0.";
                return View(type);
            }
            type.price_per_night = price;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
