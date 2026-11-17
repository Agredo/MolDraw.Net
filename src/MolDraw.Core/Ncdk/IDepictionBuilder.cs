using MolDraw.Depiction;
using NCDK;

namespace MolDraw.Core.Ncdk;

/// <summary>Converts NCDK molecules into a toolkit-neutral <see cref="MoleculeDepiction"/>.</summary>
/// <remarks>Implementations are stateless and thread-safe; the input molecule is never modified.</remarks>
public interface IDepictionBuilder
{
    /// <summary>
    /// Builds a depiction. Atom <c>i</c> of the result corresponds to <c>molecule.Atoms[i]</c>, bond <c>j</c> to <c>molecule.Bonds[j]</c>.
    /// </summary>
    /// <param name="molecule">Molecule (not modified; a copy is laid out if coordinates are missing).</param>
    /// <param name="options">Options; <see langword="null"/> uses <see cref="DepictionBuildOptions.Default"/>.</param>
    /// <exception cref="DepictionException">Layout or perception failed.</exception>
    MoleculeDepiction Build(IAtomContainer molecule, DepictionBuildOptions? options = null);
}
