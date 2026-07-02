using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using LaptopTracker.Models;
using LaptopTracker.Models.Agent;

namespace LaptopTracker.Data
{
    public class LaptopTrackerDbContext : DbContext
    {
        public LaptopTrackerDbContext(DbContextOptions<LaptopTrackerDbContext> options) : base(options) { }

        public DbSet<ReturnDevice> ReturnDevices { get; set; } = null!;
        public DbSet<ReturnDevicePhoto> ReturnDevicePhotos { get; set; } = null!;
        public DbSet<WicStockDevice> WicStockDevices { get; set; } = null!;
        public DbSet<LoanerDevice> LoanerDevices { get; set; } = null!;
        public DbSet<Handover> Handovers { get; set; } = null!;
        public DbSet<HandoverDevice> HandoverDevices { get; set; } = null!;
        public DbSet<IdempotencyRecord> IdempotencyRecords { get; set; } = null!;
        public DbSet<AgentActionAudit> AgentActionAudits { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ReturnDevice>()
                .Property(d => d.Status).HasConversion<string>().HasMaxLength(32).IsRequired();

            modelBuilder.Entity<WicStockDevice>()
                .Property(d => d.Status).HasConversion<string>().HasMaxLength(32).IsRequired();

            modelBuilder.Entity<LoanerDevice>()
                .Property(d => d.Status).HasConversion<string>().HasMaxLength(32).IsRequired();

            modelBuilder.Entity<HandoverDevice>()
                .HasOne(hd => hd.Handover)
                .WithMany(h => h.Devices)
                .HasForeignKey(hd => hd.HandoverId);

            modelBuilder.Entity<ReturnDevicePhoto>()
                .HasOne(p => p.ReturnDevice)
                .WithMany(d => d.Photos)
                .HasForeignKey(p => p.ReturnDeviceId);

            modelBuilder.Entity<IdempotencyRecord>()
                .Property(r => r.RequestId).HasMaxLength(128).IsRequired();
            modelBuilder.Entity<IdempotencyRecord>()
                .Property(r => r.Endpoint).HasMaxLength(128).IsRequired();
            modelBuilder.Entity<IdempotencyRecord>()
                .HasIndex(r => new { r.RequestId, r.Endpoint }).IsUnique();

            modelBuilder.Entity<AgentActionAudit>()
                .Property(a => a.RequestId).HasMaxLength(128).IsRequired();
            modelBuilder.Entity<AgentActionAudit>()
                .Property(a => a.Endpoint).HasMaxLength(128).IsRequired();
            modelBuilder.Entity<AgentActionAudit>()
                .Property(a => a.Source).HasMaxLength(64).IsRequired();
        }
    }

    public class LaptopTrackerDbContextFactory : IDesignTimeDbContextFactory<LaptopTrackerDbContext>
    {
        public LaptopTrackerDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();
            var optionsBuilder = new DbContextOptionsBuilder<LaptopTrackerDbContext>();
            optionsBuilder.UseSqlServer(config.GetConnectionString("DefaultConnection"));
            return new LaptopTrackerDbContext(optionsBuilder.Options);
        }
    }
}
