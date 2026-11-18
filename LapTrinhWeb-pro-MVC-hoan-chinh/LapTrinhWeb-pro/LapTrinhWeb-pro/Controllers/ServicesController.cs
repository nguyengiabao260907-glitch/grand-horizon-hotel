using System.Web.Mvc;
using LapTrinhWeb_pro.Models;

namespace LapTrinhWeb_pro.Controllers
{
    public class ServicesController : Controller
    {
        // GET /Services/Index — liệt kê đủ 11 dịch vụ trong ServiceCatalog.
        // 4 dịch vụ đầu có trong bảng SERVICE (DbId 1-4), 7 dịch vụ còn lại
        // chỉ hiển thị, chưa có trong DB nên không đặt kèm qua form booking.
        public ActionResult Index()
        {
            return View(ServiceCatalog.All);
        }
    }
}
