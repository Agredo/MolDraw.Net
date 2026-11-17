using MolDraw.Depiction;
using SkiaSharp;

namespace MolDraw.Skia.Tests;

public class HighlightRenderingTests
{
    private static readonly MoleculeRenderOptions Opaque = MoleculeRenderOptions.Light with { HighlightOpacity = 1f };

    /// <summary>A point inside the disc of atom <paramref name="i"/> but away from the bond line (chain along x).</summary>
    private static (int X, int Y) InsideDisc(RenderedMolecule r, int i)
    {
        var p = r.AtomPositions[i];
        return ((int)p.X, (int)(p.Y + (r.AtomRadius * 0.6f)));
    }

    [Fact]
    public void ExplicitAtomColourIsDrawnUnderBonds()
    {
        var d = Molecules.Chain(3);
        var highlights = new MoleculeHighlights { AtomColors = new Dictionary<int, SKColor> { [1] = SKColors.Red } };
        var r = new MoleculeRenderer().Layout(new SKRect(0, 0, 300, 150), d, Opaque, highlights);

        using var bmp = Molecules.Render(d, 300, 150, Opaque, highlights, SKColors.White);

        var (x, y) = InsideDisc(r, 1);
        Assert.True(Molecules.Near(SKColors.Red, bmp.GetPixel(x, y)), $"expected red, got {bmp.GetPixel(x, y)}");
        var (x0, y0) = InsideDisc(r, 0);
        Assert.True(Molecules.Near(SKColors.White, bmp.GetPixel(x0, y0)));

        // bond is drawn on top of the disc
        var bondPixel = bmp.GetPixel((int)r.AtomPositions[1].X + 3, (int)r.AtomPositions[1].Y);
        Assert.True(bondPixel.Red < 120, $"bond should be dark on top of highlight, got {bondPixel}");
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(0.5f)]
    [InlineData(1f)]
    public void WeightsAreMappedThroughColormap(float weight)
    {
        var d = Molecules.Chain(3);
        var highlights = new MoleculeHighlights { AtomWeights = [weight, float.NaN, weight], Colormap = Colormap.Viridis };
        var r = new MoleculeRenderer().Layout(new SKRect(0, 0, 300, 150), d, Opaque, highlights);

        using var bmp = Molecules.Render(d, 300, 150, Opaque, highlights, SKColors.White);

        var (x, y) = InsideDisc(r, 0);
        Assert.True(Molecules.Near(Colormap.Viridis.Map(weight), bmp.GetPixel(x, y)), $"got {bmp.GetPixel(x, y)}");
        var (x1, y1) = InsideDisc(r, 1);
        Assert.True(Molecules.Near(SKColors.White, bmp.GetPixel(x1, y1)), "NaN weight must not be highlighted");
    }

    [Fact]
    public void ThresholdSuppressesLowWeights()
    {
        var d = Molecules.Chain(2);
        var options = Opaque with { HighlightThreshold = 0.3f };
        var highlights = new MoleculeHighlights { AtomWeights = [0.1f, 0.9f] };
        var r = new MoleculeRenderer().Layout(new SKRect(0, 0, 200, 120), d, options, highlights);

        using var bmp = Molecules.Render(d, 200, 120, options, highlights, SKColors.White);

        var (x0, y0) = InsideDisc(r, 0);
        var (x1, y1) = InsideDisc(r, 1);
        Assert.True(Molecules.Near(SKColors.White, bmp.GetPixel(x0, y0)));
        Assert.True(Molecules.Near(Colormap.Viridis.Map(0.9f), bmp.GetPixel(x1, y1)));
    }

    [Fact]
    public void ExplicitColoursWinOverWeights()
    {
        var d = Molecules.Chain(2);
        var highlights = new MoleculeHighlights { AtomWeights = [1f, 1f], AtomColors = new Dictionary<int, SKColor> { [0] = SKColors.Blue } };
        var r = new MoleculeRenderer().Layout(new SKRect(0, 0, 200, 120), d, Opaque, highlights);

        using var bmp = Molecules.Render(d, 200, 120, Opaque, highlights, SKColors.White);

        var (x, y) = InsideDisc(r, 0);
        Assert.True(Molecules.Near(SKColors.Blue, bmp.GetPixel(x, y)));
    }

    [Fact]
    public void OpacityBlendsWithBackground()
    {
        var d = Molecules.Chain(2);
        var options = MoleculeRenderOptions.Light with { HighlightOpacity = 0.5f };
        var highlights = new MoleculeHighlights { AtomColors = new Dictionary<int, SKColor> { [0] = SKColors.Red } };
        var r = new MoleculeRenderer().Layout(new SKRect(0, 0, 200, 120), d, options, highlights);

        using var bmp = Molecules.Render(d, 200, 120, options, highlights, SKColors.White);

        var (x, y) = InsideDisc(r, 0);
        Assert.True(Molecules.Near(new SKColor(255, 128, 128), bmp.GetPixel(x, y), 6), $"got {bmp.GetPixel(x, y)}");
    }

