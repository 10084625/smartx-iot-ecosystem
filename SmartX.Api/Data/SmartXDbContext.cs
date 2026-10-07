using Microsoft.EntityFrameworkCore;
using SmartX.Api.Models;

namespace SmartX.Api.Data;

/// <summary>
/// EF Core gateway to PostgreSQL. Holds registered sensors, their attachments, and a durable
/// telemetry log - the persistence layer that sits behind the in-memory ingestion pipeline
/// (TelemetryIngestService / BoundedHistory&lt;T&gt;) used for live processing.
/// </summary>
public sealed class SmartXDbContext : DbContext
{
    public SmartXDbContext(DbContextOptions<SmartXDbContext> options) : base(options) { }

    public DbSet<SensorRecord> Sensors => Set<SensorRecord>();
    public DbSet<SensorAttachment> Attachments => Set<SensorAttachment>();
    public DbSet<TelemetryReading> Readings => Set<TelemetryReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SensorRecord>(e =>
        {
            e.ToTable("sensors");
            e.HasKey(s => s.MacAddress);
            e.Property(s => s.MacAddress).HasMaxLength(64);
            e.Property(s => s.DeploymentLocation).HasMaxLength(256).IsRequired();
            e.Property(s => s.Category).HasConversion<string>().HasMaxLength(32);
            e.HasMany(s => s.Attachments)
                .WithOne()
                .HasForeignKey(a => a.SensorMacAddress)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SensorAttachment>(e =>
        {
            e.ToTable("sensor_attachments");
            e.HasKey(a => a.Id);
            e.Property(a => a.FileName).HasMaxLength(260).IsRequired();
            e.Property(a => a.ContentType).HasMaxLength(128).IsRequired();
            e.Property(a => a.StoredPath).HasMaxLength(1024).IsRequired();
        });

        modelBuilder.Entity<TelemetryReading>(e =>
        {
            e.ToTable("telemetry_readings");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).ValueGeneratedOnAdd();
            e.Property(r => r.SensorId).HasMaxLength(64).IsRequired();
            e.Property(r => r.Metric).HasMaxLength(128).IsRequired();
            e.Property(r => r.ValueType).HasMaxLength(16).IsRequired();
            e.Property(r => r.ValueText).HasMaxLength(64).IsRequired();
            e.HasIndex(r => new { r.SensorId, r.Metric, r.Timestamp }); // range/timeline queries
        });
    }
}
