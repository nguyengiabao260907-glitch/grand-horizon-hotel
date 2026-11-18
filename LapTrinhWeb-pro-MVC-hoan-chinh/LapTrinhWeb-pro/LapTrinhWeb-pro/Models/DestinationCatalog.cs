using System.Collections.Generic;

namespace LapTrinhWeb_pro.Models
{
    // Mô tả hiển thị cho từng địa điểm (khách sạn). Bảng PROPERTY chỉ có
    // property_id, property_name, address, phone, membership_tier nên phần
    // mô tả / mùa đẹp nhất / ảnh được để ở đây, key theo property_id trong DB.
    public class DestinationInfo
    {
        public int PropertyId { get; set; }
        public string Description { get; set; }
        public string BestSeason { get; set; }
        public string ImageUrl { get; set; }
    }

    public static class DestinationCatalog
    {
        public static readonly Dictionary<int, DestinationInfo> ByPropertyId =
            new Dictionary<int, DestinationInfo>
        {
            {
                1, new DestinationInfo
                {
                    PropertyId = 1,
                    Description = "Khách sạn bên bờ biển Đà Nẵng, gần bãi biển Mỹ Khê và cầu Rồng.",
                    BestSeason = "Tháng 3 - tháng 8",
                    ImageUrl = "https://images.unsplash.com/photo-1559592413-7cec4d0cae2b?q=80&w=1200&auto=format&fit=crop"
                }
            },
            {
                2, new DestinationInfo
                {
                    PropertyId = 2,
                    Description = "Giữa trung tâm Sài Gòn sôi động, gần phố đi bộ Nguyễn Huệ và chợ Bến Thành.",
                    BestSeason = "Tháng 12 - tháng 4",
                    ImageUrl = "https://images.unsplash.com/photo-1583417319070-4a69db38a482?q=80&w=1200&auto=format&fit=crop"
                }
            },
            {
                3, new DestinationInfo
                {
                    PropertyId = 3,
                    Description = "Phố cổ đèn lồng, sông Hoài lặng trôi — nghỉ dưỡng chậm rãi giữa di sản.",
                    BestSeason = "Tháng 2 - tháng 8",
                    ImageUrl = "https://images.unsplash.com/photo-1528127269322-539801943592?q=80&w=1200&auto=format&fit=crop"
                }
            },
            {
                4, new DestinationInfo
                {
                    PropertyId = 4,
                    Description = "Sương sớm, đồi thông và cà phê nóng — trốn phố về với núi rừng Đà Lạt.",
                    BestSeason = "Tháng 11 - tháng 3",
                    ImageUrl = "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?q=80&w=1200&auto=format&fit=crop"
                }
            },
            {
                5, new DestinationInfo
                {
                    PropertyId = 5,
                    Description = "Biển xanh, cát trắng Nha Trang, gần Vinpearl và các đảo đẹp nhất vịnh.",
                    BestSeason = "Tháng 1 - tháng 8",
                    ImageUrl = "https://images.unsplash.com/photo-1540541338287-41700207dee6?q=80&w=1200&auto=format&fit=crop"
                }
            },
            {
                6, new DestinationInfo
                {
                    PropertyId = 6,
                    Description = "Đảo ngọc Phú Quốc với bãi Sao, hoàng hôn đẹp nhất Việt Nam.",
                    BestSeason = "Tháng 11 - tháng 4",
                    ImageUrl = "https://images.unsplash.com/photo-1559494007-9f5847c49d94?q=80&w=1200&auto=format&fit=crop"
                }
            },
            {
                7, new DestinationInfo
                {
                    PropertyId = 7,
                    Description = "Giữa lòng Hà Nội cổ kính, gần hồ Hoàn Kiếm và phố cổ 36 phố phường.",
                    BestSeason = "Tháng 9 - tháng 12",
                    ImageUrl = "https://images.unsplash.com/photo-1509030450996-dd1a26dda07a?q=80&w=1200&auto=format&fit=crop"
                }
            }
        };

        public static DestinationInfo ByPropertyIdOrDefault(int propertyId)
        {
            DestinationInfo info;
            if (ByPropertyId.TryGetValue(propertyId, out info)) return info;
            return new DestinationInfo
            {
                PropertyId = propertyId,
                Description = "Khách sạn Grand Horizon — nghỉ dưỡng tiện nghi, dịch vụ tận tâm.",
                BestSeason = "Quanh năm",
                ImageUrl = "https://images.unsplash.com/photo-1566073771259-6a8506099945?q=80&w=1200&auto=format&fit=crop"
            };
        }
    }
}
