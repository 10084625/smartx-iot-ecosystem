using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Models;
using SmartX.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Register services & CORS
builder.Services.AddSingleton<TelemetryBufferService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();
app.UseCors();

// 1. Gateway Architectural Pillars Endpoint
app.MapGet("/api/gateway/pillars", () => new[]
{
    new { Id = 1, Title = "Sensor Data Ingestion and Telemetry", Status = "Active" },
    new { Id = 2, Title = "Real-Time Command Stream and History", Status = "Disabled (Part 2)" },
    new { Id = 3, Title = "Network Topology and Mesh Routing", Status = "Disabled (PoE)" }
});

// 2. Telemetry Ingestion Endpoint (Generics)
app.MapPost("/api/telemetry/ingest", ([FromBody] TelemetryPacket<float> packet, TelemetryBufferService buffer) =>
{
    buffer.ActiveStream.Add(packet);
    return Results.Ok(new { Message = "Telemetry Ingested Successfully", Data = packet });
});

// 3. Batch Sample Ingestion (Jagged Arrays to List<T>)
app.MapPost("/api/telemetry/ingest-batch/{mac}", (string mac, [FromBody] float[] samples, TelemetryBufferService buffer) =>
{
    buffer.ProcessRawBatch(samples, mac);
    return Results.Ok(new { Message = "Batch Stream Buffer Processed", TotalStreamCount = buffer.ActiveStream.Count });
});

// 4. Validate Deployment Tree Endpoint (Recursion)
app.MapPost("/api/sensor/validate-zone", ([FromBody] NetworkZoneNode rootZone) =>
{
    bool isValid = rootZone.ValidateHierarchyRecursively();
    return Results.Ok(new { Zone = rootZone.ZoneName, IsSafeDeployment = isValid });
});

// 5. Multipart Hardware Log/Media File Upload Endpoint
app.MapPost("/api/sensor/upload-log/{macAddress}", async (string macAddress, IFormFile file) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("Invalid hardware log file uploaded.");

    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "UploadedLogs");
    Directory.CreateDirectory(uploadsFolder);

    var filePath = Path.Combine(uploadsFolder, $"{macAddress}_{file.FileName}");
    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    return Results.Ok(new { MacAddress = macAddress, SavedFilePath = filePath, FileSizeBytes = file.Length });
});

app.Run();