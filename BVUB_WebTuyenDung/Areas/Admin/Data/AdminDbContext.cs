using BVUB_WebTuyenDung.Areas.Admin.Models;
using Microsoft.EntityFrameworkCore;
using M = BVUB_WebTuyenDung.Areas.Admin.Models;

namespace BVUB_WebTuyenDung.Areas.Admin.Data
{
    public class AdminDbContext : DbContext
    {
        public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

        public DbSet<M.AdminUser> AdminUsers { get; set; }
        public DbSet<M.UngVien> UngViens { get; set; }
        public DbSet<M.AuditTrail> AuditTrail { get; set; }
        public DbSet<M.DonVienChuc> DonVienChucs { get; set; }
        public DbSet<M.HopDongNguoiLaoDong> HopDongNguoiLaoDongs { get; set; }
        public DbSet<M.ThongTinTuyenDung> ThongTinTuyenDungs { get; set; }
        public DbSet<M.HuongDanDangKy> HuongDans { get; set; }
        public DbSet<M.DanhMucChucDanhDuTuyen> DanhMucChucDanhDuTuyens { get; set; }
        public DbSet<M.DanhMucViTriDuTuyen> DanhMucViTriDuTuyens { get; set; }
        public DbSet<M.DanhMucKhoaPhong> DanhMucKhoaPhongs { get; set; }
        public DbSet<M.VanBang> VanBangs { get; set; }
        public DbSet<M.KhoaPhongViTri> KhoaPhongViTris { get; set; }
        public DbSet<AuditTrail> AuditTrails { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        protected override void OnModelCreating(ModelBuilder model)
        {
            base.OnModelCreating(model);

            // ====== Bảng & tên bảng ======
            model.Entity<M.AdminUser>().ToTable("AdminUser");
            model.Entity<M.UngVien>().ToTable("UngVien");
            model.Entity<M.AuditTrail>().ToTable("AuditTrail");
            model.Entity<M.ThongTinTuyenDung>().ToTable("ThongTinTuyenDung");
            model.Entity<M.HuongDanDangKy>().ToTable("HuongDanDangKy");

            // ====== UngVien ======
            model.Entity<M.UngVien>(e =>
            {
                e.ToTable("UngVien");
                // Kiểu cột ngày/thời gian khớp Data Annotations
                e.Property(x => x.NgaySinh).HasColumnType("date");
                e.Property(x => x.NgayCapCCCD).HasColumnType("date");
                e.Property(x => x.NgayUngTuyen).HasColumnType("datetime2(3)");
            });

            // ====== VanBang (FK -> UngVien) ======
            model.Entity<M.VanBang>(e =>
            {
                e.ToTable("VanBang");
                e.HasKey(v => v.VanBangId);
                e.Property(v => v.NgayCap).HasColumnType("date");

                // Quan hệ tường minh để KHÔNG sinh UngVienId1
                e.HasOne(v => v.UngVien)
                 .WithMany(u => u.VanBangs)
                 .HasForeignKey(v => v.UngVienId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ====== DonVienChuc ======
            model.Entity<M.DonVienChuc>(e =>
            {
                e.ToTable("DonVienChuc");
                e.HasKey(d => d.VienChucId);
                e.Property(d => d.NgayNop).HasColumnType("date");

                // Chỉ 1 quan hệ tới UngVien, dùng đúng FK DonVienChuc.UngVienId
                // (Vì UngVien hiện KHÔNG có ICollection<DonVienChuc>, dùng .WithMany() là đúng)
                e.HasOne(d => d.UngVien)
                 .WithMany()
                 .HasForeignKey(d => d.UngVienId)
                 .OnDelete(DeleteBehavior.Restrict);

                // FK khác dùng Restrict để tránh cascade vòng
                e.HasOne(d => d.ViTriDuTuyen)
                 .WithMany(v => v.DonVienChucs)
                 .HasForeignKey(d => d.ViTriDuTuyenId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(d => d.ChucDanhDuTuyen)
                 .WithMany(c => c.DonVienChucs)
                 .HasForeignKey(d => d.ChucDanhDuTuyenId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(d => d.KhoaPhong)
                 .WithMany(k => k.DonVienChucs)
                 .HasForeignKey(d => d.KhoaPhongId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ====== HopDongNguoiLaoDong ======
            model.Entity<M.HopDongNguoiLaoDong>(e =>
            {
                e.ToTable("HopDongNguoiLaoDong");

                e.HasOne(h => h.UngVien)
                 .WithMany()
                 .HasForeignKey(h => h.UngVienId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(h => h.KhoaPhongCongTac)
                 .WithMany()
                 .HasForeignKey(h => h.KhoaPhongCongTacId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ====== Danh mục chức danh dự tuyển ======
            model.Entity<M.DanhMucChucDanhDuTuyen>(e =>
            {
                e.ToTable("DanhMucChucDanhDuTuyen");
                e.HasKey(x => x.ChucDanhId);
                e.Property(x => x.TenChucDanh).IsRequired().HasMaxLength(70);
            });

            // ====== Danh mục vị trí dự tuyển ======
            model.Entity<M.DanhMucViTriDuTuyen>(e =>
            {
                e.ToTable("DanhMucViTriDuTuyen");
                e.HasKey(x => x.ViTriId);
                e.Property(x => x.TenViTri).IsRequired().HasMaxLength(70);
                e.Property(x => x.ChucDanhId).IsRequired();

                e.HasOne(x => x.ChucDanh)
                 .WithMany(c => c.ViTris)
                 .HasForeignKey(x => x.ChucDanhId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ====== Danh mục khoa phòng ======
            model.Entity<M.DanhMucKhoaPhong>(e =>
            {
                e.ToTable("DanhMucKhoaPhong");
                e.HasKey(x => x.KhoaPhongId);
                e.Property(x => x.Ten).IsRequired().HasMaxLength(70);
                e.Property(x => x.Loai).IsRequired().HasMaxLength(20);
            });

            // ====== Bảng nối KhoaPhong - ViTri (khóa kép) ======
            model.Entity<M.KhoaPhongViTri>(e =>
            {
                e.ToTable("KhoaPhongViTri");
                e.HasKey(x => new { x.KhoaPhongId, x.ViTriId });

                e.HasOne(x => x.KhoaPhong)
                 .WithMany(k => k.KhoaPhongViTris)
                 .HasForeignKey(x => x.KhoaPhongId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.ViTri)
                 .WithMany(v => v.KhoaPhongViTris)
                 .HasForeignKey(x => x.ViTriId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
