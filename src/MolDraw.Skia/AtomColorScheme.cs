using SkiaSharp;

namespace MolDraw.Skia;

/// <summary>Element colours for atom labels (by atomic number). Immutable.</summary>
public sealed record AtomColorScheme
{
    /// <summary>CPK/Jmol-like colours, darkened for readability on light backgrounds.</summary>
    public static AtomColorScheme Cpk { get; } = new()
    {
        ElementColors = new Dictionary<int, SKColor>
        {
            [1] = new SKColor(0x60, 0x60, 0x60),   // H
            [5] = new SKColor(0xD3, 0x7A, 0x5A),   // B
            [7] = new SKColor(0x30, 0x50, 0xF8),   // N
            [8] = new SKColor(0xE0, 0x10, 0x10),   // O
            [9] = new SKColor(0x3A, 0x9A, 0x2A),   // F
            [11] = new SKColor(0x7A, 0x3D, 0xC8),  // Na
            [12] = new SKColor(0x3A, 0x9A, 0x00),  // Mg
            [14] = new SKColor(0xB0, 0x8A, 0x6A),  // Si
            [15] = new SKColor(0xE0, 0x70, 0x00),  // P
            [16] = new SKColor(0xB8, 0x9B, 0x00),  // S
            [17] = new SKColor(0x1F, 0x9A, 0x1F),  // Cl
            [19] = new SKColor(0x7A, 0x3D, 0xC8),  // K
            [20] = new SKColor(0x2A, 0x9A, 0x00),  // Ca
            [26] = new SKColor(0xC0, 0x50, 0x30),  // Fe
            [29] = new SKColor(0xB0, 0x60, 0x30),  // Cu
            [30] = new SKColor(0x6A, 0x6A, 0x9A),  // Zn
            [34] = new SKColor(0xD0, 0x80, 0x00),  // Se
            [35] = new SKColor(0xA6, 0x29, 0x29),  // Br
            [53] = new SKColor(0x94, 0x00, 0x94),  // I
        },
    };

    /// <summary>CPK-like colours brightened for dark backgrounds.</summary>
    public static AtomColorScheme CpkDark { get; } = new()
    {
        ElementColors = new Dictionary<int, SKColor>
        {
            [1] = new SKColor(0xC8, 0xC8, 0xC8),
            [5] = new SKColor(0xFF, 0xB5, 0xB5),
            [7] = new SKColor(0x8F, 0xA8, 0xFF),
            [8] = new SKColor(0xFF, 0x6B, 0x6B),
            [9] = new SKColor(0x90, 0xE0, 0x50),
            [11] = new SKColor(0xC0, 0x90, 0xFF),
            [12] = new SKColor(0x8A, 0xFF, 0x00),
            [14] = new SKColor(0xF0, 0xC8, 0xA0),
            [15] = new SKColor(0xFF, 0xA0, 0x40),
            [16] = new SKColor(0xFF, 0xE0, 0x50),
            [17] = new SKColor(0x6F, 0xE0, 0x6F),
            [19] = new SKColor(0xC0, 0x90, 0xFF),
            [20] = new SKColor(0x7A, 0xFF, 0x50),
            [26] = new SKColor(0xFF, 0x9A, 0x70),
            [29] = new SKColor(0xFF, 0xA0, 0x70),
            [30] = new SKColor(0xB0, 0xB0, 0xE0),
            [34] = new SKColor(0xFF, 0xC0, 0x40),
            [35] = new SKColor(0xE0, 0x70, 0x70),
            [53] = new SKColor(0xE0, 0x60, 0xE0),
        },
    };

    /// <summary>Element colours by atomic number. Elements not contained use the foreground colour.</summary>
    public IReadOnlyDictionary<int, SKColor> ElementColors { get; init; } = new Dictionary<int, SKColor>();

    /// <summary>A scheme without element colours (all labels in the foreground colour).</summary>
    public static AtomColorScheme Monochrome { get; } = new();

    /// <summary>Returns the colour for an atomic number, or <paramref name="fallback"/>.</summary>
    /// <param name="atomicNumber">Atomic number.</param>
    /// <param name="fallback">Colour used for unknown elements and carbon.</param>
    public SKColor GetColor(int atomicNumber, SKColor fallback)
        => ElementColors.TryGetValue(atomicNumber, out var c) ? c : fallback;
}
