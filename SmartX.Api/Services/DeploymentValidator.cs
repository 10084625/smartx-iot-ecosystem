using SmartX.Api.Models;

namespace SmartX.Api.Services;

/// <summary>
/// Validates that a sensor's deployment path (e.g. Sub-Zone B -> Zone 1 -> Facility A) is a
/// well-formed, safely-configured node inside the wider deployment tree. Implemented recursively
/// because the tree's depth is not fixed - a facility can be subdivided arbitrarily deeply, so an
/// iterative approach would need its own manual stack; recursion mirrors the tree's own structure.
/// </summary>
public sealed class DeploymentValidator
{
    private const int MaxSafeDepth = 8; // guards against pathological/cyclic input

    /// <summary>
    /// Recursively walks the tree from <paramref name="root"/> looking for a leaf whose
    /// SensorMac matches <paramref name="targetMac"/>. Base case: a leaf node (no children) is
    /// compared directly; recursive case: descend into every child and combine results.
    /// </summary>
    public DeploymentValidationResult ValidateNodePath(DeploymentNode root, string targetMac)
    {
        var errors = new List<string>();
        var found = Walk(root, targetMac, depth: 1, errors, out var depthReached);
        if (!found)
        {
            errors.Add($"No node with SensorMac '{targetMac}' was found under '{root.Name}'.");
        }
        return new DeploymentValidationResult(found && errors.Count == 0, depthReached, errors);
    }

    private bool Walk(DeploymentNode node, string targetMac, int depth, List<string> errors, out int deepestSeen)
    {
        deepestSeen = depth;

        if (string.IsNullOrWhiteSpace(node.Name))
        {
            errors.Add($"Node at depth {depth} has no name.");
        }

        if (depth > MaxSafeDepth)
        {
            errors.Add($"Depth {depth} at node '{node.Name}' exceeds the maximum safe nesting of {MaxSafeDepth}.");
            return false; // base case: unwind, tree is misconfigured
        }

        // Base case: leaf node - the recursion bottoms out here.
        if (node.Children.Count == 0)
        {
            return node.SensorMac == targetMac;
        }

        // Recursive case: descend into every child, tracking the deepest branch explored.
        var matched = false;
        foreach (var child in node.Children)
        {
            var childMatched = Walk(child, targetMac, depth + 1, errors, out var childDepth);
            deepestSeen = Math.Max(deepestSeen, childDepth);
            matched = matched || childMatched;
        }
        return matched;
    }

    /// <summary>Recursively checks the whole tree is well-formed (names present, depth safe).</summary>
    public DeploymentValidationResult ValidateTree(DeploymentNode root)
    {
        var errors = new List<string>();
        var deepest = CheckStructure(root, 1, errors);
        return new DeploymentValidationResult(errors.Count == 0, deepest, errors);
    }

    private int CheckStructure(DeploymentNode node, int depth, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(node.Name))
            errors.Add($"Unnamed node at depth {depth}.");

        if (depth > MaxSafeDepth)
        {
            errors.Add($"Node '{node.Name}' at depth {depth} exceeds max safe depth {MaxSafeDepth}.");
            return depth; // base case
        }

        if (node.Children.Count == 0) return depth; // base case: leaf

        var deepest = depth;
        foreach (var child in node.Children) // recursive case
        {
            deepest = Math.Max(deepest, CheckStructure(child, depth + 1, errors));
        }
        return deepest;
    }
}
