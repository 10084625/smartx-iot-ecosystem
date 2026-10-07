using SmartX.Api.Models;

namespace SmartX.Api.Services;

public sealed record SensorHealth(
    string SensorId,
    int HealthScore,      // 0-100 gamified score, not a bare progress bar
    string StatusLabel,   // "Nominal" | "Watch" | "Anomaly" | "Silent"
    string StreakLabel,   // e.g. "4h 12m clean streak"
    bool AnomalyDetected
);

/// <summary>
/// Implements the chosen research strategy from Task 1: proactive, gamified anomaly feedback.
/// Rather than a static progress bar, each sensor gets a live "Health Score" that reacts to
/// (a) how stale its last reading is, and (b) whether its recent readings deviate sharply from
/// its own rolling average (a simple z-score-style spike check) - so an operator glancing at the
/// dashboard sees WHICH sensor needs attention and WHY, before they'd have noticed on a raw chart.
/// </summary>
public sealed class EngagementService
{
    private readonly TelemetryIngestService _telemetry;

    public EngagementService(TelemetryIngestService telemetry) => _telemetry = telemetry;

    public SensorHealth Evaluate(string sensorId, string metric)
    {
        var history = _telemetry.GetFloatHistory(sensorId, metric);

        if (history.Count == 0)
        {
            return new SensorHealth(sensorId, 0, "Silent", "No data yet", AnomalyDetected: false);
        }

        var lastSeen = history[^1].Timestamp;
        var secondsSinceLast = (DateTimeOffset.UtcNow - lastSeen).TotalSeconds;

        var values = history.Select(p => (double)p.Value).ToList();
        var mean = values.Average();
        var stdDev = Math.Sqrt(values.Sum(v => Math.Pow(v - mean, 2)) / Math.Max(1, values.Count - 1));
        var latest = values[^1];
        var zScore = stdDev < 0.0001 ? 0 : Math.Abs(latest - mean) / stdDev;

        var anomaly = zScore > 2.5; // > 2.5 std devs from its own rolling mean = spike/drop
        var stale = secondsSinceLast > 60;

        int score = 100;
        if (stale) score -= 40;
        if (anomaly) score -= 35;
        score = Math.Clamp(score, 0, 100);

        var status = (stale, anomaly) switch
        {
            (true, _) => "Silent",
            (_, true) => "Anomaly",
            _ when score < 85 => "Watch",
            _ => "Nominal"
        };

        var streak = anomaly || stale
            ? "Streak reset"
            : $"{values.Count} clean reading{(values.Count == 1 ? "" : "s")} in a row";

        return new SensorHealth(sensorId, score, status, streak, anomaly);
    }
}
