namespace MolDraw.Depiction;

/// <summary>
/// A chemistry backend ("engine"): turns a structure string into a <see cref="MoleculeDepiction"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is the only place where the toolkit is chosen. <c>MolDraw.Core</c> implements it with NCDK; a different
/// toolkit (Indigo, RDKit, …) would ship its own implementation. The renderer, the exporter and
/// <c>MoleculeView.Depiction</c> never need an engine, because they consume <see cref="MoleculeDepiction"/> only.
/// </para>
/// <para>Implementations must be stateless and thread-safe; callers may invoke them on a background thread.</para>
/// </remarks>
public interface IChemistryEngine
{
    /// <summary>Short engine name for diagnostics and error messages (e.g. <c>NCDK</c>).</summary>
    string Name { get; }

    /// <summary>Parses a SMILES string and builds its 2D depiction.</summary>
    /// <param name="smiles">SMILES string. An empty string yields <see cref="MoleculeDepiction.Empty"/>.</param>
    /// <param name="options">Build options; <see langword="null"/> uses <see cref="DepictionBuildOptions.Default"/>.</param>
    /// <returns>The depiction; atom order follows the SMILES.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="smiles"/> is <see langword="null"/>.</exception>
    /// <exception cref="MoleculeParseException">The SMILES cannot be parsed.</exception>
    /// <exception cref="DepictionException">The depiction cannot be created (e.g. 2D layout failure).</exception>
    MoleculeDepiction Depict(string smiles, DepictionBuildOptions? options = null);
}
