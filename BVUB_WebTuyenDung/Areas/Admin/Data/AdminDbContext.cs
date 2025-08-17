using Microsoft.EntityFrameworkCore;
using BVUB_WebTuyenDung.Areas.Admin.Models;

namespace BVUB_WebTuyenDung.Areas.Admin.Data
{
    public class AdminDbContext : DbContext
    {
        public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<UngVien> UngViens { get; set; }
        public DbSet<AuditTrail> AuditTrail { get; set; }
        public DbSet<DonVienChuc> DonVienChuc { get; set; }
        public DbSet<HopDongNguoiLaoDong> HopDongNguoiLaoDong { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AdminUser>().ToTable("AdminUser");
            modelBuilder.Entity<UngVien>().ToTable("UngVien");
            modelBuilder.Entity<AuditTrail>().ToTable("AuditTrail");
            modelBuilder.Entity<DonVienChuc>().ToTable("DonVienChuc");
            modelBuilder.Entity<HopDongNguoiLaoDong>().ToTable("HopDongNguoiLaoDong");
        }
    }
}
