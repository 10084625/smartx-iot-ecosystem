namespace SmartX.Api.Models;

/// <summary>
/// A single smart-meter power reading (watts) tied to a point in time. Operators are
/// overloaded so the gateway can express meter maths the way the domain thinks about it -
/// "Meter3 = Meter1 + Meter2" for aggregate load, "Meter1 - Meter2" for a delta between two
/// polls - instead of manually plucking .Watts out of two objects everywhere in the codebase.
/// </summary>
public readonly struct MeterReading
{
    public string NodeId { get; }
    public double Watts { get; }
    public DateTimeOffset Timestamp { get; }

    public MeterReading(string nodeId, double watts, DateTimeOffset? timestamp = null)
    {
        NodeId = nodeId;
        Watts = watts;
        Timestamp = timestamp ?? DateTimeOffset.UtcNow;
    }

    /// <summary>Aggregate load of two meters, e.g. Meter3 = Meter1 + Meter2.</summary>
    public static MeterReading operator +(MeterReading a, MeterReading b) =>
        new($"{a.NodeId}+{b.NodeId}", a.Watts + b.Watts, DateTimeOffset.UtcNow);

    /// <summary>Delta between two polls of the same (or different) meter(s).</summary>
    public static MeterReading operator -(MeterReading a, MeterReading b) =>
        new($"{a.NodeId}-{b.NodeId}", a.Watts - b.Watts, DateTimeOffset.UtcNow);

    /// <summary>Lets alert logic write "if (reading &gt; threshold)" directly.</summary>
    public static bool operator >(MeterReading a, MeterReading b) => a.Watts > b.Watts;
    public static bool operator <(MeterReading a, MeterReading b) => a.Watts < b.Watts;
    public static bool operator >=(MeterReading a, MeterReading b) => a.Watts >= b.Watts;
    public static bool operator <=(MeterReading a, MeterReading b) => a.Watts <= b.Watts;

    public override string ToString() => $"{NodeId}: {Watts:F2} W @ {Timestamp:HH:mm:ss}";
}
