namespace LapTrinhWeb_pro.Models
{
    // Món trong thực đơn buffet — khớp 6 cột của bảng dbo.BUFFET_ITEM.
    // Class thường, KHÔNG nằm trong EDMX (không sửa HotelDb.edmx).
    // Mọi truy cập bảng này dùng db.Database.SqlQuery<BuffetItem> /
    // db.Database.ExecuteSqlCommand với SqlParameter.
    public class BuffetItem
    {
        public int item_id { get; set; }
        public string name { get; set; }
        public string category { get; set; }
        public string description { get; set; }
        public decimal price { get; set; }
        public bool is_available { get; set; }
    }
}
