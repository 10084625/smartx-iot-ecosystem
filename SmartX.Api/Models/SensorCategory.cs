namespace SmartX.Api.Models;

/// <summary>
/// The three broad classes of Smart-X telemetry sources. Kept small and closed (an enum,
/// not a free-text string) so the ingestion pipeline can validate incoming registrations
/// without a database round trip.
/// </summary>
public enum SensorCategory
{
    Environmental,
    PowerConsumption,
    Actuator
}
