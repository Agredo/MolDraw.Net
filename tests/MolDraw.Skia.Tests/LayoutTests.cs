using MolDraw.Depiction;
using SkiaSharp;

namespace MolDraw.Skia.Tests;

public class LayoutTests
{
    private readonly MoleculeRenderer _renderer = new();

    [Fact]
    public void SmallMoleculeIsNotBlownUp()
    {
        var options = MoleculeRenderOptions.Light with { MaxBondLength = 40 };

        var r = _renderer.Layout(new SKRect(0, 0, 1000, 1000), Molecules.Chain(2), options);

        Assert.Equal(40, r.BondLength, 2);
    }

    [Fact]
    public void LargeMoleculeIsScaledDownToFit()
    {
        var options = MoleculeRenderOptions.Light with { Padding = 10 };
        var bounds = new SKRect(0, 0, 200, 120);

        var r = _renderer.Layout(bounds, Molecules.Depict(Molecules.Samples[3].Smiles), options);

        Assert.True(r.BondLength < 40);
        Assert.All(r.AtomPositions, p => Assert.True(p.X >= 10 && p.X <= 190 && p.Y >= 10 && p.Y <= 110, $"{p} outside"));
    }

    [Fact]
    public void ChainIsCenteredWithUniformScaleAndYFlip()
    {
        var bounds = new SKRect(20, 30, 420, 230);

        var r = _renderer.Layout(bounds, Molecules.Chain(5), MoleculeRenderOptions.Light with { MaxBondLength = 1000 });

        var xs = r.AtomPositions.Select(p => p.X).ToArray();
        var ys = r.AtomPositions.Select(p => p.Y).ToArray();
        Assert.Equal(bounds.MidX, (xs.Min() + xs.Max()) / 2, 1);
        Assert.Equal(bounds.MidY, (ys.Min() + ys.Max()) / 2, 1);
        Assert.Equal(r.Transform.ScaleX, -r.Transform.ScaleY, 4);
        Assert.True(r.Transform.ScaleY < 0);
        Assert.Equal(r.BondLength, xs[1] - xs[0], 2);
    }

    [Fact]
    public void AspectRatioIsPreservedInTallAndWideBounds()
    {
        var d = Molecules.Depict("c1ccccc1");
        var options = MoleculeRenderOptions.Light with { MaxBondLength = 1000 };

        var wide = _renderer.Layout(new SKRect(0, 0, 800, 200), d, options);
        var tall = _renderer.Layout(new SKRect(0, 0, 200, 800), d, options);

        foreach (var r in new[] { wide, tall })
        {
            Assert.Equal(r.Transform.ScaleX, -r.Transform.ScaleY, 4);
            var lengths = d.Bonds.Select(b => SKPoint.Distance(r.AtomPositions[b.Begin], r.AtomPositions[b.End])).ToArray();
            Assert.All(lengths, l => Assert.Equal(r.BondLength, l, 1));
        }
    }

    [Fact]
    public void ModelToCanvasRoundTrips()
    {
        var d = Molecules.Depict("CCO");
        var r = _renderer.Layout(new SKRect(0, 0, 300, 300), d, MoleculeRenderOptions.Light);

        var p = r.ModelToCanvas(d.Atoms[2].X, d.Atoms[2].Y);
        Assert.Equal(r.AtomPositions[2], p);
        var back = r.CanvasToModel(p);
        Assert.Equal(d.Atoms[2].X, back.X, 3);
        Assert.Equal(d.Atoms[2].Y, back.Y, 3);
    }

    [Fact]
    public void LayoutDoesNotDependOnHighlightColours()
    {
        var d = Molecules.Depict("CCO");
        var bounds = new SKRect(0, 0, 300, 200);

        var a = _renderer.Layout(bounds, d, MoleculeRenderOptions.Light);
        var b = _renderer.Layout(bounds, d, MoleculeRenderOptions.Light, new MoleculeHighlights { AtomWeights = [0.1f, 0.5f, 1f], SelectedAtom = 1 });

        Assert.Equal(a.AtomPositions, b.AtomPositions);
    }

    [Fact]
    public void EmptyDepictionGivesEmptyResult()
    {
        using var bmp = new SKBitmap(50, 50);
        using var canvas = new SKCanvas(bmp);

        var r = _renderer.Render(canvas, new SKRect(0, 0, 50, 50), MoleculeDepiction.Empty, MoleculeRenderOptions.Light);

        Assert.Empty(r.AtomPositions);
        Assert.Null(r.HitTestAtom(new SKPoint(25, 25)));
    }

    [Fact]
    public void UnlabelledSingleAtomIsCentred()
    {
        var d = new MoleculeDepiction([new DepictionAtom(0, "C", 3, 4, 0, 0, null, false, HydrogenPosition.Right)], []);

        var r = _renderer.Layout(new SKRect(0, 0, 100, 60), d, MoleculeRenderOptions.Light);

        Assert.Equal(50, r.AtomPositions[0].X, 1);
        Assert.Equal(30, r.AtomPositions[0].Y, 1);
    }

    [Fact]
    public void LabelledSingleAtomLabelIsCentred()
    {
        // "CH4": the label (not the carbon) is centred, so the atom sits slightly left of the centre
        var r = _renderer.Layout(new SKRect(0, 0, 100, 60), Molecules.Depict("C"), MoleculeRenderOptions.Light);

        Assert.InRange(r.AtomPositions[0].X, 35, 50);
        Assert.Equal(30, r.AtomPositions[0].Y, 0);
    }

    [Fact]
    public void ZeroSizedBoundsDoNotThrow()
    {
        using var bmp = new SKBitmap(10, 10);
        using var canvas = new SKCanvas(bmp);

        var r = _renderer.Render(canvas, SKRect.Empty, Molecules.Depict("CCO"), MoleculeRenderOptions.Light);

        Assert.Empty(r.AtomPositions);
    }
}
