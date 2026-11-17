using SkiaSharp;

namespace MolDraw.Skia;

/// <summary>How aromatic bonds are drawn.</summary>
public enum AromaticStyle
{
    /// <summary>Alternating single/double bonds (Kekulé form).</summary>
    Kekule,

    /// <summary>Single bonds plus an inner circle per aromatic ring.</summary>
    Circle,
}

/// <summary>Position of the colour bar legend.</summary>
public enum ColorBarPosition
{
    /// <summary>Vertical bar at the right edge.</summary>
    Right,

    /// <summary>Horizontal bar at the bottom edge.</summary>
    Bottom,
}

/// <summary>
/// Immutable rendering options. Use <see cref="Light"/> or <see cref="Dark"/> and customise with <c>with</c> expressions.
/// Sizes are in canvas pixels at the nominal bond length <see cref="MaxBondLength"/>; when a molecule has to be drawn
/// smaller to fit, font size and line widths shrink proportionally (but not below <see cref="MinFontSize"/>).
/// </summary>
public sealed record MoleculeRenderOptions
{
    /// <summary>Preset for light backgrounds (transparent background, black bonds, CPK label colours).</summary>
    public static MoleculeRenderOptions Light { get; } = new();

    /// <summary>Preset for dark backgrounds (transparent background, light bonds, brightened CPK label colours).</summary>
    public static MoleculeRenderOptions Dark { get; } = new()
    {
        ForegroundColor = new SKColor(0xE6, 0xE6, 0xE6),
        AtomColors = AtomColorScheme.CpkDark,
        SelectionColor = new SKColor(0xFF, 0xB3, 0x00),
        HoverColor = new SKColor(0x4F, 0xC3, 0xF7),
        AtomIndexColor = new SKColor(0x80, 0xCB, 0xFF),
        ColorBarTextColor = new SKColor(0xE6, 0xE6, 0xE6),
    };

    /// <summary>Background fill; <see cref="SKColors.Transparent"/> (default) leaves the canvas untouched.</summary>
    public SKColor BackgroundColor { get; init; } = SKColors.Transparent;

    /// <summary>Colour of bonds and carbon/unknown labels.</summary>
    public SKColor ForegroundColor { get; init; } = new(0x20, 0x20, 0x20);

    /// <summary>Element colours for heteroatom labels.</summary>
    public AtomColorScheme AtomColors { get; init; } = AtomColorScheme.Cpk;

    /// <summary>Colour bonds half-and-half by the colours of their atoms (RDKit style). Default <see langword="false"/>.</summary>
    public bool ColorBondsByAtom { get; init; }

    /// <summary>Bond line width in pixels (at nominal size).</summary>
    public float BondWidth { get; init; } = 1.6f;

    /// <summary>Maximum (= nominal) bond length in pixels; small molecules are not blown up beyond this.</summary>
    public float MaxBondLength { get; init; } = 40f;

    /// <summary>Padding between the bounds and the drawing in pixels.</summary>
    public float Padding { get; init; } = 10f;

    /// <summary>Label font size in pixels (at nominal size).</summary>
    public float FontSize { get; init; } = 14f;

    /// <summary>Lower limit for the font size when molecules are scaled down.</summary>
    public float MinFontSize { get; init; } = 5f;

    /// <summary>Font family for labels; <see langword="null"/> uses the platform default typeface.</summary>
    public string? FontFamily { get; init; }

    /// <summary>Explicit typeface for labels (overrides <see cref="FontFamily"/>). Not disposed by the renderer.</summary>
    public SKTypeface? Typeface { get; init; }

    /// <summary>Distance between the lines of double/triple bonds as fraction of the bond length.</summary>
    public float MultipleBondSpacing { get; init; } = 0.18f;

    /// <summary>Width of the wide end of wedge/hash bonds as fraction of the bond length.</summary>
    public float WedgeWidth { get; init; } = 0.22f;

    /// <summary>How aromatic bonds are drawn.</summary>
    public AromaticStyle AromaticStyle { get; init; } = AromaticStyle.Kekule;

    /// <summary>Draw the atom index next to each atom (useful for mapping per-atom data).</summary>
    public bool ShowAtomIndices { get; init; }

    /// <summary>Colour of atom indices.</summary>
    public SKColor AtomIndexColor { get; init; } = new(0x1E, 0x6F, 0xD9);

    /// <summary>Radius of highlight discs as fraction of the bond length (before <see cref="MoleculeHighlights.RadiusScale"/>).</summary>
    public float HighlightRadius { get; init; } = 0.42f;

    /// <summary>Opacity (0..1) of highlight discs.</summary>
    public float HighlightOpacity { get; init; } = 0.85f;

    /// <summary>Scale disc radius with the atom weight (weight 0 → 35 %, weight 1 → 100 %).</summary>
    public bool WeightScalesRadius { get; init; }

    /// <summary>Scale disc opacity with the atom weight.</summary>
    public bool WeightScalesOpacity { get; init; }

    /// <summary>Atoms with a weight below this threshold get no disc. Default 0 (all weighted atoms are drawn).</summary>
    public float HighlightThreshold { get; init; }

    /// <summary>Draw discs with a soft radial fall-off instead of a solid fill.</summary>
    public bool SoftHighlights { get; init; }

    /// <summary>Outline colour of the selected atom.</summary>
    public SKColor SelectionColor { get; init; } = new(0xE6, 0x51, 0x00);

    /// <summary>Outline colour of the hovered atom.</summary>
    public SKColor HoverColor { get; init; } = new(0x02, 0x88, 0xD1);

    /// <summary>Width of selection/hover rings in pixels (at nominal size).</summary>
    public float SelectionRingWidth { get; init; } = 2.5f;

    /// <summary>Draw a colour bar legend when atom weights are highlighted.</summary>
    public bool ShowColorBar { get; init; }

    /// <summary>Position of the colour bar.</summary>
    public ColorBarPosition ColorBarPosition { get; init; } = ColorBarPosition.Right;

    /// <summary>Thickness of the colour bar in pixels.</summary>
    public float ColorBarThickness { get; init; } = 10f;

    /// <summary>Label at the low end of the colour bar.</summary>
    public string ColorBarMinLabel { get; init; } = "0";

    /// <summary>Label at the high end of the colour bar.</summary>
    public string ColorBarMaxLabel { get; init; } = "1";

    /// <summary>Colour of colour bar labels and frame.</summary>
    public SKColor ColorBarTextColor { get; init; } = new(0x20, 0x20, 0x20);
}
