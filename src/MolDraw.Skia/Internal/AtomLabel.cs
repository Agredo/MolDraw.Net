using SkiaSharp;

namespace MolDraw.Skia.Internal;

/// <summary>A piece of label text positioned in canvas coordinates.</summary>
internal readonly record struct TextRun(string Text, float FontSize, float X, float Baseline, SKColor Color);

/// <summary>Laid-out atom label: text runs, extra dots (radicals) and the rectangles used to clip bonds.</summary>
internal sealed class AtomLabel
{
    public List<TextRun> Runs { get; } = [];

    public List<SKPoint> Dots { get; } = [];

    public float DotRadius { get; set; }

    public List<SKRect> Rects { get; } = [];

    public SKRect Union
    {
        get
        {
            if (Rects.Count == 0)
                return SKRect.Empty;
            var r = Rects[0];
            for (var i = 1; i < Rects.Count; i++)
                r.Union(Rects[i]);
            return r;
        }
    }
}
