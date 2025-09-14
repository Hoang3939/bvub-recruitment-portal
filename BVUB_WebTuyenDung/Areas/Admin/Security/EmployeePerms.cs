using System;

namespace BVUB_WebTuyenDung.Areas.Admin.Security
{
    [Flags]
    public enum StaffPerms : int
    {
        None = 0,
        Dashboard = 1 << 1,                 // 2
        Candidates = 1 << 2,                // 4
        Recruitments = 1 << 3,              // 8
        Guides = 1 << 4,                    // 16
        Departments = 1 << 5,               // 32
        Positions = 1 << 6,                 // 64
        Titles = 1 << 7,                    // 128
        Reports = 1 << 8,                   // 256
        AuditTrail = 1 << 9,                // 512
        Settings = 1 << 10                  // 1024
    }

    public static class StaffPermOptions
    {
        public static readonly (int val, string text)[] All = new[]
        {
            ((int)StaffPerms.Dashboard , "Bảng điều khiển"),
            ((int)StaffPerms.Candidates , "Quản lý ứng viên"),
            ((int)StaffPerms.Recruitments, "Quản lý thông tin tuyển dụng"),
            ((int)StaffPerms.Guides, "Quản lý hướng dẫn đăng ký"),
            ((int)StaffPerms.Departments , "Quản lý danh mục khoa phòng"),
            ((int)StaffPerms.Positions   , "Quản lý danh mục vị trí dự tuyển"),
            ((int)StaffPerms.Titles      , "Quản lý danh mục chức danh dự tuyển"),
            ((int)StaffPerms.Reports     , "Báo cáo thống kê"),
            ((int)StaffPerms.AuditTrail    , "Nhật ký hệ thống"),
            ((int)StaffPerms.Settings    , "Cài đặt"),
        };
    }
}
