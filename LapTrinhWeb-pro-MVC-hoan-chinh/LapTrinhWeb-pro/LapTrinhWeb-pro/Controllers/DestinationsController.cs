using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using LapTrinhWeb_pro.Models;

namespace LapTrinhWeb_pro.Controllers
{
    public class DestinationsController : Controller
    {
        private readonly HotelDbEntities db = new HotelDbEntities();

        // GET /Destinations/Index — liệt kê tất cả khách sạn (PROPERTY) trong DB.
        public ActionResult Index()
        {
            var properties = db.PROPERTies.OrderBy(p => p.property_id).ToList();
            return View(properties);
        }

        // GET /Destinations/Details/3 — 1 khách sạn + các loại phòng của nó.
        public ActionResult Details(int id)
        {
            var property = db.PROPERTies.FirstOrDefault(p => p.property_id == id);
            if (property == null) return HttpNotFound();

            ViewBag.Info = DestinationCatalog.ByPropertyIdOrDefault(id);
            ViewBag.RoomTypes = db.ROOM_TYPE
                .Where(r => r.property_id == id)
                .OrderBy(r => r.price_per_night).ToList();
            return View(property);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
