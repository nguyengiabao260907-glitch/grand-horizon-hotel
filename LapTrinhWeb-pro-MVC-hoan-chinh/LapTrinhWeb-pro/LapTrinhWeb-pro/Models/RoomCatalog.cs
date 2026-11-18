using System;
using System.Collections.Generic;
using System.Linq;

namespace LapTrinhWeb_pro.Models
{
    // Thông tin chi tiết của một loại phòng (diện tích, giường, view, tiện nghi...).
    // LƯU Ý: bảng ROOM_TYPE trong EDMX chỉ có room_type_id, property_id, name,
    // room_type1 và price_per_night, KHÔNG có các cột này, nên dữ liệu chi tiết
    // được để ở class static này. KHÔNG sửa EDMX / file Model auto-generated.
    public class RoomInfo
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public int MaxGuests { get; set; }
        public int AreaM2 { get; set; }
        public string Bed { get; set; }
        public string View { get; set; }
        public string[] Amenities { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
    }

    public static class RoomCatalog
    {
        public static readonly Dictionary<string, RoomInfo> ByKey =
            new Dictionary<string, RoomInfo>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "standard", new RoomInfo
                {
                    Key = "standard", Name = "Standard", MaxGuests = 2, AreaM2 = 28,
                    Bed = "1 giường đôi", View = "View thành phố",
                    Amenities = new[] { "Wifi miễn phí", "TV màn hình phẳng", "Minibar", "Điều hòa", "Két an toàn", "Bàn làm việc" },
                    Description = "Phòng ấm cúng, đầy đủ tiện nghi, phù hợp cho cặp đôi hoặc khách đi công tác ngắn ngày.",
                    ImageUrl = "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?q=80&w=1200&auto=format&fit=crop"
                }
            },
            {
                "deluxe", new RoomInfo
                {
                    Key = "deluxe", Name = "Deluxe", MaxGuests = 2, AreaM2 = 38,
                    Bed = "1 giường King", View = "View biển",
                    Amenities = new[] { "Wifi miễn phí", "TV màn hình phẳng", "Minibar", "Bồn tắm riêng", "Ban công hướng biển", "Máy pha cà phê" },
                    Description = "Phòng rộng rãi, ban công hướng biển, bồn tắm riêng — lựa chọn được yêu thích nhất cho kỳ nghỉ biển.",
                    ImageUrl = "https://images.unsplash.com/photo-1611892440504-42a792e24d32?q=80&w=1200&auto=format&fit=crop"
                }
            },
            {
                "suite", new RoomInfo
                {
                    Key = "suite", Name = "Executive Suite", MaxGuests = 4, AreaM2 = 65,
                    Bed = "1 giường King + sofa bed", View = "View toàn cảnh",
                    Amenities = new[] { "Wifi miễn phí", "Phòng khách riêng", "Bồn tắm riêng", "Minibar", "Ban công lớn", "Dịch vụ phòng 24/7" },
                    Description = "Suite cao cấp với phòng khách riêng và view toàn cảnh, phù hợp cho gia đình hoặc khách muốn không gian riêng tư.",
                    ImageUrl = "https://images.unsplash.com/photo-1582719508461-905c673771fd?q=80&w=1200&auto=format&fit=crop"
                }
            },
            {
                "family", new RoomInfo
                {
                    Key = "family", Name = "Family Suite", MaxGuests = 4, AreaM2 = 55,
                    Bed = "2 phòng ngủ: 1 giường đôi + 2 giường đơn", View = "View vườn / biển",
                    Amenities = new[] { "Wifi miễn phí", "2 phòng ngủ", "TV màn hình phẳng", "Minibar", "Bồn tắm riêng", "Khu vui chơi trẻ em" },
                    Description = "Suite gia đình 2 phòng ngủ, thoải mái cho 2 người lớn và 2 trẻ em, gần hồ bơi và khu vui chơi.",
                    ImageUrl = "https://images.unsplash.com/photo-1590490360182-c33d57733427?q=80&w=1200&auto=format&fit=crop"
                }
            },
            {
                "ocean", new RoomInfo
                {
                    Key = "ocean", Name = "Ocean View Deluxe", MaxGuests = 3, AreaM2 = 42,
                    Bed = "1 giường King + 1 giường đơn", View = "View biển trực diện",
                    Amenities = new[] { "Wifi miễn phí", "Ban công view biển", "Bồn tắm riêng", "TV màn hình phẳng", "Minibar", "Máy pha cà phê" },
                    Description = "Phòng Deluxe view biển trực diện, mở cửa ban công là thấy biển — ngắm bình minh ngay từ giường ngủ.",
                    ImageUrl = "https://images.unsplash.com/photo-1578683010236-d716f9a3f461?q=80&w=1200&auto=format&fit=crop"
                }
            },
            {
                "presidential", new RoomInfo
                {
                    Key = "presidential", Name = "Presidential Suite", MaxGuests = 4, AreaM2 = 120,
                    Bed = "1 giường King + phòng ngủ phụ", View = "View biển toàn cảnh 180 độ",
                    Amenities = new[] { "Wifi miễn phí", "Bể bơi riêng", "Phòng khách lớn", "Bếp riêng", "Quản gia riêng", "Đưa đón sân bay miễn phí" },
                    Description = "Suite Tổng thống sang trọng nhất khách sạn: bể bơi riêng, quản gia riêng và view biển 180 độ.",
                    ImageUrl = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?q=80&w=1200&auto=format&fit=crop"
                }
            }
        };

        // Map tên loại phòng trong DB (hoặc key người dùng gõ) sang key của catalog.
        // "executive suite" / "executive" / "suite" đều map về "suite".
        public static string KeyForName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "deluxe";
            string key = name.Trim().ToLowerInvariant();
            if (ByKey.ContainsKey(key)) return key;
            if (key.Contains("presidential")) return "presidential";
            if (key.Contains("ocean")) return "ocean";
            if (key.Contains("family")) return "family";
            if (key.Contains("executive") || key.Contains("suite")) return "suite";
            if (key.Contains("deluxe")) return "deluxe";
            if (key.Contains("standard")) return "standard";
            return "deluxe";
        }

        public static RoomInfo ByName(string name)
        {
            RoomInfo info;
            return ByKey.TryGetValue(KeyForName(name), out info) ? info : ByKey["deluxe"];
        }

        public static RoomInfo ByKeyOrDefault(string key)
        {
            RoomInfo info;
            return !string.IsNullOrWhiteSpace(key) && ByKey.TryGetValue(key.Trim(), out info)
                ? info : ByKey["deluxe"];
        }

        public static IEnumerable<RoomInfo> AllInOrder()
        {
            string[] order = { "standard", "deluxe", "ocean", "family", "suite", "presidential" };
            foreach (string key in order)
                yield return ByKey[key];
        }
    }
}
