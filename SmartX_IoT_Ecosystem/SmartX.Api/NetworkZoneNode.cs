namespace SmartX.Api.Models;

public class NetworkZoneNode
{
    public string ZoneName { get; set; } = string.Empty;
    public bool IsSafe { get; set; } = true;
    public List<NetworkZoneNode> SubZones { get; set; } = new();

    public NetworkZoneNode(string name, bool isSafe = true)
    {
        ZoneName = name;
        IsSafe = isSafe;
    }

    /// <summary>
    /// Recursive validation algorithm parsing nested deployment trees.
    /// </summary>
    public bool ValidateHierarchyRecursively()
    {
        // Base case: If current sub-zone is flagged unsafe, fail immediately
        if (!IsSafe) return false;

        // Recursive case: Validate all nested sub-zones depth-first
        foreach (var subZone in SubZones)
        {
            if (!subZone.ValidateHierarchyRecursively())
            {
                return false; // Propagate failure up the tree
            }
        }

        return true;
    }
}