using Microsoft.EntityFrameworkCore;

namespace UserDemo.Data
{
    public class MyDbContext : DbContext
    {
        
        public MyDbContext(DbContextOptions options) : base(options) { }

        #region Dbset

        public DbSet<NguoiDung> NguoiDungs { get; set; }
        public DbSet<Session> Sessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NguoiDung>(entity =>
            {
                entity.HasIndex(e => e.UserName).IsUnique();
                entity.Property(e => e.HoTen).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            });
            modelBuilder.Entity<Session>()
                    .HasOne(se => se.NguoiDung)
                    .WithMany()
                    .HasForeignKey(se => se.UserNameId);
        }

        #endregion
    }
}
