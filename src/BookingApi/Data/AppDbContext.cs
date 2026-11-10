using BookingApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace BookingApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<SalonService> Services => Set<SalonService>();
    public DbSet<Master> Masters => Set<Master>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SalonService>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Price).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Master>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Specialization).HasMaxLength(100);
            e.Property(x => x.PhotoUrl).HasMaxLength(500);

            e.HasMany(x => x.Services)
                .WithMany(x => x.Masters)
                .UsingEntity(j => j.ToTable("MasterServices"));

            e.HasMany(x => x.WorkingHours)
                .WithOne()
                .HasForeignKey(x => x.MasterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkingHours>(e =>
        {
            e.HasIndex(x => new { x.MasterId, x.DayOfWeek }).IsUnique();
        });

        modelBuilder.Entity<Booking>(e =>
        {
            e.Property(x => x.ClientName).HasMaxLength(100);
            e.Property(x => x.ClientPhone).HasMaxLength(30);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(x => x.Master).WithMany().HasForeignKey(x => x.MasterId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.MasterId, x.Date });
        });
    }
}
