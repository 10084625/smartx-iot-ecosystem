using Microsoft.EntityFrameworkCore;
using SmartX.Api.Data;
using SmartX.Api.Models;

namespace SmartX.Api.Services;

/// <summary>
/// PostgreSQL-backed registry of every sensor the gateway knows about, keyed by MAC address.
/// Registered as Scoped (one instance per HTTP request) because it wraps a Scoped
/// SmartXDbContext - EF Core DbContexts are not thread-safe and must not be shared as
/// singletons across concurrent requests.
/// </summary>
public sealed class SensorRegistry
{
    private readonly SmartXDbContext _db;

    public SensorRegistry(SmartXDbContext db) => _db = db;

    public async Task<SensorRecord> RegisterAsync(SensorRecord record)
    {
        var existing = await _db.Sensors.FindAsync(record.MacAddress);
        if (existing is null)
        {
            _db.Sensors.Add(record);
        }
        else
        {
            _db.Entry(existing).CurrentValues.SetValues(record);
        }
        await _db.SaveChangesAsync();
        return existing ?? record;
    }

    public Task<SensorRecord?> GetAsync(string macAddress) =>
        _db.Sensors.Include(s => s.Attachments)
            .FirstOrDefaultAsync(s => s.MacAddress == macAddress);

    public Task<List<SensorRecord>> GetAllAsync() =>
        _db.Sensors.Include(s => s.Attachments).AsNoTracking().ToListAsync();

    public async Task<bool> AddAttachmentAsync(string macAddress, SensorAttachment attachment)
    {
        var exists = await _db.Sensors.AnyAsync(s => s.MacAddress == macAddress);
        if (!exists) return false;

        _db.Attachments.Add(attachment);
        await _db.SaveChangesAsync();
        return true;
    }
}
