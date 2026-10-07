using Microsoft.EntityFrameworkCore;
using SmartX.Api.Data;
using SmartX.Api.Models;
using SmartX.Api.Services;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

var MacAddressPattern = new Regex(@"^([0-9A-Fa-f]{2}:){5}[0-9A-Fa-f]{2}$", RegexOptions.Compiled);

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// ---------------------------------------------------------------------------------------------
// Database: PostgreSQL via EF Core. Connection string comes from configuration/env var so the
// same image works locally (appsettings.Development.json) and in docker-compose
// (ConnectionStrings__SmartX env var pointing at the "smartx-db" service).
// ---------------------------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("SmartX")
    ?? "Host=localhost;Port=5432;Database=smartx;Username=smartx;Password=smartx";

builder.Services.AddDbContext<SmartXDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddScoped<SensorRegistry>();          // wraps a scoped DbContext
builder.Services.AddSingleton<TelemetryIngestService>(); // pure in-memory pipeline (generics/arrays/collections)
builder.Services.AddSingleton<DeploymentValidator>();
builder.Services.AddSingleton<EngagementService>();
builder.Services.AddScoped<AttachmentService>();

const string ClientCorsPolicy = "SmartXClient";
builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientCorsPolicy, policy =>
        policy.WithOrigins(
                "http://localhost:5173",  // Vite dev server
                "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();
app.UseCors(ClientCorsPolicy);

// Apply pending EF Core migrations on startup so `docker compose up` gives a ready database
// with zero manual steps. Safe to call every start - it's a no-op once migrations are applied.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SmartXDbContext>();
    await db.Database.MigrateAsync();
}

// ---------------------------------------------------------------------------------------------
// Pillar status - drives the three-pillar landing menu on the client (2 of 3 disabled in Part 1)
// ---------------------------------------------------------------------------------------------
app.MapGet("/api/pillars", () => Results.Ok(new[]
{
    new { Key = "ingestion", Title = "Sensor Data Ingestion and Telemetry", Enabled = true },
    new { Key = "commands", Title = "Real-Time Command Stream and History", Enabled = false },
    new { Key = "topology", Title = "Network Topology and Mesh Routing", Enabled = false }
}));

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", utc = DateTimeOffset.UtcNow }));

// ---------------------------------------------------------------------------------------------
// Sensor registration and management (PostgreSQL-backed via SensorRegistry)
// ---------------------------------------------------------------------------------------------
var sensors = app.MapGroup("/api/sensors");

sensors.MapGet("/", async (SensorRegistry registry) => Results.Ok(await registry.GetAllAsync()));

sensors.MapGet("/{mac}", async (string mac, SensorRegistry registry) =>
    await registry.GetAsync(mac) is { } record ? Results.Ok(record) : Results.NotFound());

sensors.MapPost("/", async (SensorRecord record, SensorRegistry registry) =>
{
    if (string.IsNullOrWhiteSpace(record.MacAddress))
        return Results.BadRequest(new { error = "MacAddress is required." });
    if (!MacAddressPattern.IsMatch(record.MacAddress))
        return Results.BadRequest(new { error = "MacAddress must look like AA:BB:CC:DD:EE:FF." });
    if (string.IsNullOrWhiteSpace(record.DeploymentLocation))
        return Results.BadRequest(new { error = "DeploymentLocation is required." });

    var saved = await registry.RegisterAsync(record);
    return Results.Created($"/api/sensors/{saved.MacAddress}", saved);
});

sensors.MapPost("/{mac}/attachments", async (
    string mac, IFormFile file, SensorRegistry registry, AttachmentService attachments, CancellationToken ct) =>
{
    if (await registry.GetAsync(mac) is null) return Results.NotFound(new { error = $"Unknown sensor '{mac}'." });
    if (file is null || file.Length == 0) return Results.BadRequest(new { error = "No file supplied." });

    var saved = await attachments.SaveAsync(mac, file, ct);
    await registry.AddAttachmentAsync(mac, saved);
    return Results.Ok(saved);
}).DisableAntiforgery();

// ---------------------------------------------------------------------------------------------
// Telemetry ingestion: generics + jagged arrays -> List<T> pipeline (in-memory, for live
// processing) PLUS a durable write to PostgreSQL (for history/timeline queries in later parts).
// ---------------------------------------------------------------------------------------------
var telemetry = app.MapGroup("/api/telemetry");

