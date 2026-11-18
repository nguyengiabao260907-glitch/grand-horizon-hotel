using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using LapTrinhWeb_pro.Models;

namespace LapTrinhWeb_pro.Controllers
{
    // Thực đơn buffet. Bảng BUFFET_ITEM không nằm trong EDMX nên mọi truy
    // cập đều dùng db.Database.SqlQuery / ExecuteSqlCommand với SqlParameter.
    public class BuffetController : Controller
    {
        private readonly HotelDbEntities db = new HotelDbEntities();

        private static readonly string[] Categories =
            { "Món chính", "Món tráng miệng", "Đồ uống" };

        // Bản v6-v7: kiểm tra admin giống BookingController (bản v5).
        private bool IsAdmin()
        {
            return ((Session["GuestEmail"] as string ?? "").Trim()
                .Equals("admin@grandhorizon.vn", StringComparison.OrdinalIgnoreCase));
        }

        private List<BuffetItem> GetItems(bool onlyAvailable)
        {
            string sql = "SELECT item_id, name, category, description, price, is_available " +
                "FROM dbo.BUFFET_ITEM" +
                (onlyAvailable ? " WHERE is_available = 1" : "") +
                " ORDER BY CASE category WHEN N'Món chính' THEN 1 " +
                "WHEN N'Món tráng miệng' THEN 2 ELSE 3 END, name";
            return db.Database.SqlQuery<BuffetItem>(sql).ToList();
        }

        private BuffetItem GetItem(int id)
        {
            return db.Database.SqlQuery<BuffetItem>(
                "SELECT item_id, name, category, description, price, is_available " +
                "FROM dbo.BUFFET_ITEM WHERE item_id = @id",
                new SqlParameter("@id", id)).FirstOrDefault();
        }

        // GET /Buffet/Index?category=Món chính&q=phở — trang cho khách.
        [HttpGet]
        public ActionResult Index(string category, string q)
        {
            var items = GetItems(true);
            if (!string.IsNullOrWhiteSpace(category) &&
                Categories.Any(c => c.Equals(category.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                string key = category.Trim();
                items = items.Where(i => i.category.Equals(key, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                string key = q.Trim().ToLower();
                items = items.Where(i =>
                    (i.name ?? "").ToLower().Contains(key) ||
                    (i.description ?? "").ToLower().Contains(key)).ToList();
            }
            ViewBag.Categories = Categories;
            ViewBag.Category = category;
            ViewBag.Q = q;
            return View(items);
        }

        // GET /Buffet/Manage — chỉ admin: bảng tất cả món, kể cả món đang ẩn.
        [HttpGet]
        public ActionResult Manage()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View(GetItems(false));
        }

        [HttpGet]
        public ActionResult Add()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.Categories = Categories;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(string name, string category, string description, decimal? price, bool isAvailable)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            name = (name ?? "").Trim();
            category = (category ?? "").Trim();
            description = (description ?? "").Trim();
            ViewBag.Categories = Categories;
            string error = ValidateItem(name, category, price);
            if (error != null)
            {
                ViewBag.Error = error;
                return View();
            }
            db.Database.ExecuteSqlCommand(
                "INSERT INTO dbo.BUFFET_ITEM(name, category, description, price, is_available) " +
                "VALUES(@name, @category, @description, @price, @available)",
                new SqlParameter("@name", name),
                new SqlParameter("@category", category),
                new SqlParameter("@description", (object)description ?? DBNull.Value),
                new SqlParameter("@price", price.Value),
                new SqlParameter("@available", isAvailable));
            TempData["AdminMsg"] = "Đã thêm món \"" + name + "\" vào thực đơn.";
            return RedirectToAction("Manage");
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var item = GetItem(id);
            if (item == null) return HttpNotFound();
            ViewBag.Categories = Categories;
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, string name, string category, string description, decimal? price, bool isAvailable)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var item = GetItem(id);
            if (item == null) return HttpNotFound();
            name = (name ?? "").Trim();
            category = (category ?? "").Trim();
            description = (description ?? "").Trim();
            ViewBag.Categories = Categories;
            string error = ValidateItem(name, category, price);
            if (error != null)
            {
                ViewBag.Error = error;
                return View(item);
            }
            db.Database.ExecuteSqlCommand(
                "UPDATE dbo.BUFFET_ITEM SET name = @name, category = @category, " +
                "description = @description, price = @price, is_available = @available " +
                "WHERE item_id = @id",
                new SqlParameter("@name", name),
                new SqlParameter("@category", category),
                new SqlParameter("@description", (object)description ?? DBNull.Value),
                new SqlParameter("@price", price.Value),
                new SqlParameter("@available", isAvailable),
                new SqlParameter("@id", id));
            TempData["AdminMsg"] = "Đã cập nhật món \"" + name + "\".";
            return RedirectToAction("Manage");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var item = GetItem(id);
            if (item == null) return HttpNotFound();
            db.Database.ExecuteSqlCommand(
                "DELETE FROM dbo.BUFFET_ITEM WHERE item_id = @id",
                new SqlParameter("@id", id));
            TempData["AdminMsg"] = "Đã xóa món \"" + item.name + "\" khỏi thực đơn.";
            return RedirectToAction("Manage");
        }

        private string ValidateItem(string name, string category, decimal? price)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Vui lòng nhập tên món.";
            if (name.Length > 100)
                return "Tên món tối đa 100 ký tự.";
            if (!Categories.Any(c => c.Equals(category ?? "", StringComparison.OrdinalIgnoreCase)))
                return "Nhóm món phải là Món chính, Món tráng miệng hoặc Đồ uống.";
            if (!price.HasValue || price.Value < 0)
                return "Giá món phải là số tiền từ 0 trở lên (0 = đã bao gồm trong giá buffet).";
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
