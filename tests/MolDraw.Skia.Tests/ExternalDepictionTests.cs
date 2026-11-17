using MolDraw.Depiction;
using MolDraw.Skia;
using SkiaSharp;

namespace MolDraw.Skia.Tests;

/// <summary>
/// Guards the documented "bring your own coordinates" path (README: <em>Using another chemistry engine</em>):
/// a depiction assembled from foreign coordinates must render without any NCDK involvement.
/// </summary>
public class ExternalDepictionTests
{
    private static MoleculeDepiction Phenol()
    {
        var atoms = new[]
        {
            new DepictionAtom(0, "C", 1.500, 0.000, 0, 0, null, false, HydrogenPosition.Right) { AtomicNumber = 6, IsAromatic = true },
            new DepictionAtom(1, "C", 0.750, 1.299, 1, 0, null, false, HydrogenPosition.Right) { AtomicNumber = 6, IsAromatic = true },
            new DepictionAtom(2, "C", -0.750, 1.299, 1, 0, null, false, HydrogenPosition.Right) { AtomicNumber = 6, IsAromatic = true },
            new DepictionAtom(3, "C", -1.500, 0.000, 1, 0, null, false, HydrogenPosition.Right) { AtomicNumber = 6, IsAromatic = true },
            new DepictionAtom(4, "C", -0.750, -1.299, 1, 0, null, false, HydrogenPosition.Right) { AtomicNumber = 6, IsAromatic = true },
            new DepictionAtom(5, "C", 0.750, -1.299, 1, 0, null, false, HydrogenPosition.Right) { AtomicNumber = 6, IsAromatic = true },
            new DepictionAtom(6, "O", 3.000, 0.000, 1, 0, null, true, HydrogenPosition.Right) { AtomicNumber = 8 },
        };

        var bonds = new[]
        {
            new DepictionBond(0, 1, BondOrder.Aromatic, BondStereoDisplay.None, DoubleBondPlacement.Center) { Index = 0, KekuleOrder = BondOrder.Single, IsInRing = true },
            new DepictionBond(1, 2, BondOrder.Aromatic, BondStereoDisplay.None, DoubleBondPlacement.Center) { Index = 1, KekuleOrder = BondOrder.Double, IsInRing = true },
            new DepictionBond(2, 3, BondOrder.Aromatic, BondStereoDisplay.None, DoubleBondPlacement.Center) { Index = 2, KekuleOrder = BondOrder.Single, IsInRing = true },
            new DepictionBond(3, 4, BondOrder.Aromatic, BondStereoDisplay.None, DoubleBondPlacement.Center) { Index = 3, KekuleOrder = BondOrder.Double, IsInRing = true },
            new DepictionBond(4, 5, BondOrder.Aromatic, BondStereoDisplay.None, DoubleBondPlacement.Center) { Index = 4, KekuleOrder = BondOrder.Single, IsInRing = true },
            new DepictionBond(5, 0, BondOrder.Aromatic, BondStereoDisplay.None, DoubleBondPlacement.Center) { Index = 5, KekuleOrder = BondOrder.Double, IsInRing = true },
            new DepictionBond(0, 6, BondOrder.Single, BondStereoDisplay.None, DoubleBondPlacement.Center) { Index = 6, KekuleOrder = BondOrder.Single },
        };

        return new MoleculeDepiction(atoms, bonds) { Rings = [new DepictionRing([0, 1, 2, 3, 4, 5], IsAromatic: true)] };
    }

    [Fact]
    public void DepictionFromForeignCoordinates_RendersAndHitTests()
    {
        var depiction = Phenol();

        Assert.Equal(7, depiction.Atoms.Count);
        Assert.Equal(1.5, depiction.GetMedianBondLength()!.Value, 3);

        var renderer = new MoleculeRenderer();
        var layout = renderer.Layout(new SKRect(0, 0, 400, 300), depiction, MoleculeRenderOptions.Light);
        Assert.Equal(7, layout.AtomPositions.Count);

        // the oxygen is the rightmost atom, so its screen position lies right of the ring centre
        Assert.True(layout.AtomPositions[6].X > layout.AtomPositions[3].X);
        Assert.Equal(6, layout.HitTestAtom(layout.AtomPositions[6], tolerance: 4).Value);

        var png = new MoleculeImageExporter(renderer).RenderPng(depiction, 400, 300, background: SKColors.White);
        Assert.True(png.Length > 1000);

        using var bitmap = SKBitmap.Decode(png);
        Assert.Equal(400, bitmap.Width);

        // the depiction is actually drawn: the molecule covers more than an empty corner
        var drawn = 0;
        for (var y = 0; y < bitmap.Height; y += 4)
        {
            for (var x = 0; x < bitmap.Width; x += 4)
            {
                if (bitmap.GetPixel(x, y) != SKColors.White)
                    drawn++;
            }
        }

        Assert.True(drawn > 50, $"only {drawn} non-white pixels");
    }

    [Fact]
    public void MissingRingsAndKekuleOrders_StillRender()
    {
        var bare = new MoleculeDepiction(
            [new DepictionAtom(0, "C", 0, 0, 3, 0, null, false, HydrogenPosition.Right),
             new DepictionAtom(1, "C", 1.5, 0, 3, 0, null, false, HydrogenPosition.Right)],
            [new DepictionBond(0, 1, BondOrder.Single, BondStereoDisplay.None, DoubleBondPlacement.Center) { Index = 0 }]);

        var png = new MoleculeImageExporter().RenderPng(bare, 200, 150, background: SKColors.White);
        Assert.True(png.Length > 200);
    }
}
