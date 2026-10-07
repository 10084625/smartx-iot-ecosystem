namespace SmartX.Api.Services;

using SmartX.Api.Models;

public class TelemetryBufferService
{
    // Jagged array holding raw telemetry sample batches: [BatchIndex][SampleIndex]
    private float[][] _rawBatches = new float[5][];
    private int _batchCounter = 0;

    // Optimized Generic List Collection for active stream management
    public List<TelemetryPacket<float>> ActiveStream { get; private set; } = new();

    public TelemetryBufferService()
    {
        for (int i = 0; i < 5; i++)
        {
            _rawBatches[i] = new float[10]; // Allocate space for sample batches
        }
    }

    /// <summary>
    /// Processes raw telemetry sample arrays into generic List collections.
    /// </summary>
    public void ProcessRawBatch(float[] sampleArray, string macAddress)
    {
        // Store in raw jagged array
        _rawBatches[_batchCounter] = sampleArray;
        _batchCounter = (_batchCounter + 1) % 5;

        // Transfer raw sample data into optimized List<T> collection
        foreach (var sample in sampleArray)
        {
            ActiveStream.Add(new TelemetryPacket<float>(macAddress, sample));
        }
    }
}