using MolDraw.Depiction;
using NCDK;

namespace MolDraw.Core.Ncdk;

/// <summary>Parses molecules from SMILES and molecule files.</summary>
/// <remarks>Implementations are stateless and thread-safe.</remarks>
public interface IMoleculeLoader
{
    /// <summary>Parses a SMILES string. Atom order follows the order of (heavy) atoms in the SMILES.</summary>
    /// <param name="smiles">SMILES string. An empty string yields an empty molecule.</param>
    /// <exception cref="ArgumentNullException"><paramref name="smiles"/> is <see langword="null"/>.</exception>
    /// <exception cref="MoleculeParseException">The SMILES is invalid; contains position and details.</exception>
    IAtomContainer ParseSmiles(string smiles);

    /// <summary>Parses a SMILES string without throwing.</summary>
    /// <param name="smiles">SMILES string.</param>
    /// <param name="molecule">The parsed molecule, or <see langword="null"/> on failure.</param>
    /// <param name="error">Error message on failure, otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> on success.</returns>
    bool TryParseSmiles(string? smiles, out IAtomContainer? molecule, out string? error);

    /// <summary>Loads the first molecule from a stream.</summary>
    /// <param name="stream">Readable stream (not disposed).</param>
    /// <param name="format">Input format.</param>
    /// <exception cref="MoleculeParseException">The content cannot be parsed or contains no molecule.</exception>
    IAtomContainer Load(Stream stream, MoleculeFormat format);

    /// <summary>Loads all molecules from a stream (SDF records, SMILES lines, or a single molfile).</summary>
    /// <param name="stream">Readable stream (not disposed).</param>
    /// <param name="format">Input format.</param>
    /// <exception cref="MoleculeParseException">A record cannot be parsed.</exception>
    IReadOnlyList<IAtomContainer> LoadAll(Stream stream, MoleculeFormat format);
}
