using MolDraw.Depiction;

namespace MolDraw.Core.Ncdk;

/// <summary>
/// NCDK based <see cref="IChemistryEngine"/>: parses the SMILES with <see cref="MoleculeLoader"/> and builds the depiction
/// with <see cref="DepictionBuilder"/> (2D layout, aromaticity, rings, wedges, hydrogen placement).
/// </summary>
/// <remarks>Stateless and thread-safe; both collaborators are thread-safe as well.</remarks>
public sealed class NcdkDepictionEngine : IChemistryEngine
{
    private readonly IMoleculeLoader _loader;
    private readonly IDepictionBuilder _builder;

    /// <summary>Creates the engine with default loader and builder.</summary>
    public NcdkDepictionEngine()
        : this(null, null)
    {
    }

    /// <summary>Creates the engine with explicit collaborators.</summary>
    /// <param name="loader">SMILES/molfile loader; <see langword="null"/> uses a default <see cref="MoleculeLoader"/>.</param>
    /// <param name="builder">Depiction builder; <see langword="null"/> uses a default <see cref="DepictionBuilder"/>.</param>
    public NcdkDepictionEngine(IMoleculeLoader? loader, IDepictionBuilder? builder)
    {
        _loader = loader ?? new MoleculeLoader();
        _builder = builder ?? new DepictionBuilder();
    }

    /// <inheritdoc />
    public string Name => "NCDK";

    /// <inheritdoc />
    public MoleculeDepiction Depict(string smiles, DepictionBuildOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(smiles);
        return _builder.Build(_loader.ParseSmiles(smiles), options);
    }
}
