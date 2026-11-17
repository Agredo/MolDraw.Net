namespace MolDraw.Depiction;

/// <summary>A bond of a <see cref="MoleculeDepiction"/>.</summary>
/// <param name="Begin">Index of the first atom (narrow end for wedge/hash bonds).</param>
/// <param name="End">Index of the second atom.</param>
/// <param name="Order">Depicted bond order. Aromatic bonds carry <see cref="BondOrder.Aromatic"/>; their Kekulé order is in <see cref="KekuleOrder"/>.</param>
/// <param name="Stereo">Wedge/hash/wavy display.</param>
/// <param name="Placement">Placement of the second line of a double bond.</param>
public sealed record DepictionBond(
    int Begin,
    int End,
    BondOrder Order,
    BondStereoDisplay Stereo,
    DoubleBondPlacement Placement)
{
    /// <summary>Bond index; equals the position in <see cref="MoleculeDepiction.Bonds"/>.</summary>
    public int Index { get; init; }

    /// <summary>
    /// Order used when aromatic bonds are drawn in Kekulé form (<see cref="BondOrder.Single"/> or <see cref="BondOrder.Double"/>).
    /// Defaults to <see cref="Order"/>; for aromatic bonds without a known Kekulé form it remains <see cref="BondOrder.Aromatic"/>.
    /// </summary>
    public BondOrder KekuleOrder { get; init; } = Order;

    /// <summary>Whether the bond is part of a ring.</summary>
    public bool IsInRing { get; init; }

    /// <summary>Returns the atom index at the other end of the bond.</summary>
    /// <param name="atomIndex">One of <see cref="Begin"/> or <see cref="End"/>.</param>
    public int Other(int atomIndex) => atomIndex == Begin ? End : atomIndex == End ? Begin
        : throw new ArgumentException($"Atom {atomIndex} is not part of bond {Begin}-{End}.", nameof(atomIndex));

    /// <summary>Whether the bond connects the given atom.</summary>
    public bool Contains(int atomIndex) => atomIndex == Begin || atomIndex == End;
}
