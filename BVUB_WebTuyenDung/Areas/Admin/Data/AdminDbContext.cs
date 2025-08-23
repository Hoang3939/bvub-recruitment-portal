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

        protected override void OnModelCreating(ModelBuilder model)
        {
            base.OnModelCreating(model);

            model.Entity<M.AdminUser>().ToTable("AdminUser");
            model.Entity<M.UngVien>().ToTable("UngVien");
            model.Entity<M.AuditTrail>().ToTable("AuditTrail");
            model.Entity<M.ThongTinTuyenDung>().ToTable("ThongTinTuyenDung");
            model.Entity<M.HuongDanDangKy>().ToTable("HuongDanDangKy");

            model.Entity<M.DonVienChuc>(e =>
            {
                e.ToTable("DonVienChuc");
                e.HasKey(d => d.VienChucId);

                e.HasOne(d => d.UngVien).WithMany()
                   .HasForeignKey(d => d.UngVienId).OnDelete(DeleteBehavior.Restrict);

                e.HasOne(d => d.ViTriDuTuyen).WithMany(v => v.DonVienChucs)
                   .HasForeignKey(d => d.ViTriDuTuyenId).OnDelete(DeleteBehavior.Restrict);

                e.HasOne(d => d.ChucDanhDuTuyen).WithMany(c => c.DonVienChucs)
                   .HasForeignKey(d => d.ChucDanhDuTuyenId).OnDelete(DeleteBehavior.Restrict);

                e.HasOne(d => d.KhoaPhong).WithMany(k => k.DonVienChucs)
                   .HasForeignKey(d => d.KhoaPhongId).OnDelete(DeleteBehavior.Restrict);
            });

            model.Entity<M.HopDongNguoiLaoDong>(e =>
            {
                e.ToTable("HopDongNguoiLaoDong");
                e.HasOne(h => h.UngVien).WithMany()
                   .HasForeignKey(h => h.UngVienId).OnDelete(DeleteBehavior.Restrict);

                e.HasOne(h => h.KhoaPhongCongTac).WithMany()
                   .HasForeignKey(h => h.KhoaPhongCongTacId).OnDelete(DeleteBehavior.Restrict);
            });

            model.Entity<M.DanhMucChucDanhDuTuyen>(e =>
            {
                e.ToTable("DanhMucChucDanhDuTuyen");
                e.HasKey(x => x.ChucDanhId);
                e.Property(x => x.TenChucDanh).IsRequired().HasMaxLength(70);
            });

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

            model.Entity<M.DanhMucKhoaPhong>(e =>
            {
                e.ToTable("DanhMucKhoaPhong");
                e.HasKey(x => x.KhoaPhongId);
                e.Property(x => x.Ten).IsRequired().HasMaxLength(70);
                e.Property(x => x.Loai).IsRequired().HasMaxLength(20);
            });

            // BẢNG NỐI: cấu hình KHÓA KÉP tại đây
            model.Entity<M.KhoaPhongViTri>(e =>
            {
                e.ToTable("KhoaPhongViTri");
                e.HasKey(x => new { x.KhoaPhongId, x.ViTriId });

                e.HasOne(x => x.KhoaPhong).WithMany(k => k.KhoaPhongViTris)
                   .HasForeignKey(x => x.KhoaPhongId).OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.ViTri).WithMany(v => v.KhoaPhongViTris)
                   .HasForeignKey(x => x.ViTriId).OnDelete(DeleteBehavior.Cascade);
            });

            model.Entity<M.VanBang>(e =>
            {
                e.ToTable("VanBang");
                e.HasKey(v => v.VanBangId);
                e.HasOne(v => v.DonVienChuc).WithMany(d => d.VanBangs)
                   .HasForeignKey(v => v.DonVienChucId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
