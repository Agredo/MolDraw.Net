namespace MolDraw.Depiction;

/// <summary>Options for building a <see cref="MoleculeDepiction"/>; used by <see cref="IChemistryEngine"/>.</summary>
public sealed record DepictionBuildOptions
{
    /// <summary>Default options.</summary>
    public static DepictionBuildOptions Default { get; } = new();

    /// <summary>Always compute new 2D coordinates, even if the molecule already has them. Default <see langword="false"/>.</summary>
    public bool ForceLayout { get; init; }

    /// <summary>
    /// Perceive aromaticity when the molecule has no aromaticity flags yet (e.g. Kekulé SMILES or molfiles).
    /// Default <see langword="true"/>.
    /// </summary>
    public bool PerceiveAromaticity { get; init; } = true;

    /// <summary>
    /// If the molecule already has 2D coordinates and tetrahedral stereo but no wedge/hash bonds, assign wedges.
    /// (When coordinates are generated, wedges come from the layout.) Default <see langword="true"/>.
    /// </summary>
    public bool AssignMissingWedges { get; init; } = true;

    /// <summary>Which carbon atoms get a visible label. Default <see cref="CarbonLabelMode.Default"/>.</summary>
    public CarbonLabelMode CarbonLabels { get; init; } = CarbonLabelMode.Default;
}
