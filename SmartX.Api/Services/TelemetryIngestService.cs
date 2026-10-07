using System.Collections.Concurrent;
using SmartX.Api.Models;

namespace SmartX.Api.Services;

/// <summary>
/// Owns the ingestion pipeline: raw batches land first in a jagged array (one row per batch,
/// preserving arrival order), and are then flattened into a strongly-typed, generic
/// TelemetryPacket&lt;T&gt; history per sensor+metric using the custom BoundedHistory&lt;T&gt;
/// collection. Kept as three parallel float/int/bool stores (instead of one
/// Dictionary&lt;string, object&gt;) so every packet stays its true unmanaged T end-to-end -
/// no boxing, no runtime casts, no type-coercion bugs.
/// </summary>
public sealed class TelemetryIngestService
{
    private const int HistoryCapacityPerSeries = 500;

    // --- Stage 1: raw jagged-array batch staging (sequential historical batches) -------------
    // Each list entry is one ingest call's raw payload; the jagged array itself models the
    // "batch of N raw readings, not all sensors reporting the same N" shape a real mesh produces.
    private readonly ConcurrentDictionary<string, List<float[]>> _rawFloatBatches = new();

    // --- Stage 2: optimised, generic, per-series history --------------------------------------
    private readonly ConcurrentDictionary<string, BoundedHistory<TelemetryPacket<float>>> _floatHistory = new();
    private readonly ConcurrentDictionary<string, BoundedHistory<TelemetryPacket<int>>> _intHistory = new();
    private readonly ConcurrentDictionary<string, BoundedHistory<TelemetryPacket<bool>>> _boolHistory = new();

    private static string SeriesKey(string sensorId, string metric) => $"{sensorId}::{metric}";

    /// <summary>
    /// Stage a batch of raw float readings (e.g. a burst of soil-moisture samples) into the
    /// jagged array, then immediately flatten that batch into the optimised generic history.
    /// This is the multi-dimensional/jagged-array -> List&lt;T&gt; pipeline the brief asks for.
    /// </summary>
    public IReadOnlyList<TelemetryPacket<float>> IngestFloatBatch(string sensorId, string metric, float[] batch)
    {
        var key = SeriesKey(sensorId, metric);
        var batches = _rawFloatBatches.GetOrAdd(key, _ => new List<float[]>());
        lock (batches) { batches.Add(batch); } // jagged array of batches, one row per call

        var history = _floatHistory.GetOrAdd(key, _ => new BoundedHistory<TelemetryPacket<float>>(HistoryCapacityPerSeries));
        var packets = batch.Select(v => new TelemetryPacket<float>
        {
            SensorId = sensorId,
            MetricName = metric,
            Value = v
        }).ToList();
        history.AddRange(packets);
        return packets;
    }

    public TelemetryPacket<int> IngestInt(string sensorId, string metric, int value)
    {
        var packet = new TelemetryPacket<int> { SensorId = sensorId, MetricName = metric, Value = value };
        _intHistory.GetOrAdd(SeriesKey(sensorId, metric),
            _ => new BoundedHistory<TelemetryPacket<int>>(HistoryCapacityPerSeries)).Add(packet);
        return packet;
    }

    public TelemetryPacket<bool> IngestBool(string sensorId, string metric, bool value)
    {
        var packet = new TelemetryPacket<bool> { SensorId = sensorId, MetricName = metric, Value = value };
        _boolHistory.GetOrAdd(SeriesKey(sensorId, metric),
            _ => new BoundedHistory<TelemetryPacket<bool>>(HistoryCapacityPerSeries)).Add(packet);
        return packet;
    }

    public IReadOnlyList<TelemetryPacket<float>> GetFloatHistory(string sensorId, string metric) =>
        _floatHistory.TryGetValue(SeriesKey(sensorId, metric), out var h) ? h.ToList() : new();

    public IReadOnlyList<TelemetryPacket<int>> GetIntHistory(string sensorId, string metric) =>
        _intHistory.TryGetValue(SeriesKey(sensorId, metric), out var h) ? h.ToList() : new();

    public IReadOnlyList<TelemetryPacket<bool>> GetBoolHistory(string sensorId, string metric) =>
        _boolHistory.TryGetValue(SeriesKey(sensorId, metric), out var h) ? h.ToList() : new();

    /// <summary>Exposes the raw jagged staging area (mainly so the demo UI can show it exists).</summary>
    public float[][] GetRawBatches(string sensorId, string metric) =>
        _rawFloatBatches.TryGetValue(SeriesKey(sensorId, metric), out var batches)
            ? batches.ToArray()
            : Array.Empty<float[]>();

    /// <summary>
    /// Operator-overloading demo: aggregate the two most recent power readings from two meters,
    /// e.g. Meter3 = Meter1 + Meter2. Wired up to a small API endpoint for the frontend to call.
    /// </summary>
    public MeterReading AggregateLoad(MeterReading meter1, MeterReading meter2) => meter1 + meter2;

    public MeterReading Delta(MeterReading meter1, MeterReading meter2) => meter1 - meter2;
}
