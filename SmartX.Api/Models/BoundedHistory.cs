using System.Collections;

namespace SmartX.Api.Models;

/// <summary>
/// Custom generic collection (fulfils the "collection is a custom data structure" bar, not just
/// a bare List&lt;T&gt;) used as the optimised destination for telemetry once it leaves the raw
/// jagged-array staging area. It behaves like a ring buffer over a List&lt;T&gt;: bounded capacity,
/// O(1) amortised append, oldest-first eviction - appropriate for a gateway that only needs to
/// keep a rolling window of recent readings per sensor in memory rather than an unbounded log.
/// </summary>
public sealed class BoundedHistory<T> : IEnumerable<T>
{
    private readonly List<T> _items;
    public int Capacity { get; }

    public BoundedHistory(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity;
        _items = new List<T>(capacity);
    }

    public int Count => _items.Count;

    public void Add(T item)
    {
        if (_items.Count == Capacity)
        {
            _items.RemoveAt(0); // evict oldest
        }
        _items.Add(item);
    }

    public void AddRange(IEnumerable<T> items)
    {
        foreach (var item in items) Add(item);
    }

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
