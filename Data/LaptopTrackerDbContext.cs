using LaptopTracker.Models;
using LaptopTracker.Models.Agent;
using LaptopTracker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LaptopTracker.Data
{
    public class LaptopTrackerDbContext : DbContext
    {
        private readonly ICurrentUser? _currentUser;
        private readonly IHttpContextAccessor? _http;

        public LaptopTrackerDbContext(
            DbContextOptions<LaptopTrackerDbContext> options,
            ICurrentUser? currentUser = null,
            IHttpContextAccessor? http = null) : base(options)
        {
            _currentUser = currentUser;
            _http = http;
        }

        public DbSet<ReturnDevice> ReturnDevices { get; set; } = null!;
        public DbSet<ReturnDevicePhoto> ReturnDevicePhotos { get; set; } = null!;
        public DbSet<WicStockDevice> WicStockDevices { get; set; } = null!;
        public DbSet<LoanerDevice> LoanerDevices { get; set; } = null!;
        public DbSet<Handover> Handovers { get; set; } = null!;
        public DbSet<HandoverDevice> HandoverDevices { get; set; } = null!;
        public DbSet<IdempotencyRecord> IdempotencyRecords { get; set; } = null!;
        public DbSet<AgentActionAudit> AgentActionAudits { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ReturnDevice>()
                .Property(d => d.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            modelBuilder.Entity<ReturnDevice>()
                .HasQueryFilter(d => !d.IsDeleted);

            modelBuilder.Entity<WicStockDevice>()
                .Property(d => d.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            modelBuilder.Entity<WicStockDevice>()
                .HasQueryFilter(d => !d.IsDeleted);

            modelBuilder.Entity<LoanerDevice>()
                .Property(d => d.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            modelBuilder.Entity<LoanerDevice>()
                .HasQueryFilter(d => !d.IsDeleted);

            modelBuilder.Entity<HandoverDevice>()
                .HasOne(hd => hd.Handover)
                .WithMany(h => h.Devices)
                .HasForeignKey(hd => hd.HandoverId);

            modelBuilder.Entity<ReturnDevicePhoto>()
                .HasOne(p => p.ReturnDevice)
                .WithMany(d => d.Photos)
                .HasForeignKey(p => p.ReturnDeviceId);
            modelBuilder.Entity<ReturnDevicePhoto>()
                .HasQueryFilter(p => !p.ReturnDevice.IsDeleted);

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
            modelBuilder.Entity<AgentActionAudit>()
                .Property(a => a.Kid).HasMaxLength(32);

            modelBuilder.Entity<AuditLog>(e =>
            {
                e.Property(x => x.Kid).HasMaxLength(32).IsRequired();
                e.Property(x => x.Action).HasMaxLength(32).IsRequired();
                e.Property(x => x.EntityName).HasMaxLength(64);
                e.Property(x => x.EntityId).HasMaxLength(64);
                e.Property(x => x.EntityLabel).HasMaxLength(128);
                e.Property(x => x.IpAddress).HasMaxLength(64);
                e.Property(x => x.Route).HasMaxLength(256);
                e.Property(x => x.Source).HasMaxLength(16).IsRequired();
                e.Property(x => x.RequestId).HasMaxLength(128);
                e.HasIndex(x => x.TimestampUtc);
                e.HasIndex(x => x.Kid);
                e.HasIndex(x => new { x.EntityName, x.EntityId });
            });
        }

        // ── Audit Engine ──────────────────────────────────────────────────────

        private static readonly HashSet<string> Audited = new()
        {
            nameof(ReturnDevice), nameof(LoanerDevice), nameof(WicStockDevice),
            nameof(ReturnDevicePhoto), nameof(Handover), nameof(HandoverDevice)
        };

        private static readonly HashSet<string> NeverAudit = new()
        {
            nameof(AuditLog), nameof(IdempotencyRecord), nameof(AgentActionAudit)
        };

        public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            var pending = CaptureAuditEntries();
            var result = await base.SaveChangesAsync(ct);

            if (pending.Count > 0)
            {
                foreach (var a in pending) a.ResolveGeneratedKeys();
                AuditLogs.AddRange(pending.Select(a => a.ToLog()));
                await base.SaveChangesAsync(ct);
            }
            return result;
        }

        private List<AuditEntry> CaptureAuditEntries()
        {
            ChangeTracker.DetectChanges();

            var kid    = _currentUser?.Kid ?? "ANONYMOUS";
            var ip     = _http?.HttpContext?.Connection.RemoteIpAddress?.ToString();
            var route  = _http?.HttpContext?.Request.Path.Value;
            var source = route?.StartsWith("/api/agent", StringComparison.OrdinalIgnoreCase) == true
                         ? "AgentApi" : (route is null ? "System" : "Web");

            var list = new List<AuditEntry>();

            foreach (var entry in ChangeTracker.Entries())
            {
                var name = entry.Entity.GetType().Name;
                if (NeverAudit.Contains(name) || !Audited.Contains(name)) continue;
                if (entry.State is EntityState.Detached or EntityState.Unchanged) continue;

                var audit = new AuditEntry(entry)
                {
                    Kid         = kid,
                    IpAddress   = ip,
                    Route       = route,
                    Source      = source,
                    EntityName  = name,
                    EntityLabel = TryGetLabel(entry)
                };

                switch (entry.State)
                {
                    case EntityState.Added:
                        audit.Action = "Create";
                        foreach (var p in entry.Properties.Where(p => !p.Metadata.IsPrimaryKey()))
                            audit.NewValues[p.Metadata.Name] = p.CurrentValue;
                        break;

                    case EntityState.Deleted:
                        audit.Action = "HardDelete";
                        foreach (var p in entry.Properties)
                            audit.OldValues[p.Metadata.Name] = p.OriginalValue;
                        break;

                    case EntityState.Modified:
                        foreach (var p in entry.Properties.Where(p => p.IsModified))
                        {
                            if (Equals(p.OriginalValue, p.CurrentValue)) continue;
                            audit.OldValues[p.Metadata.Name] = p.OriginalValue;
                            audit.NewValues[p.Metadata.Name] = p.CurrentValue;
                        }
                        if (audit.NewValues.Count == 0) continue;

                        audit.Action = audit.NewValues.ContainsKey("IsDeleted")
                            ? (Equals(audit.NewValues["IsDeleted"], true) ? "SoftDelete" : "Restore")
                            : "Update";
                        break;
                }

                foreach (var p in entry.Properties.Where(p => p.Metadata.IsPrimaryKey()))
                {
                    if (p.IsTemporary) audit.TemporaryKeys.Add(p);
                    else audit.EntityId = p.CurrentValue?.ToString() ?? "";
                }

                list.Add(audit);
            }
            return list;
        }

        private static string? TryGetLabel(EntityEntry entry) =>
            entry.Properties.FirstOrDefault(p => p.Metadata.Name == "SerialNumber")?.CurrentValue?.ToString()
            ?? entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Name")?.CurrentValue?.ToString();
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
