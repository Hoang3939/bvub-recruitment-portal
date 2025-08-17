using BVUB_WebTuyenDung.Models;
using Microsoft.EntityFrameworkCore;

namespace BVUB_WebTuyenDung.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

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
        public DbSet<HuongDanDangKy> HuongDanDangKy { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<AuditTrail>()
                .Property(a => a.LoaiDon)
                .IsRequired();

            modelBuilder.Entity<AuditTrail>()
                .HasOne(a => a.AdminCapNhat)
                .WithMany()
                .HasForeignKey(a => a.AdminCapNhatId);
        }
    }
}
