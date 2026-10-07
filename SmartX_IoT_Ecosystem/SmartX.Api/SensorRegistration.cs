namespace SmartX.Api.Models;

public class SensorRegistration
{
    public string MacAddress { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Environmental, Power, Actuator
    public string? LogFilePath { get; set; }
} 