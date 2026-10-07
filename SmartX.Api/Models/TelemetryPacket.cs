namespace SmartX.Api.Models;

/// <summary>
/// Generic wrapper that lets the gateway carry ANY telemetry payload type (float soil-moisture
/// readings, int wattage counts, bool valve states, ...) through one uniform pipeline.
///
/// Why generics instead of "object payload" or a switch on a type-code string:
///   - No boxing/unboxing: a non-generic "object Value" field would box every struct
///     (float, int, bool) onto the heap on write, and unbox on every read. On a gateway
///     ingesting thousands of packets/sec from ESP32 nodes that overhead adds up fast.
///     TelemetryPacket&lt;T&gt; is a closed generic per value-type, so T stays on the stack
///     inside the packet instance - the JIT emits a specialised value-type layout for
///     TelemetryPacket&lt;float&gt;, TelemetryPacket&lt;int&gt;, TelemetryPacket&lt;bool&gt;, etc.
///   - No runtime type-coercion / casting bugs: the compiler enforces at compile time that a
///     "PowerWatts" packet can only ever be constructed with a numeric T, catching mismatches
///     that a stringly-typed payload would only surface at runtime.
///   - Single, reusable model: one class serves every current and future sensor type without
///     adding a new class per payload shape.
/// </summary>
/// <typeparam name="T">
/// The struct type carried by this packet (float, int, bool, double, ...). Constrained to
/// unmanaged so the payload is guaranteed to be a fixed-size, stack-friendly value type -
/// exactly the kind of data an ESP32 telemetry frame produces.
/// </typeparam>
public sealed class TelemetryPacket<T> where T : unmanaged
{
    public Guid PacketId { get; init; } = Guid.NewGuid();
    public required string SensorId { get; init; }
    public required string MetricName { get; init; }
    public required T Value { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public override string ToString() =>
        $"[{Timestamp:HH:mm:ss}] {SensorId}/{MetricName} = {Value} ({typeof(T).Name})";
}
