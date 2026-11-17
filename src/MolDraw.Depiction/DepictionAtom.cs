namespace MolDraw.Depiction;

/// <summary>An atom of a <see cref="MoleculeDepiction"/>.</summary>
/// <param name="Index">Atom index; equals the position in <see cref="MoleculeDepiction.Atoms"/> and the atom index of the source molecule.</param>
/// <param name="Symbol">Element symbol (e.g. <c>C</c>, <c>N</c>, <c>Cl</c>) or a pseudo atom label (e.g. <c>R</c>, <c>*</c>).</param>
/// <param name="X">Model x coordinate.</param>
/// <param name="Y">Model y coordinate (y axis pointing up, as in MOL files).</param>
/// <param name="ImplicitHydrogens">Number of implicit hydrogens.</param>
/// <param name="Charge">Formal charge.</param>
/// <param name="MassNumber">Isotope mass number, or <see langword="null"/> for natural abundance.</param>
/// <param name="ShowLabel">Whether the element symbol is drawn (heteroatoms, charged/isotopic carbons, ...).</param>
/// <param name="HydrogenPosition">Where implicit hydrogens are placed relative to the symbol.</param>
public sealed record DepictionAtom(
    int Index,
    string Symbol,
    double X,
    double Y,
    int ImplicitHydrogens,
    int Charge,
    int? MassNumber,
    bool ShowLabel,
    HydrogenPosition HydrogenPosition)
{
    /// <summary>Atomic number (0 for pseudo atoms / unknown).</summary>
    public int AtomicNumber { get; init; }

    /// <summary>Whether the atom is part of an aromatic system.</summary>
    public bool IsAromatic { get; init; }

    /// <summary>Number of unpaired electrons (radicals), drawn as dots if &gt; 0.</summary>
    public int Radicals { get; init; }
}
