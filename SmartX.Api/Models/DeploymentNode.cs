namespace SmartX.Api.Models;

/// <summary>
/// One level of a physical deployment tree, e.g. Facility A -> Zone 1 -> Sub-Zone B.
/// Nodes nest arbitrarily deep, which is exactly why validating them calls for recursion
/// rather than a fixed number of nested loops - a hydroponics facility might be 3 levels
/// deep today and 5 levels deep once a client subdivides a greenhouse further.
/// </summary>
public sealed class DeploymentNode
{
    public required string Name { get; init; }
    public List<DeploymentNode> Children { get; init; } = new();

    /// <summary>Leaf nodes may host an actual sensor MAC address; branch nodes are pure grouping.</summary>
    public string? SensorMac { get; init; }
}

public sealed record DeploymentValidationResult(bool IsValid, int DepthReached, List<string> Errors);
