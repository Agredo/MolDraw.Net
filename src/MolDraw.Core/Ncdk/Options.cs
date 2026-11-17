namespace MolDraw.Core.Ncdk;

/// <summary>Input formats supported by <see cref="IMoleculeLoader.Load(System.IO.Stream, MoleculeFormat)"/>.</summary>
public enum MoleculeFormat
{
    /// <summary>MDL molfile (V2000 or V3000, detected automatically).</summary>
    MolFile,

    /// <summary>SD file; <see cref="IMoleculeLoader.Load"/> returns the first record.</summary>
    Sdf,

    /// <summary>SMILES; the first non-empty line is parsed (an optional title after whitespace is ignored).</summary>
    Smiles,
}

/// <summary>Options for <see cref="MoleculeLoader"/>.</summary>
public sealed record MoleculeLoaderOptions
{
    /// <summary>Default options (RDKit-compatible hydrogen handling, lenient parsing).</summary>
    public static MoleculeLoaderOptions Default { get; } = new();

    /// <summary>
    /// When <see langword="false"/> (default), simple explicit hydrogens (e.g. <c>[H]</c> in SMILES or H atoms in molfiles)
    /// are converted into implicit hydrogen counts, like RDKit's <c>MolFromSmiles</c>/<c>RemoveHs</c>. Hydrogens that carry
    /// information (isotopes such as <c>[2H]</c>, charges, bridging/H<sub>2</sub>) are kept. Atom order of the remaining atoms
    /// is preserved, so atom indices match RDKit's heavy-atom indices.
    /// </summary>
    public bool KeepExplicitHydrogens { get; init; }

    /// <summary>Strict SMILES parsing (reject non-standard syntax accepted leniently by NCDK). Default <see langword="false"/>.</summary>
    public bool StrictSmiles { get; init; }
}
