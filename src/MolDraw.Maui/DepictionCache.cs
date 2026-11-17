using MolDraw.Depiction;

namespace MolDraw.Maui;

/// <summary>Caches depictions computed from SMILES so that re-binding the same molecule does not repeat parsing and layout.</summary>
/// <remarks>Implementations must be thread-safe.</remarks>
public interface IDepictionCache
{
    /// <summary>Tries to get a cached depiction.</summary>
    /// <param name="smiles">SMILES string.</param>
    /// <param name="options">Build options used.</param>
    /// <param name="depiction">Cached depiction.</param>
    bool TryGet(string smiles, DepictionBuildOptions options, out MoleculeDepiction depiction);

    /// <summary>Stores a depiction.</summary>
    /// <param name="smiles">SMILES string.</param>
    /// <param name="options">Build options used.</param>
    /// <param name="depiction">Depiction.</param>
    void Set(string smiles, DepictionBuildOptions options, MoleculeDepiction depiction);
}

/// <summary>Thread-safe least-recently-used <see cref="IDepictionCache"/>.</summary>
public sealed class DepictionCache : IDepictionCache
{
    private readonly int _capacity;
    private readonly Dictionary<(string, DepictionBuildOptions), LinkedListNode<((string, DepictionBuildOptions) Key, MoleculeDepiction Value)>> _map = [];
    private readonly LinkedList<((string, DepictionBuildOptions) Key, MoleculeDepiction Value)> _order = new();
    private readonly Lock _lock = new();

    /// <summary>Creates a cache.</summary>
    /// <param name="capacity">Maximum number of entries (default 256).</param>
    public DepictionCache(int capacity = 256)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _capacity = capacity;
    }

    /// <summary>Number of cached entries.</summary>
    public int Count
    {
        get
        {
            lock (_lock)
                return _map.Count;
        }
    }

    /// <inheritdoc />
    public bool TryGet(string smiles, DepictionBuildOptions options, out MoleculeDepiction depiction)
    {
        lock (_lock)
        {
            if (_map.TryGetValue((smiles, options), out var node))
            {
                _order.Remove(node);
                _order.AddFirst(node);
                depiction = node.Value.Value;
                return true;
            }
        }

        depiction = MoleculeDepiction.Empty;
        return false;
    }

    /// <inheritdoc />
    public void Set(string smiles, DepictionBuildOptions options, MoleculeDepiction depiction)
    {
        ArgumentNullException.ThrowIfNull(smiles);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(depiction);
        var key = (smiles, options);
        lock (_lock)
        {
            if (_map.TryGetValue(key, out var existing))
                _order.Remove(existing);
            var node = _order.AddFirst((key, depiction));
            _map[key] = node;
            while (_map.Count > _capacity)
            {
                var last = _order.Last!;
                _order.RemoveLast();
                _map.Remove(last.Value.Key);
            }
        }
    }
}
