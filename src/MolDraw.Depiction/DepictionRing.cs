namespace MolDraw.Depiction;

/// <summary>A ring of a <see cref="MoleculeDepiction"/> (smallest set of smallest rings).</summary>
/// <param name="AtomIndices">Atom indices in ring order.</param>
/// <param name="IsAromatic">Whether all ring bonds are aromatic (renderers may draw an inner circle).</param>
public sealed record DepictionRing(IReadOnlyList<int> AtomIndices, bool IsAromatic);
