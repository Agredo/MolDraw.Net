using MolDraw.Depiction;
using SkiaSharp;

namespace MolDraw.Skia;

/// <summary>Renders a <see cref="MoleculeDepiction"/> onto an <see cref="SKCanvas"/>.</summary>
/// <remarks>Implementations are thread-safe; a canvas must of course only be used by one thread at a time.</remarks>
public interface IMoleculeRenderer
{
    /// <summary>Renders the depiction scaled and centred into <paramref name="bounds"/>.</summary>
    /// <param name="canvas">Target canvas (any: raster, GPU, SVG, PDF).</param>
    /// <param name="bounds">Target rectangle in canvas coordinates.</param>
    /// <param name="depiction">The depiction.</param>
    /// <param name="options">Rendering options.</param>
    /// <param name="highlights">Optional highlights.</param>
    /// <returns>Screen-space geometry for hit testing.</returns>
    RenderedMolecule Render(SKCanvas canvas, SKRect bounds, MoleculeDepiction depiction, MoleculeRenderOptions options, MoleculeHighlights? highlights = null);

    /// <summary>Computes the screen-space geometry (as <see cref="Render"/> would) without drawing.</summary>
    /// <param name="bounds">Target rectangle in canvas coordinates.</param>
    /// <param name="depiction">The depiction.</param>
    /// <param name="options">Rendering options.</param>
    /// <param name="highlights">Optional highlights (affect colour bar space and disc radius).</param>
    RenderedMolecule Layout(SKRect bounds, MoleculeDepiction depiction, MoleculeRenderOptions options, MoleculeHighlights? highlights = null);
}
