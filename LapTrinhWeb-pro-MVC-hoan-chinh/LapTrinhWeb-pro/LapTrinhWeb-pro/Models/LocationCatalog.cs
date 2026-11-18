using System;
using System.Collections.Generic;
using System.Linq;

namespace LapTrinhWeb_pro.Models
{
    // Tỉnh/thành phố và danh sách quận/huyện của nó.
    // LƯU Ý: chỉ đủ cho 7 nơi đang có khách sạn (KHÔNG đủ 63 tỉnh cả nước).
    // Tên quận/huyện đều là tên thật của Việt Nam.
    public class ProvinceInfo
    {
        public string Name { get; set; }
        public List<string> Districts { get; set; }
    }

    public static class LocationCatalog
    {
        public static readonly List<ProvinceInfo> Provinces = new List<ProvinceInfo>
        {
            new ProvinceInfo { Name = "Đà Nẵng", Districts = new List<string> { "Hải Châu", "Thanh Khê", "Sơn Trà", "Ngũ Hành Sơn", "Liên Chiểu", "Cẩm Lệ" } },
            new ProvinceInfo { Name = "TP. Hồ Chí Minh", Districts = new List<string> { "Quận 1", "Quận 3", "Quận 5", "Quận 10", "Bình Thạnh", "Phú Nhuận", "Tân Bình", "Thủ Đức" } },
            new ProvinceInfo { Name = "Quảng Nam", Districts = new List<string> { "Hội An", "Tam Kỳ", "Điện Bàn", "Duy Xuyên" } },
            new ProvinceInfo { Name = "Lâm Đồng", Districts = new List<string> { "Đà Lạt", "Bảo Lộc", "Đức Trọng", "Lạc Dương" } },
            new ProvinceInfo { Name = "Khánh Hòa", Districts = new List<string> { "Nha Trang", "Cam Ranh", "Ninh Hòa", "Diên Khánh" } },
            new ProvinceInfo { Name = "Kiên Giang", Districts = new List<string> { "Phú Quốc", "Rạch Giá", "Hà Tiên", "Châu Thành" } },
            new ProvinceInfo { Name = "Hà Nội", Districts = new List<string> { "Hoàn Kiếm", "Ba Đình", "Đống Đa", "Hai Bà Trưng", "Cầu Giấy", "Tây Hồ", "Thanh Xuân", "Hoàng Mai" } }
        };

        // Ánh xạ khách sạn -> (tỉnh, quận/huyện), khớp với thành phố trong
        // địa chỉ seed của database.sql. Địa chỉ seed hầu hết KHÔNG ghi tên
        // quận (vd "123 Đường Biển, Đà Nẵng") nên lọc theo quận KHÔNG thể
        // dựa vào chuỗi địa chỉ — phải dùng bảng ánh xạ này.
        // property_id: 1 Đà Nẵng, 2 Sài Gòn, 3 Hội An, 4 Đà Lạt,
        // 5 Nha Trang, 6 Phú Quốc, 7 Hà Nội.
        public static readonly Dictionary<int, string[]> PropertyLocation =
            new Dictionary<int, string[]>
        {
            { 1, new[] { "Đà Nẵng", "Sơn Trà" } },
            { 2, new[] { "TP. Hồ Chí Minh", "Quận 1" } },
            { 3, new[] { "Quảng Nam", "Hội An" } },
            { 4, new[] { "Lâm Đồng", "Đà Lạt" } },
            { 5, new[] { "Khánh Hòa", "Nha Trang" } },
            { 6, new[] { "Kiên Giang", "Phú Quốc" } },
            { 7, new[] { "Hà Nội", "Hoàn Kiếm" } }
        };

        public static List<string> DistrictsOf(string province)
        {
            if (string.IsNullOrWhiteSpace(province)) return new List<string>();
            string key = province.Trim();
            var found = Provinces.FirstOrDefault(p =>
                p.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
            return found != null ? found.Districts.ToList() : new List<string>();
        }

        public static List<int> PropertyIdsInProvince(string province)
        {
            if (string.IsNullOrWhiteSpace(province)) return new List<int>();
            string key = province.Trim();
            return PropertyLocation
                .Where(kv => kv.Value[0].Equals(key, StringComparison.OrdinalIgnoreCase))
                .Select(kv => kv.Key).ToList();
        }

        public static List<int> PropertyIdsInDistrict(string province, string district)
        {
            if (string.IsNullOrWhiteSpace(district)) return PropertyIdsInProvince(province);
            string key = district.Trim();
            return PropertyLocation
                .Where(kv => kv.Value[1].Equals(key, StringComparison.OrdinalIgnoreCase) &&
                    (string.IsNullOrWhiteSpace(province) ||
                     kv.Value[0].Equals(province.Trim(), StringComparison.OrdinalIgnoreCase)))
                .Select(kv => kv.Key).ToList();
        }
    }
}
