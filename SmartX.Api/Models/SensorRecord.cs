namespace SmartX.Api.Models;

/// <summary>
/// A registered physical (or simulated) sensor node. Doubles as both the API's JSON
/// contract and the EF Core entity persisted to PostgreSQL - the MAC address is the
/// primary key both in the database and as the dictionary-style lookup key used
/// elsewhere in the pipeline.
/// </summary>
public sealed class SensorRecord
{
    public required string MacAddress { get; init; }   // primary key
    public required string DeploymentLocation { get; init; }
    public required SensorCategory Category { get; init; }
    public DateTimeOffset RegisteredAt { get; init; } = DateTimeOffset.UtcNow;

    // Navigation property - EF Core loads this via .Include(); mutable because EF
    // materialises/populates collections after construction.
    public List<SensorAttachment> Attachments { get; init; } = new();
}

/// <summary>A config file / deployment photo / hardware log attached to a sensor profile.</summary>
public sealed class SensorAttachment
{
    public Guid Id { get; init; } = Guid.NewGuid();

    // Foreign key back to the owning sensor.
    public required string SensorMacAddress { get; init; }

    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
    public required string StoredPath { get; init; }
    public DateTimeOffset UploadedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>Flat, JSON-friendly shape used to accept a telemetry reading over the API.</summary>
public sealed class TelemetryReadingDto
{
    public required string SensorId { get; init; }
    public required string MetricName { get; init; }
    public required string ValueType { get; init; } // "float" | "int" | "bool"
    public required string Value { get; init; }      // raw value, parsed server-side per ValueType
}

/// <summary>
/// Persisted, queryable log of every ingested reading - the durable counterpart to the
/// in-memory TelemetryPacket&lt;T&gt; pipeline. Kept as a separate table/type deliberately:
/// TelemetryPacket&lt;T&gt; exists to satisfy the generics/no-boxing requirement for the live
/// processing path, while TelemetryReading is the flat, database-friendly row shape EF Core
/// maps to PostgreSQL for durable history (e.g. what Part 2/3 timeline views will query).
/// </summary>
public sealed class TelemetryReading
{
    public long Id { get; init; } // identity PK
    public required string SensorId { get; init; }
    public required string Metric { get; init; }
    public required string ValueType { get; init; } // "float" | "int" | "bool"
    public required string ValueText { get; init; }  // value serialised as text, typed by ValueType
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
