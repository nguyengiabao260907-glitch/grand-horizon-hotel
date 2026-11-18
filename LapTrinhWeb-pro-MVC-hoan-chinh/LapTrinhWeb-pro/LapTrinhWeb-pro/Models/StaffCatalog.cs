using System;
using System.Collections.Generic;
using System.Linq;

namespace LapTrinhWeb_pro.Models
{
    // Một lễ tân: làm việc tại khách sạn nào, ca nào, số điện thoại nào.
    // LƯU Ý: đây là dữ liệu MẪU cho đồ án (không có bảng nhân viên trong
    // database), muốn đổi tên/giờ trực thì sửa trực tiếp file này.
    public class StaffMember
    {
        public string Name { get; set; }
        public int PropertyId { get; set; }
        public string ShiftName { get; set; }
        public TimeSpan ShiftStart { get; set; }
        public TimeSpan ShiftEnd { get; set; }
        public string Phone { get; set; }

        public string ShiftText
        {
            get { return ShiftName + ": " + ShiftStart.ToString(@"hh\:mm") + " – " + ShiftEnd.ToString(@"hh\:mm"); }
        }
    }

    public static class StaffCatalog
    {
        // 3 ca cố định: sáng 06:00–14:00, chiều 14:00–22:00, đêm 22:00–06:00.
        public static readonly TimeSpan MorningStart = new TimeSpan(6, 0, 0);
        public static readonly TimeSpan MorningEnd = new TimeSpan(14, 0, 0);
        public static readonly TimeSpan AfternoonStart = new TimeSpan(14, 0, 0);
        public static readonly TimeSpan AfternoonEnd = new TimeSpan(22, 0, 0);
        public static readonly TimeSpan NightStart = new TimeSpan(22, 0, 0);
        public static readonly TimeSpan NightEnd = new TimeSpan(6, 0, 0);

        public static readonly List<StaffMember> All = new List<StaffMember>
        {
            // Khách sạn Đà Nẵng (property_id = 1) — đủ cả 3 ca.
            new StaffMember { Name = "Trần Thị Mai Lan", PropertyId = 1, ShiftName = "Ca sáng", ShiftStart = MorningStart, ShiftEnd = MorningEnd, Phone = "0369 436 101" },
            new StaffMember { Name = "Nguyễn Văn Hùng", PropertyId = 1, ShiftName = "Ca chiều", ShiftStart = AfternoonStart, ShiftEnd = AfternoonEnd, Phone = "0369 436 102" },
            new StaffMember { Name = "Lê Thị Thu Hằng", PropertyId = 1, ShiftName = "Ca đêm", ShiftStart = NightStart, ShiftEnd = NightEnd, Phone = "0369 436 103" },
            // Sài Gòn (2)
            new StaffMember { Name = "Phạm Quốc Bảo", PropertyId = 2, ShiftName = "Ca sáng", ShiftStart = MorningStart, ShiftEnd = MorningEnd, Phone = "028 3822 3101" },
            new StaffMember { Name = "Đỗ Thị Ngọc Trâm", PropertyId = 2, ShiftName = "Ca chiều", ShiftStart = AfternoonStart, ShiftEnd = AfternoonEnd, Phone = "028 3822 3102" },
            // Hội An (3)
            new StaffMember { Name = "Huỳnh Thị Kim Chi", PropertyId = 3, ShiftName = "Ca sáng", ShiftStart = MorningStart, ShiftEnd = MorningEnd, Phone = "0235 3991 101" },
            new StaffMember { Name = "Trương Minh Quân", PropertyId = 3, ShiftName = "Ca chiều", ShiftStart = AfternoonStart, ShiftEnd = AfternoonEnd, Phone = "0235 3991 102" },
            // Đà Lạt (4)
            new StaffMember { Name = "Ngô Thị Thanh Thảo", PropertyId = 4, ShiftName = "Ca sáng", ShiftStart = MorningStart, ShiftEnd = MorningEnd, Phone = "0263 3554 101" },
            new StaffMember { Name = "Bùi Văn Sơn", PropertyId = 4, ShiftName = "Ca chiều", ShiftStart = AfternoonStart, ShiftEnd = AfternoonEnd, Phone = "0263 3554 102" },
            // Nha Trang (5)
            new StaffMember { Name = "Võ Thị Mỹ Linh", PropertyId = 5, ShiftName = "Ca sáng", ShiftStart = MorningStart, ShiftEnd = MorningEnd, Phone = "0258 3556 101" },
            new StaffMember { Name = "Đặng Hoàng Phúc", PropertyId = 5, ShiftName = "Ca chiều", ShiftStart = AfternoonStart, ShiftEnd = AfternoonEnd, Phone = "0258 3556 102" },
            // Phú Quốc (6)
            new StaffMember { Name = "Lý Thị Cẩm Tú", PropertyId = 6, ShiftName = "Ca sáng", ShiftStart = MorningStart, ShiftEnd = MorningEnd, Phone = "0297 3889 101" },
            // Hà Nội (7) — đủ cả 3 ca.
            new StaffMember { Name = "Hoàng Thị Minh Anh", PropertyId = 7, ShiftName = "Ca sáng", ShiftStart = MorningStart, ShiftEnd = MorningEnd, Phone = "024 3822 1101" },
            new StaffMember { Name = "Vũ Đức Anh", PropertyId = 7, ShiftName = "Ca chiều", ShiftStart = AfternoonStart, ShiftEnd = AfternoonEnd, Phone = "024 3822 1102" },
            new StaffMember { Name = "Phan Thị Diệu Linh", PropertyId = 7, ShiftName = "Ca đêm", ShiftStart = NightStart, ShiftEnd = NightEnd, Phone = "024 3822 1103" }
        };

        // Có đang trực không? So sánh theo giờ trong ngày (TimeSpan).
        // Ca thường (bắt đầu < kết thúc): trực khi start <= now < end.
        // Ca đêm qua ngày (bắt đầu > kết thúc, vd 22:00–06:00):
        // trực khi now >= start HOẶC now < end.
        public static bool IsOnDuty(TimeSpan shiftStart, TimeSpan shiftEnd, TimeSpan now)
        {
            if (shiftStart < shiftEnd)
                return now >= shiftStart && now < shiftEnd;
            return now >= shiftStart || now < shiftEnd;
        }

        public static bool IsOnDuty(StaffMember staff, DateTime now)
        {
            return IsOnDuty(staff.ShiftStart, staff.ShiftEnd, now.TimeOfDay);
        }

        public static List<StaffMember> OnDutyNow(DateTime now)
        {
            return All.Where(s => IsOnDuty(s, now)).ToList();
        }

        public static List<StaffMember> ByProperty(int propertyId)
        {
            return All.Where(s => s.PropertyId == propertyId).ToList();
        }
    }
}