telemetry.MapPost("/float-batch", async (FloatBatchRequest req, TelemetryIngestService svc, SmartXDbContext db) =>
{
    var packets = svc.IngestFloatBatch(req.SensorId, req.Metric, req.Values);

    db.Readings.AddRange(packets.Select(p => new TelemetryReading
    {
        SensorId = p.SensorId,
        Metric = p.MetricName,
        ValueType = "float",
        ValueText = p.Value.ToString("R"),
        Timestamp = p.Timestamp
    }));
    await db.SaveChangesAsync();

    return Results.Ok(packets);
});

telemetry.MapPost("/int", async (ScalarRequest<int> req, TelemetryIngestService svc, SmartXDbContext db) =>
{
    var packet = svc.IngestInt(req.SensorId, req.Metric, req.Value);
    db.Readings.Add(new TelemetryReading
    {
        SensorId = packet.SensorId, Metric = packet.MetricName,
        ValueType = "int", ValueText = packet.Value.ToString(), Timestamp = packet.Timestamp
    });
    await db.SaveChangesAsync();
    return Results.Ok(packet);
});

telemetry.MapPost("/bool", async (ScalarRequest<bool> req, TelemetryIngestService svc, SmartXDbContext db) =>
{
    var packet = svc.IngestBool(req.SensorId, req.Metric, req.Value);
    db.Readings.Add(new TelemetryReading
    {
        SensorId = packet.SensorId, Metric = packet.MetricName,
        ValueType = "bool", ValueText = packet.Value.ToString(), Timestamp = packet.Timestamp
    });
    await db.SaveChangesAsync();
    return Results.Ok(packet);
});

telemetry.MapGet("/{sensorId}/{metric}/float", (string sensorId, string metric, TelemetryIngestService svc) =>
    Results.Ok(svc.GetFloatHistory(sensorId, metric)));

telemetry.MapGet("/{sensorId}/{metric}/int", (string sensorId, string metric, TelemetryIngestService svc) =>
    Results.Ok(svc.GetIntHistory(sensorId, metric)));

telemetry.MapGet("/{sensorId}/{metric}/bool", (string sensorId, string metric, TelemetryIngestService svc) =>
    Results.Ok(svc.GetBoolHistory(sensorId, metric)));

telemetry.MapGet("/{sensorId}/{metric}/raw-batches", (string sensorId, string metric, TelemetryIngestService svc) =>
    Results.Ok(svc.GetRawBatches(sensorId, metric)));

// Durable, database-backed history - proves the PostgreSQL round trip independently of the
// in-memory BoundedHistory<T> window, and is what a future timeline/range-query view would use.
telemetry.MapGet("/{sensorId}/{metric}/db-history", async (string sensorId, string metric, SmartXDbContext db) =>
    Results.Ok(await db.Readings
        .Where(r => r.SensorId == sensorId && r.Metric == metric)
        .OrderBy(r => r.Timestamp)
        .ToListAsync()));

// ---------------------------------------------------------------------------------------------
// Operator overloading demo (MeterReading + / -)
// ---------------------------------------------------------------------------------------------
app.MapPost("/api/meters/aggregate", (MeterAggregateRequest req, TelemetryIngestService svc) =>
{
    var m1 = new MeterReading(req.Meter1Id, req.Meter1Watts);
    var m2 = new MeterReading(req.Meter2Id, req.Meter2Watts);
    return Results.Ok(new
    {
        aggregate = svc.AggregateLoad(m1, m2).ToString(),
        delta = svc.Delta(m1, m2).ToString()
    });
});

// ---------------------------------------------------------------------------------------------
// Recursive deployment-tree validation
// ---------------------------------------------------------------------------------------------
app.MapPost("/api/deployment/validate", (DeploymentValidationRequest req, DeploymentValidator validator) =>
    Results.Ok(validator.ValidateNodePath(req.Root, req.TargetMac)));

// ---------------------------------------------------------------------------------------------
// Dynamic engagement feature (gamified health score)
// ---------------------------------------------------------------------------------------------
app.MapGet("/api/engagement/{sensorId}/{metric}", (string sensorId, string metric, EngagementService svc) =>
    Results.Ok(svc.Evaluate(sensorId, metric)));

app.Run();

// -------------------------------------------------------------------------------------------
// Request DTOs
// -------------------------------------------------------------------------------------------
record FloatBatchRequest(string SensorId, string Metric, float[] Values);
record ScalarRequest<T>(string SensorId, string Metric, T Value);
record MeterAggregateRequest(string Meter1Id, double Meter1Watts, string Meter2Id, double Meter2Watts);
record DeploymentValidationRequest(DeploymentNode Root, string TargetMac);
