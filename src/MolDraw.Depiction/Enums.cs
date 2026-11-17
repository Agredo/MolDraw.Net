namespace MolDraw.Depiction;

/// <summary>Bond order as it should be depicted.</summary>
public enum BondOrder
{
    /// <summary>Single bond.</summary>
    Single = 1,

    /// <summary>Double bond.</summary>
    Double = 2,

    /// <summary>Triple bond.</summary>
    Triple = 3,

    /// <summary>Aromatic bond (drawn either in Kekulé form or as part of an aromatic ring circle).</summary>
    Aromatic = 4,
}

/// <summary>Stereo display of a single bond. The narrow end of a wedge/hash is always at <see cref="DepictionBond.Begin"/>.</summary>
public enum BondStereoDisplay
{
    /// <summary>Plain bond.</summary>
    None = 0,

    /// <summary>Solid wedge (bond points towards the viewer).</summary>
    Wedge,

    /// <summary>Hashed wedge (bond points away from the viewer).</summary>
    Hash,

    /// <summary>Wavy bond (unknown/either configuration).</summary>
    Wavy,
}

/// <summary>
/// Placement of the second line of a double bond, relative to the bond direction
/// <see cref="DepictionBond.Begin"/> → <see cref="DepictionBond.End"/> in model coordinates (y axis pointing up).
/// </summary>
public enum DoubleBondPlacement
{
    /// <summary>Both lines are offset symmetrically around the atom-atom axis.</summary>
    Center = 0,

    /// <summary>The second line is drawn on the left-hand side (e.g. towards the ring centre).</summary>
    Left,

    /// <summary>The second line is drawn on the right-hand side.</summary>
    Right,
}

/// <summary>Controls which carbon atoms get an explicit element label.</summary>
public enum CarbonLabelMode
{
    /// <summary>Skeletal formula: carbons are only labelled when charged, isotope-labelled, radical or isolated.</summary>
    Default,

    /// <summary>Additionally label terminal carbons (e.g. CH<sub>3</sub>).</summary>
    Terminal,

    /// <summary>Label every carbon atom.</summary>
    All,
}

/// <summary>Where implicit hydrogens are placed relative to the element symbol of a labelled atom.</summary>
public enum HydrogenPosition
{
    /// <summary>After the symbol, e.g. <c>OH</c>.</summary>
    Right = 0,

    /// <summary>Before the symbol, e.g. <c>HO</c>.</summary>
    Left,

    /// <summary>Above the symbol (towards larger model y).</summary>
    Above,

    /// <summary>Below the symbol (towards smaller model y).</summary>
    Below,
}
