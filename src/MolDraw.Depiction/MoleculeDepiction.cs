namespace MolDraw.Depiction;

/// <summary>
/// Immutable, toolkit-neutral 2D depiction of a molecule. Produced by <c>MolDraw.Core</c> from NCDK molecules,
/// but can equally be created from any other chemistry toolkit (e.g. RDKit coordinates) and rendered by <c>MolDraw.Skia</c>.
/// </summary>
/// <remarks>
/// Invariants (checked on construction): <c>Atoms[i].Index == i</c>, <c>Bonds[i].Index == i</c> and every bond references
/// two distinct, existing atoms. Coordinates are in model units with the y axis pointing up; any bond length works,
/// renderers scale the depiction to the median bond length.
/// </remarks>
public sealed record MoleculeDepiction
{
    private readonly IReadOnlyList<DepictionAtom> _atoms = [];
    private readonly IReadOnlyList<DepictionBond> _bonds = [];
    private readonly IReadOnlyList<DepictionRing> _rings = [];

    /// <summary>An empty depiction.</summary>
    public static MoleculeDepiction Empty { get; } = new([], []);

    /// <summary>Creates a depiction.</summary>
    /// <param name="atoms">Atoms; <c>atoms[i].Index</c> must equal <c>i</c>.</param>
    /// <param name="bonds">Bonds; <c>bonds[i].Index</c> must equal <c>i</c>.</param>
    /// <exception cref="ArgumentException">An invariant is violated.</exception>
    public MoleculeDepiction(IReadOnlyList<DepictionAtom> atoms, IReadOnlyList<DepictionBond> bonds)
    {
        Atoms = atoms;
        Bonds = bonds;
    }

    /// <summary>Atoms; index == atom index of the source molecule.</summary>
    public IReadOnlyList<DepictionAtom> Atoms
    {
        get => _atoms;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            for (var i = 0; i < value.Count; i++)
            {
                if (value[i] is null)
                    throw new ArgumentException($"Atom {i} is null.", nameof(Atoms));
                if (value[i].Index != i)
                    throw new ArgumentException($"Atom at position {i} has Index {value[i].Index}; indices must match positions.", nameof(Atoms));
                if (!double.IsFinite(value[i].X) || !double.IsFinite(value[i].Y))
                    throw new ArgumentException($"Atom {i} has non-finite coordinates.", nameof(Atoms));
            }

            _atoms = value;
            ValidateBonds(_bonds, value.Count);
        }
    }

    /// <summary>Bonds; index == bond index of the source molecule.</summary>
    public IReadOnlyList<DepictionBond> Bonds
    {
        get => _bonds;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            ValidateBonds(value, _atoms.Count);
            _bonds = value;
        }
    }

    /// <summary>Rings (smallest set of smallest rings), used for aromatic circles. Optional.</summary>
    public IReadOnlyList<DepictionRing> Rings
    {
        get => _rings;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            foreach (var ring in value)
            {
                foreach (var a in ring.AtomIndices)
                {
                    if ((uint)a >= (uint)_atoms.Count)
                        throw new ArgumentException($"Ring references unknown atom {a}.", nameof(Rings));
                }
            }

            _rings = value;
        }
    }

    /// <summary>Axis-aligned bounds of all atom coordinates (empty depiction: all zero).</summary>
    public DepictionBounds GetBounds()
    {
        if (_atoms.Count == 0)
            return default;
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (var a in _atoms)
        {
            minX = Math.Min(minX, a.X);
            minY = Math.Min(minY, a.Y);
            maxX = Math.Max(maxX, a.X);
            maxY = Math.Max(maxY, a.Y);
        }

        return new DepictionBounds(minX, minY, maxX, maxY);
    }

    /// <summary>Median bond length in model units, or <see langword="null"/> if there are no (non-degenerate) bonds.</summary>
    public double? GetMedianBondLength()
    {
        if (_bonds.Count == 0)
            return null;
        var lengths = new List<double>(_bonds.Count);
        foreach (var b in _bonds)
        {
            var a1 = _atoms[b.Begin];
            var a2 = _atoms[b.End];
            var len = Math.Sqrt(((a1.X - a2.X) * (a1.X - a2.X)) + ((a1.Y - a2.Y) * (a1.Y - a2.Y)));
            if (len > 1e-9)
                lengths.Add(len);
        }

        if (lengths.Count == 0)
            return null;
        lengths.Sort();
        var mid = lengths.Count / 2;
        return lengths.Count % 2 == 1 ? lengths[mid] : (lengths[mid - 1] + lengths[mid]) / 2;
    }

    private static void ValidateBonds(IReadOnlyList<DepictionBond> bonds, int atomCount)
    {
        for (var i = 0; i < bonds.Count; i++)
        {
            var b = bonds[i] ?? throw new ArgumentException($"Bond {i} is null.", nameof(Bonds));
            if (b.Index != i)
                throw new ArgumentException($"Bond at position {i} has Index {b.Index}; indices must match positions.", nameof(Bonds));
            if ((uint)b.Begin >= (uint)atomCount || (uint)b.End >= (uint)atomCount)
                throw new ArgumentException($"Bond {i} references an unknown atom ({b.Begin}-{b.End}, atom count {atomCount}).", nameof(Bonds));
            if (b.Begin == b.End)
                throw new ArgumentException($"Bond {i} connects atom {b.Begin} with itself.", nameof(Bonds));
        }
    }
}

/// <summary>Axis-aligned bounds in model coordinates.</summary>
/// <param name="MinX">Minimum x.</param>
/// <param name="MinY">Minimum y.</param>
/// <param name="MaxX">Maximum x.</param>
/// <param name="MaxY">Maximum y.</param>
public readonly record struct DepictionBounds(double MinX, double MinY, double MaxX, double MaxY)
{
    /// <summary>Width (<c>MaxX - MinX</c>).</summary>
    public double Width => MaxX - MinX;

    /// <summary>Height (<c>MaxY - MinY</c>).</summary>
    public double Height => MaxY - MinY;

    /// <summary>Centre x.</summary>
    public double CenterX => (MinX + MaxX) / 2;

    /// <summary>Centre y.</summary>
    public double CenterY => (MinY + MaxY) / 2;
}
