using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using LapTrinhWeb_pro.Models;

namespace LapTrinhWeb_pro.Controllers
{
    public class BookingController : Controller
    {
        private readonly HotelDbEntities db = new HotelDbEntities();

        // Giá dịch vụ do server xác định; không nhận giá từ trình duyệt.
        private static readonly Dictionary<int, decimal> ServicePrices = new Dictionary<int, decimal>
        {
            { 1, 250000m }, { 2, 400000m }, { 3, 600000m }, { 4, 150000m }
        };
        private static readonly HashSet<int> ServicePerNight = new HashSet<int> { 1, 4 };

        // Mã thanh toán ngắn gọn để ghi vào PAYMENT.payment_method (NVARCHAR(30)).
        // Không ghi nguyên câu tiếng Việt dài vào đây: database cũ của nhóm có
        // thể định nghĩa cột này ngắn hơn 30 ký tự, ghi dài sẽ bị lỗi truncate
        // và làm hỏng cả đặt phòng. Tiếng Việt đầy đủ chỉ hiện ở giao diện.
        private static string PaymentCode(string method)
        {
            if (string.IsNullOrWhiteSpace(method)) return null;
            if (method.Contains("Chuyển khoản")) return "BankTransfer";
            return "Cash";
        }

        // Bản v5: tài khoản quản trị là admin@grandhorizon.vn (cùng cách
        // kiểm tra với AdminController.IsAdmin). Admin chỉ quản lý phòng,
        // không đặt phòng.
        private bool IsAdmin()
        {
            return ((Session["GuestEmail"] as string ?? "").Trim()
                .Equals("admin@grandhorizon.vn", StringComparison.OrdinalIgnoreCase));
        }

        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        // GET /Booking/Create?roomTypeId=2&serviceId=1 — trang đặt phòng riêng.
        // Form ở đây submit bằng fetch tới Create (JsonResult) bên dưới, khi
        // thành công thì chuyển sang /Booking/Confirmation?id={bookId}.
        [HttpGet]
        public ActionResult Create(int? roomTypeId, string roomType, int? serviceId,
            DateTime? checkin, DateTime? checkout)
        {
            // Bản v3: đặt phòng phải đăng nhập. Chưa đăng nhập thì về trang
            // Login, kèm returnUrl để sau khi đăng nhập (và qua OTP) quay lại
            // đúng trang đặt phòng với đúng loại phòng/dịch vụ đã chọn.
            if (Session["GuestId"] == null)
            {
                string backUrl = Url.Action("Create", "Booking",
                    new { roomTypeId = roomTypeId, roomType = roomType, serviceId = serviceId, checkin = checkin, checkout = checkout });
                return RedirectToAction("Login", "Account", new { returnUrl = backUrl });
            }
            // Bản v5: tài khoản quản trị chỉ quản lý phòng, không đặt phòng.
            if (IsAdmin())
            {
                TempData["Error"] = "Tài khoản quản trị chỉ quản lý phòng, không đặt phòng.";
                return RedirectToAction("Index", "Admin");
            }
            var types = db.ROOM_TYPE.OrderBy(r => r.price_per_night).ToList();
            // Bản v4: nếu user đã chọn ngày (đi từ trang chi tiết phòng),
            // dropdown chỉ hiện loại phòng còn trống trong khoảng ngày đó.
            // Chưa chọn ngày thì hiện tất cả — lúc submit, Create POST vẫn
            // kiểm tra lại ở server.
            if (checkin.HasValue && checkout.HasValue && checkout.Value.Date > checkin.Value.Date)
            {
                DateTime ci = checkin.Value.Date, co = checkout.Value.Date;
                types = types.Where(t => RoomAvailability.IsTypeAvailable(db, t.room_type_id, ci, co)).ToList();
                ViewBag.Checkin = ci.ToString("yyyy-MM-dd");
                ViewBag.Checkout = co.ToString("yyyy-MM-dd");
            }
            ViewBag.RoomTypes = types;
            string selectedKey = "deluxe";
            if (roomTypeId.HasValue)
            {
                var t = types.FirstOrDefault(x => x.room_type_id == roomTypeId.Value);
                if (t != null) selectedKey = RoomCatalog.KeyForName(t.name);
            }
            else if (!string.IsNullOrWhiteSpace(roomType))
                selectedKey = RoomCatalog.KeyForName(roomType);
            if (!types.Any(x => RoomCatalog.KeyForName(x.name) == selectedKey))
                selectedKey = types.Count > 0 ? RoomCatalog.KeyForName(types[0].name) : "deluxe";
            ViewBag.SelectedKey = selectedKey;
            ViewBag.Services = db.SERVICEs.OrderBy(s => s.service_id).ToList();
            ViewBag.PreselectedService = serviceId;
            return View();
        }