    [Fact]
    public void LabelledAtomHighlightRemainsVisibleAroundLabel()
    {
        var d = Molecules.Depict("CCO");
        var highlights = new MoleculeHighlights { AtomColors = new Dictionary<int, SKColor> { [2] = SKColors.Lime } };
        var r = new MoleculeRenderer().Layout(new SKRect(0, 0, 300, 200), d, Opaque, highlights);

        using var bmp = Molecules.Render(d, 300, 200, Opaque, highlights, SKColors.White);

        // sample a ring of points inside the disc - most must be highlight-coloured (no opaque label background)
        var p = r.AtomPositions[2];
        var hits = 0;
        var samples = 0;
        for (var angle = 0; angle < 360; angle += 15)
        {
            for (var f = 0.2f; f <= 0.9f; f += 0.1f)
            {
                var x = (int)(p.X + (MathF.Cos(angle * MathF.PI / 180) * r.AtomRadius * f));
                var y = (int)(p.Y + (MathF.Sin(angle * MathF.PI / 180) * r.AtomRadius * f));
                samples++;
                if (Molecules.Near(SKColors.Lime, bmp.GetPixel(x, y), 30))
                    hits++;
            }
        }

        Assert.True(hits > samples * 0.6, $"{hits}/{samples} highlight pixels");
        Assert.True(hits < samples, "label glyphs should be drawn on top");
    }

    [Fact]
    public void BondHighlightIsDrawn()
    {
        var d = Molecules.Chain(2);
        var highlights = new MoleculeHighlights { BondColors = new Dictionary<int, SKColor> { [0] = SKColors.Orange } };
        var r = new MoleculeRenderer().Layout(new SKRect(0, 0, 200, 120), d, Opaque, highlights);

        using var bmp = Molecules.Render(d, 200, 120, Opaque, highlights, SKColors.White);

        var mid = new SKPoint((r.AtomPositions[0].X + r.AtomPositions[1].X) / 2, r.AtomPositions[0].Y + (r.BondLength * 0.12f));
        Assert.True(Molecules.Near(SKColors.Orange, bmp.GetPixel((int)mid.X, (int)mid.Y)), $"got {bmp.GetPixel((int)mid.X, (int)mid.Y)}");
    }

    [Fact]
    public void SelectedAndHoveredAtomsGetRings()
    {
        var d = Molecules.Chain(3);
        var options = MoleculeRenderOptions.Light with { SelectionColor = SKColors.Magenta, HoverColor = SKColors.Cyan, SelectionRingWidth = 4 };
        var highlights = new MoleculeHighlights { SelectedAtom = 0, HoveredAtom = 2 };
        var r = new MoleculeRenderer().Layout(new SKRect(0, 0, 300, 150), d, options, highlights);

        using var bmp = Molecules.Render(d, 300, 150, options, highlights, SKColors.White);

        bool RingHas(int atom, SKColor color)
        {
            var p = r.AtomPositions[atom];
            for (var dy = (int)(r.AtomRadius - 2); dy <= r.AtomRadius + 8; dy++)
            {
                if (Molecules.Near(color, bmp.GetPixel((int)p.X, (int)(p.Y - dy)), 40))
                    return true;
            }

            return false;
        }

        Assert.True(RingHas(0, SKColors.Magenta));
        Assert.True(RingHas(2, SKColors.Cyan));
        Assert.False(RingHas(1, SKColors.Magenta));
    }

    [Fact]
    public void ColorBarShowsColormap()
    {
        var d = Molecules.Chain(3);
        var options = Opaque with { ShowColorBar = true, ColorBarThickness = 12 };
        var highlights = new MoleculeHighlights { AtomWeights = [0f, 0.5f, 1f], Colormap = Colormap.Plasma };

        using var bmp = Molecules.Render(d, 400, 200, options, highlights, SKColors.White);

        var found = false;
        for (var x = 300; x < 400 && !found; x++)
        {
            found = Molecules.Near(Colormap.Plasma.Map(1f), bmp.GetPixel(x, 40), 25)
                || Molecules.Near(Colormap.Plasma.Map(0.5f), bmp.GetPixel(x, 100), 25);
        }

        Assert.True(found, "colour bar not found at the right edge");
    }

    [Fact]
    public void RecolouringReusesDepiction()
    {
        var d = Molecules.Depict("c1ccccc1O");
        var exporter = new MoleculeImageExporter();

        using var a = exporter.RenderBitmap(d, 200, 200, Opaque, new MoleculeHighlights { AtomWeights = Enumerable.Repeat(0f, 7).ToArray() });
        using var b = exporter.RenderBitmap(d, 200, 200, Opaque, new MoleculeHighlights { AtomWeights = Enumerable.Repeat(1f, 7).ToArray() });

        Assert.NotEqual(a.Bytes, b.Bytes);
    }
}
