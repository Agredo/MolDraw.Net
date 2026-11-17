using SkiaSharp;

namespace MolDraw.Skia;

/// <summary>
/// Immutable highlight description. Highlights are drawn as discs/bands <em>under</em> bonds and labels; changing
/// highlights never requires a new depiction (coordinates are not recomputed).
/// </summary>
public sealed record MoleculeHighlights
{
    private static readonly IReadOnlyDictionary<int, SKColor> EmptyColors = new Dictionary<int, SKColor>();

    /// <summary>No highlights.</summary>
    public static MoleculeHighlights None { get; } = new();

    /// <summary>Explicit atom colours by atom index (take precedence over <see cref="AtomWeights"/>).</summary>
    public IReadOnlyDictionary<int, SKColor> AtomColors { get; init; } = EmptyColors;

    /// <summary>
    /// Per-atom weights in [0, 1] by atom index, mapped through <see cref="Colormap"/>. Shorter lists only cover the
    /// first atoms; <see cref="float.NaN"/> means "no highlight".
    /// </summary>
    public IReadOnlyList<float>? AtomWeights { get; init; }

    /// <summary>Colour map for <see cref="AtomWeights"/>. Default <see cref="Skia.Colormap.Viridis"/>.</summary>
    public Colormap Colormap { get; init; } = Colormap.Viridis;

    /// <summary>Bond highlight colours by bond index (drawn as bands under the bond).</summary>
    public IReadOnlyDictionary<int, SKColor> BondColors { get; init; } = EmptyColors;

    /// <summary>Selected atom (drawn with an outline ring).</summary>
    public int? SelectedAtom { get; init; }

    /// <summary>Hovered atom (drawn with an outline ring).</summary>
    public int? HoveredAtom { get; init; }

    /// <summary>Scale factor for highlight disc radii. Default 1.</summary>
    public float RadiusScale { get; init; } = 1f;

    /// <summary>Whether anything would be drawn.</summary>
    public bool IsEmpty => AtomColors.Count == 0 && (AtomWeights is null || AtomWeights.Count == 0) && BondColors.Count == 0
        && SelectedAtom is null && HoveredAtom is null;

    /// <summary>Resolves the highlight colour of an atom, or <see langword="null"/> if it is not highlighted.</summary>
    /// <param name="atomIndex">Atom index.</param>
    /// <param name="threshold">Weights below this value are ignored.</param>
    /// <param name="weight">The weight used (1 for explicit colours).</param>
    public SKColor? GetAtomColor(int atomIndex, float threshold, out float weight)
    {
        if (AtomColors.TryGetValue(atomIndex, out var c))
        {
            weight = 1f;
            return c;
        }

        weight = float.NaN;
        if (AtomWeights is null || atomIndex < 0 || atomIndex >= AtomWeights.Count)
            return null;
        var w = AtomWeights[atomIndex];
        if (float.IsNaN(w) || w < threshold)
            return null;
        weight = Math.Clamp(w, 0f, 1f);
        return Colormap.Map(weight);
    }
}
