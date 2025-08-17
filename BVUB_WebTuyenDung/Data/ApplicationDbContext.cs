using BVUB_WebTuyenDung.Models;
using Microsoft.EntityFrameworkCore;

namespace BVUB_WebTuyenDung.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<DanhMucChucDanhDuTuyen> DanhMucChucDanhDuTuyen { get; set; }
        public DbSet<DanhMucViTriDuTuyen> DanhMucViTriDuTuyen { get; set; }
        public DbSet<DanhMucKhoaPhong> DanhMucKhoaPhong { get; set; }
        public DbSet<UngVien> UngVien { get; set; }
        public DbSet<DonVienChuc> DonVienChuc { get; set; }
        public DbSet<VanBang> VanBang { get; set; }
        public DbSet<HopDongNguoiLaoDong> HopDongNguoiLaoDong { get; set; }
        public DbSet<AdminUser> AdminUser { get; set; }
        public DbSet<AuditTrail> AuditTrail { get; set; }
        public DbSet<ThongTinTuyenDung> ThongTinTuyenDung { get; set; }
        public DbSet<KhoaPhongViTri> KhoaPhongViTri { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== AuditTrail =====
            modelBuilder.Entity<AuditTrail>()
                .Property(a => a.LoaiDon).IsRequired();

            modelBuilder.Entity<AuditTrail>()
                .HasOne(a => a.AdminCapNhat)
                .WithMany()
                .HasForeignKey(a => a.AdminCapNhatId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AuditTrail>()
                .HasCheckConstraint("CK_AuditTrail_LoaiDon",
                    "[LoaiDon] IN ('VienChuc','NguoiLaoDong')");

            // ===== Quan hệ 1-1: UngVien ↔ DonVienChuc =====
            modelBuilder.Entity<UngVien>()
                .HasOne(u => u.DonVienChuc)
                .WithOne(d => d.UngVien)
                .HasForeignKey<DonVienChuc>(d => d.UngVienId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DonVienChuc>()
                .HasIndex(d => d.UngVienId)
                .IsUnique();

            // ===== Quan hệ 1-1: UngVien ↔ HopDongNguoiLaoDong =====
            modelBuilder.Entity<UngVien>()
                .HasOne(u => u.HopDongNguoiLaoDong)
                .WithOne(h => h.UngVien)
                .HasForeignKey<HopDongNguoiLaoDong>(h => h.UngVienId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HopDongNguoiLaoDong>()
                .HasIndex(h => h.UngVienId)
                .IsUnique();

            // ===== Email duy nhất =====
            // UngVien.Email unique (thay cho Gmail)
            modelBuilder.Entity<UngVien>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // AdminUser.Email unique
            modelBuilder.Entity<AdminUser>()
                .HasIndex(a => a.Email)
                .IsUnique();

            // ===== Quan hệ 1-n: DonVienChuc ↔ VanBang =====
            modelBuilder.Entity<DonVienChuc>()
                .HasMany(d => d.VanBangs)
                .WithOne(v => v.DonVienChuc)
                .HasForeignKey(v => v.DonVienChucId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== Bảng nối KhoaPhong ↔ ViTri =====
            modelBuilder.Entity<KhoaPhongViTri>()
                .HasKey(x => new { x.KhoaPhongId, x.ViTriId });

            modelBuilder.Entity<KhoaPhongViTri>()
                .HasOne(x => x.KhoaPhong)
                .WithMany(kp => kp.KhoaPhongViTris)
                .HasForeignKey(x => x.KhoaPhongId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<KhoaPhongViTri>()
                .HasOne(x => x.ViTri)
                .WithMany(vt => vt.KhoaPhongViTris)
                .HasForeignKey(x => x.ViTriId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== Global filters cho danh mục đang hoạt động =====
            modelBuilder.Entity<DanhMucChucDanhDuTuyen>()
                .HasQueryFilter(x => x.TamNgung == 0);
            modelBuilder.Entity<DanhMucViTriDuTuyen>()
                .HasQueryFilter(x => x.TamNgung == 0);
            modelBuilder.Entity<DanhMucKhoaPhong>()
                .HasQueryFilter(x => x.TamNgung == 0);

            // ===== Ràng buộc/độ dài =====
            modelBuilder.Entity<DanhMucChucDanhDuTuyen>()
                .Property(x => x.TenChucDanh).HasMaxLength(70).IsRequired();
            modelBuilder.Entity<DanhMucKhoaPhong>()
                .Property(x => x.Ten).HasMaxLength(70).IsRequired();

            // Username unique
            modelBuilder.Entity<AdminUser>()
                .HasIndex(a => a.Username)
                .IsUnique();

            // Trạng thái tuyển dụng (0..3)
            modelBuilder.Entity<ThongTinTuyenDung>()
                .HasCheckConstraint("CK_ThongTinTuyenDung_TrangThai",
                    "[TrangThai] IN (0,1,2,3)");

            // Mã tra cứu duy nhất
            modelBuilder.Entity<DonVienChuc>()
                .HasIndex(d => d.MaTraCuu)
                .IsUnique();
        }
    }
}
