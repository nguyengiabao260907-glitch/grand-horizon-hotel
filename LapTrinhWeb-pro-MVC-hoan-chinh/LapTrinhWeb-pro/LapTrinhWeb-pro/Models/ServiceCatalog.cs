using System.Collections.Generic;
using System.Linq;

namespace LapTrinhWeb_pro.Models
{
    // Thông tin hiển thị của một dịch vụ khách sạn.
    // 4 dịch vụ đầu (DbId 1-4) là 4 dịch vụ đã có trong bảng SERVICE, giá tính
    // ở server phải khớp BookingController.ServicePrices. 7 dịch vụ còn lại
    // (DbId = null) chỉ là dịch vụ của khách sạn, chưa có trong bảng SERVICE
    // nên không đặt kèm qua form booking mà chỉ hiển thị trên trang Dịch vụ.
    public class ServiceInfo
    {
        public int? DbId { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public decimal Price { get; set; }
        public string Unit { get; set; }
        public bool PerNight { get; set; }
        public string Description { get; set; }
    }

    public static class ServiceCatalog
    {
        public static readonly List<ServiceInfo> All = new List<ServiceInfo>
        {
            new ServiceInfo { DbId = 1, Name = "Buffet Sáng 4★", Icon = "🍳", Price = 250000m, Unit = "đ / đêm", PerNight = true,
                Description = "Buffet sáng đa dạng món Á - Âu, phục vụ từ 6:00 đến 10:00 tại nhà hàng tầng trệt." },
            new ServiceInfo { DbId = 2, Name = "Đưa Đón Sân Bay", Icon = "🚗", Price = 400000m, Unit = "đ / chuyến", PerNight = false,
                Description = "Xe riêng đưa đón sân bay - khách sạn, tài xế đón đúng giờ bay của bạn." },
            new ServiceInfo { DbId = 3, Name = "Spa & Massage", Icon = "💆", Price = 600000m, Unit = "đ / gói", PerNight = false,
                Description = "Liệu trình spa thư giãn 60 phút với tinh dầu thiên nhiên, đặt trước để giữ khung giờ đẹp." },
            new ServiceInfo { DbId = 4, Name = "Giặt Ủi Nhanh", Icon = "👕", Price = 150000m, Unit = "đ / đêm", PerNight = true,
                Description = "Giặt ủi lấy trong ngày, trả đồ trước 18:00, áp dụng cho quần áo thường." },
            new ServiceInfo { DbId = null, Name = "Hồ Bơi Vô Cực", Icon = "🏊", Price = 0m, Unit = "miễn phí", PerNight = false,
                Description = "Hồ bơi vô cực trên tầng thượng, view biển, mở cửa 6:00 - 21:00 cho mọi khách lưu trú." },
            new ServiceInfo { DbId = null, Name = "Phòng Gym 24/7", Icon = "🏋️", Price = 0m, Unit = "miễn phí", PerNight = false,
                Description = "Phòng gym hiện đại mở cửa 24/7, có huấn luyện viên hỗ trợ theo khung giờ cố định." },
            new ServiceInfo { DbId = null, Name = "Nhà Hàng Fine Dining", Icon = "🍽️", Price = 850000m, Unit = "đ / người", PerNight = false,
                Description = "Nhà hàng fine dining với thực đơn theo mùa, phục vụ 11:00 - 22:00, nên đặt bàn trước." },
            new ServiceInfo { DbId = null, Name = "Bar Rooftop", Icon = "🍹", Price = 350000m, Unit = "đ / người", PerNight = false,
                Description = "Quầy bar trên sân thượng, cocktail và nhạc sống mỗi tối cuối tuần, view toàn thành phố." },
            new ServiceInfo { DbId = null, Name = "Phòng Họp & Hội Nghị", Icon = "💼", Price = 5000000m, Unit = "đ / ngày", PerNight = false,
                Description = "Phòng họp sức chứa 10 - 200 người, đầy đủ màn chiếu, âm thanh và tiệc trà giữa giờ." },
            new ServiceInfo { DbId = null, Name = "Trông Trẻ", Icon = "🧒", Price = 200000m, Unit = "đ / giờ", PerNight = false,
                Description = "Dịch vụ trông trẻ tận phòng hoặc tại khu vui chơi, nhân viên được đào tạo, đặt trước 1 ngày." },
            new ServiceInfo { DbId = null, Name = "Thuê Xe Máy", Icon = "🛵", Price = 150000m, Unit = "đ / ngày", PerNight = false,
                Description = "Thuê xe máy giao tận khách sạn, kèm mũ bảo hiểm và bản đồ các điểm tham quan gần nhất." }
        };

        public static ServiceInfo ByDbId(int id)
        {
            return All.FirstOrDefault(s => s.DbId.HasValue && s.DbId.Value == id);
        }

        public static string PriceText(ServiceInfo s)
        {
            if (s.Price == 0) return "Miễn phí";
            return s.Price.ToString("N0") + " " + s.Unit;
        }
    }
}