        // GET /Booking/Confirmation?id=12 — trang xác nhận sau khi đặt phòng.
        // Không yêu cầu đăng nhập vì khách đặt phòng không cần tài khoản
        // vẫn cần xem được xác nhận ngay sau khi đặt.
        [HttpGet]
        public ActionResult Confirmation(int id)
        {
            var booking = db.BOOKINGs
                .Include(b => b.GUEST)
                .Include(b => b.BOOKING_DETAIL.Select(d => d.ROOM.ROOM_TYPE))
                .Include(b => b.SERVICE_BOOKING.Select(s => s.SERVICE))
                .FirstOrDefault(b => b.book_id == id);
            if (booking == null) return HttpNotFound();
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Create(string name, string phone, string email,
            DateTime? checkin, DateTime? checkout, string roomType, int[] services,
            string promoCode = null, string paymentMethod = null)
        {
            // Bản v3: đặt phòng phải đăng nhập. Chưa đăng nhập thì KHÔNG tạo
            // GUEST, KHÔNG lưu booking — trả về cờ requireLogin để JavaScript
            // chuyển tới trang Login (giữ returnUrl về lại trang đặt phòng).
            if (Session["GuestId"] == null)
                return Json(new
                {
                    success = false,
                    requireLogin = true,
                    loginUrl = Url.Action("Login", "Account", new { returnUrl = "/Booking/Create" })
                });
            // Bản v5: tài khoản quản trị chỉ quản lý phòng, không đặt phòng.
            if (IsAdmin())
                return Json(new { success = false, message = "Tài khoản quản trị chỉ quản lý phòng, không đặt phòng." });

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(phone))
                return Json(new { success = false, message = "Vui lòng nhập họ tên, số điện thoại và email." });

            if (!checkin.HasValue || !checkout.HasValue ||
                checkout.Value.Date <= checkin.Value.Date || checkin.Value.Date < DateTime.Today)
                return Json(new { success = false, message = "Ngày nhận phòng phải từ hôm nay và ngày trả phòng phải sau ngày nhận phòng." });

            name = name.Trim();
            email = email.Trim();
            phone = phone.Trim();
            if (!new EmailAddressAttribute().IsValid(email))
                return Json(new { success = false, message = "Địa chỉ email không hợp lệ." });
            if (name.Length > 100 || email.Length > 150 || phone.Length > 20)
                return Json(new { success = false, message = "Thông tin khách hàng vượt quá độ dài cho phép." });

            // Lấy ngày (bỏ giờ) RA NGOÀI truy vấn LINQ trước khi dùng.
            // EF6 (LINQ to Entities) không dịch được thuộc tính .Date trong
            // truy vấn: viết checkout.Value.Date / checkin.Value.Date ngay
            // trong db.ROOMs.FirstOrDefault(...) sẽ ném lỗi
            // "The specified type member 'Date' is not supported in LINQ to
            // Entities" và làm hỏng cả đặt phòng. Dùng biến cục bộ đã tính
            // sẵn thì EF chỉ cần truyền 2 giá trị DateTime vào SQL.
            DateTime checkinDate = checkin.Value.Date;
            DateTime checkoutDate = checkout.Value.Date;
            int nights = (checkoutDate - checkinDate).Days;
            string typeName;
            switch ((roomType ?? "").ToLowerInvariant())
            {
                case "standard": typeName = "Standard"; break;
                case "deluxe": typeName = "Deluxe"; break;
                case "suite":
                case "executive suite":
                case "executive": typeName = "Executive Suite"; break;
                case "family": typeName = "Family Suite"; break;
                case "ocean": typeName = "Ocean View Deluxe"; break;
                case "presidential": typeName = "Presidential Suite"; break;
                default: return Json(new { success = false, message = "Loại phòng không hợp lệ." });
            }

            // Mã giảm giá GRAND10 (không phân biệt hoa thường): giảm 10% tổng tiền.
            bool promoApplied = !string.IsNullOrWhiteSpace(promoCode) &&
                promoCode.Trim().Equals("GRAND10", StringComparison.OrdinalIgnoreCase);

            int[] serviceIds = (services ?? new int[0]).Distinct().ToArray();
            if (serviceIds.Any(id => !ServicePrices.ContainsKey(id)))
                return Json(new { success = false, message = "Dịch vụ được chọn không hợp lệ." });

            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var roomTypeEntity = db.ROOM_TYPE.FirstOrDefault(r =>
                        r.name == typeName || r.room_type1 == typeName);
                    if (roomTypeEntity == null)
                        return Json(new { success = false, message = "Chưa có loại phòng " + typeName + " trong Database." });

                    var validServiceIds = db.SERVICEs
                        .Where(s => serviceIds.Contains(s.service_id))
                        .Select(s => s.service_id).ToList();
                    if (validServiceIds.Count != serviceIds.Length)
                        return Json(new { success = false, message = "Một hoặc nhiều dịch vụ chưa được khai báo trong Database." });

                    // Bản v4: chọn phòng qua hàm dùng chung
                    // RoomAvailability.IsRoomAvailable (cùng 1 công thức
                    // trùng lịch với mọi chỗ khác) thay vì viết lại truy vấn.
                    var candidateRooms = db.ROOMs
                        .Where(r => r.room_type_id == roomTypeEntity.room_type_id && r.status == "Available")
                        .OrderBy(r => r.room_number)
                        .ToList();
                    var room = candidateRooms.FirstOrDefault(r =>
                        RoomAvailability.IsRoomAvailable(db, r.room_id, checkinDate, checkoutDate));

                    if (room == null)
                        return Json(new { success = false, message = "Hiện không còn phòng " + typeName + " trong khoảng ngày bạn chọn." });

                    // Bản v3: chỉ tài khoản đang đăng nhập mới được đặt phòng.
                    // Nhánh khách vãng lai (tìm-hoặc-tạo GUEST theo email) đã bị
                    // xóa — khách chưa đăng nhập đã bị chặn ở đầu hàm này rồi.
                    int guestId = (int)Session["GuestId"];
                    GUEST guest = db.GUESTs.FirstOrDefault(g => g.guest_id == guestId);
                    if (guest == null)
                    {
                        Session.Clear();
                        return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại." });
                    }
                    // Tài khoản đang đăng nhập là nguồn dữ liệu khách hàng đáng tin cậy.
                    name = (guest.first_name + " " + guest.last_name).Trim();
                    email = guest.email;
                    phone = guest.phone;

                    decimal total = roomTypeEntity.price_per_night * nights;
                    foreach (int id in serviceIds)
                        total += ServicePerNight.Contains(id) ? ServicePrices[id] * nights : ServicePrices[id];
                    decimal discount = 0;
                    if (promoApplied)
                    {
                        discount = total * 0.10m;
                        total -= discount;
                    }

                    var booking = new BOOKING
                    {
                        guest_id = guest.guest_id,
                        property_id = roomTypeEntity.property_id,
                        check_in_date = checkinDate,
                        check_out_date = checkoutDate,
                        total_amount = total
                    };
                    db.BOOKINGs.Add(booking);
                    db.SaveChanges();

                    db.BOOKING_DETAIL.Add(new BOOKING_DETAIL
                    {
                        book_id = booking.book_id,
                        room_id = room.room_id
                    });

                    foreach (int id in serviceIds)
                        db.SERVICE_BOOKING.Add(new SERVICE_BOOKING
                        {
                            book_id = booking.book_id,
                            service_id = id
                        });

                    db.SaveChanges();
                    transaction.Commit();

                    // Ghi nhận thanh toán SAU KHI booking đã commit xong.
                    // Trước đây đoạn này nằm trong transaction: nếu insert PAYMENT
                    // lỗi (ví dụ bảng PAYMENT chưa có, hoặc cột payment_method
                    // ngắn hơn chuỗi tiếng Việt), cả đặt phòng bị rollback và
                    // chỉ hiện thông báo chung chung. Giờ booking đã lưu chắc
                    // chắn, lỗi thanh toán chỉ ghi chú thêm, không phá booking.
                    string paymentNote = null;
                    string payCode = PaymentCode(paymentMethod);
                    if (payCode != null)
                    {
                        try
                        {
                            db.PAYMENTs.Add(new PAYMENT
                            {
                                book_id = booking.book_id,
                                amount = total,
                                payment_date = DateTime.Now,
                                payment_method = payCode
                            });
                            db.SaveChanges();
                        }
                        catch (Exception payEx)
                        {
                            paymentNote = "Đã lưu đặt phòng, nhưng chưa ghi được thanh toán: " +
                                payEx.Message +
                                (payEx.InnerException != null ? " | " + payEx.InnerException.Message : "");
                        }
                    }

                    return Json(new
                    {
                        success = true,
                        bookId = booking.book_id,
                        total = total,
                        discount = discount,
                        promoApplied = promoApplied,
                        room = room.room_number,
                        nights = nights,
                        paymentNote = paymentNote
                    });
                }
                catch (Exception ex)
                {
                    try { transaction.Rollback(); } catch { }
                    Response.StatusCode = 500;
                    // Trả về lỗi thật (detail) để chẩn đoán: trước đây catch nuốt
                    // hết exception nên màn hình chỉ hiện thông báo chung chung
                    // và không biết lỗi SQL nào. Khi sửa xong thì có thể bỏ
                    // trường detail này đi.
                    string detail = ex.Message +
                        (ex.InnerException != null ? " | " + ex.InnerException.Message : "") +
                        (ex.InnerException != null && ex.InnerException.InnerException != null
                            ? " | " + ex.InnerException.InnerException.Message : "");
                    return Json(new { success = false, message = "Không thể lưu đặt phòng. Vui lòng kiểm tra cấu hình Database và thử lại.", detail = detail });
                }
            }
        }

        // POST /Booking/Cancel — chỉ chủ booking được hủy, và chỉ khi
        // đơn chưa kết thúc (chưa qua ngày trả phòng). Xóa SERVICE_BOOKING,
        // BOOKING_DETAIL, PAYMENT rồi mới xóa BOOKING, trong 1 transaction.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Cancel(int id)
        {
            if (Session["GuestId"] == null)
                return RedirectToAction("Login", "Account");

            int guestId = (int)Session["GuestId"];
            var booking = db.BOOKINGs.FirstOrDefault(b => b.book_id == id && b.guest_id == guestId);
            if (booking == null) return HttpNotFound();
            if (booking.check_out_date.Date <= DateTime.Today)
            {
                TempData["CancelError"] = "Đơn này đã trả phòng nên không thể hủy.";
                return RedirectToAction("MyBookings");
            }

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
                }
                catch
                {
                    transaction.Rollback();
                    TempData["CancelError"] = "Không thể hủy đặt phòng. Vui lòng thử lại.";
                }
            }
            return RedirectToAction("MyBookings");
        }

        [HttpGet]
        public ActionResult MyBookings()
        {
            if (Session["GuestId"] == null)
                return RedirectToAction("Login", "Account");

            int guestId = (int)Session["GuestId"];
            var bookings = db.BOOKINGs
                .Include(b => b.PROPERTY)
                .Include(b => b.BOOKING_DETAIL.Select(d => d.ROOM.ROOM_TYPE))
                .Include(b => b.SERVICE_BOOKING.Select(s => s.SERVICE))
                .Where(b => b.guest_id == guestId)
                .OrderByDescending(b => b.check_in_date)
                .ToList();

            return View(bookings);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
