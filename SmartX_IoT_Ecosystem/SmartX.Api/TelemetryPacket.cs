namespace SmartX.Api.Models;

/// <summary>
/// Reusable generic wrapper handling disparate sensor data types uniformly without boxing/unboxing overhead.
/// </summary>
public class TelemetryPacket<T> where T : struct, IComparable, IComparable<T>, IEquatable<T>
{
    public string MacAddress { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public T Value { get; set; }

    public TelemetryPacket() { }

    public TelemetryPacket(string macAddress, T value)
    {
        MacAddress = macAddress;
        Value = value;
    }

    // --- OPERATOR OVERLOADING ---

    // Aggregate values (e.g., Meter3 = Meter1 + Meter2)
    public static TelemetryPacket<T> operator +(TelemetryPacket<T> a, TelemetryPacket<T> b)
    {
        dynamic valA = a.Value;
        dynamic valB = b.Value;
        return new TelemetryPacket<T>(a.MacAddress, (T)(valA + valB));
    }

    // Delta comparison variance
    public static TelemetryPacket<T> operator -(TelemetryPacket<T> a, TelemetryPacket<T> b)
    {
        dynamic valA = a.Value;
        dynamic valB = b.Value;
        return new TelemetryPacket<T>(a.MacAddress, (T)(valA - valB));
    }

    // Comparison threshold operators
    public static bool operator >(TelemetryPacket<T> a, TelemetryPacket<T> b) => a.Value.CompareTo(b.Value) > 0;
    public static bool operator <(TelemetryPacket<T> a, TelemetryPacket<T> b) => a.Value.CompareTo(b.Value) < 0;
}