using MolDraw.Depiction;
using SkiaSharp;

namespace MolDraw.Skia.Tests;

public class RenderingTests
{
    public static TheoryData<string, string> Samples()
    {
        var data = new TheoryData<string, string>();
        foreach (var (name, smiles) in Molecules.Samples)
            data.Add(name, smiles);
        return data;
    }

    private static string SnapshotDirectory()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "MolDraw.Net.slnx")))
            dir = Path.GetDirectoryName(dir);
        var target = Path.Combine(dir ?? AppContext.BaseDirectory, "artifacts", "snapshots");
        Directory.CreateDirectory(target);
        return target;
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public void PngSmokeSnapshot(string name, string smiles)
    {
        var d = Molecules.Depict(smiles);
        var exporter = new MoleculeImageExporter();
        var weights = Enumerable.Range(0, d.Atoms.Count).Select(i => (float)i / Math.Max(1, d.Atoms.Count - 1)).ToArray();

        foreach (var (suffix, options, highlights, bg) in new (string, MoleculeRenderOptions, MoleculeHighlights?, SKColor)[]
                 {
                     ("light", MoleculeRenderOptions.Light, null, SKColors.White),
                     ("dark", MoleculeRenderOptions.Dark with { AromaticStyle = AromaticStyle.Circle }, null, new SKColor(0x1E, 0x1E, 0x1E)),
                     ("weights", MoleculeRenderOptions.Light with { ShowColorBar = true, ShowAtomIndices = true }, new MoleculeHighlights { AtomWeights = weights, SelectedAtom = 0, HoveredAtom = d.Atoms.Count - 1 }, SKColors.White),
                 })
        {
            var png = exporter.RenderPng(d, 400, 300, options, highlights, bg);
            File.WriteAllBytes(Path.Combine(SnapshotDirectory(), $"{name}-{suffix}.png"), png);

            using var decoded = SKBitmap.Decode(png);
            Assert.Equal(400, decoded.Width);
            Assert.Equal(300, decoded.Height);
            var inked = 0;
            for (var y = 0; y < decoded.Height; y += 2)
            {
                for (var x = 0; x < decoded.Width; x += 2)
                {
                    if (!Molecules.Near(decoded.GetPixel(x, y), bg, 30))
                        inked++;
                }
            }

            Assert.True(inked > 100, $"{name}-{suffix}: only {inked} drawn pixels");
        }
    }

    [Fact]
    public void TransparentBackgroundByDefault()
    {
        using var bmp = Molecules.Render(Molecules.Depict("CCO"), 200, 100);

        Assert.Equal(0, bmp.GetPixel(1, 1).Alpha);
        Assert.Equal(SKColors.Transparent, MoleculeRenderOptions.Light.BackgroundColor);
        Assert.Equal(SKColors.Transparent, MoleculeRenderOptions.Dark.BackgroundColor);
    }

    [Fact]
    public void BackgroundOptionFillsBounds()
    {
        using var bmp = Molecules.Render(Molecules.Depict("CCO"), 200, 100, MoleculeRenderOptions.Light with { BackgroundColor = SKColors.Yellow });

        Assert.Equal(SKColors.Yellow, bmp.GetPixel(1, 1));
    }

    [Fact]
    public void DarkPresetDrawsLightBonds()
    {
        var d = Molecules.Chain(2);
        var r = new MoleculeRenderer().Layout(new SKRect(0, 0, 200, 100), d, MoleculeRenderOptions.Dark);
        using var bmp = Molecules.Render(d, 200, 100, MoleculeRenderOptions.Dark with { BondWidth = 4 }, background: SKColors.Black);

        var mid = new SKPoint((r.AtomPositions[0].X + r.AtomPositions[1].X) / 2, r.AtomPositions[0].Y);
        Assert.True(bmp.GetPixel((int)mid.X, (int)mid.Y).Red > 180);
    }

    [Fact]
    public void HeteroatomLabelsUseElementColours()
    {
        var d = Molecules.Depict("O");
        var options = MoleculeRenderOptions.Light with { FontSize = 30, MaxBondLength = 60 };

        using var bmp = Molecules.Render(d, 120, 120, options, background: SKColors.White);

        var oxygen = AtomColorScheme.Cpk.GetColor(8, SKColors.Black);
        var found = false;
        for (var y = 0; y < 120 && !found; y++)
        {
            for (var x = 0; x < 120 && !found; x++)
                found = Molecules.Near(oxygen, bmp.GetPixel(x, y), 20);
        }

        Assert.True(found);
    }

    [Fact]
    public void WedgeIsFilledAndHashIsNot()
    {
        DepictionAtom A(int i, double x) => new(i, "C", x, 0, 0, 0, null, false, HydrogenPosition.Right) { AtomicNumber = 6 };
        MoleculeDepiction Make(BondStereoDisplay s) => new([A(0, 0), A(1, 1.5)], [new DepictionBond(0, 1, BondOrder.Single, s, DoubleBondPlacement.Center)]);
        var options = MoleculeRenderOptions.Light with { MaxBondLength = 100, WedgeWidth = 0.3f };

        int Count(MoleculeDepiction d)
        {
            using var bmp = Molecules.Render(d, 200, 120, options, background: SKColors.White);
            var n = 0;
            for (var y = 0; y < 120; y++)
            {
                for (var x = 0; x < 200; x++)
                {
                    if (bmp.GetPixel(x, y).Red < 200)
                        n++;
                }
            }

            return n;
        }

        var plain = Count(Make(BondStereoDisplay.None));
        var wedge = Count(Make(BondStereoDisplay.Wedge));
        var hash = Count(Make(BondStereoDisplay.Hash));
        var wavy = Count(Make(BondStereoDisplay.Wavy));
        Assert.True(wedge > plain * 4, $"wedge {wedge} vs plain {plain}");
        Assert.True(hash > plain && hash < wedge, $"hash {hash}");
        Assert.True(wavy > plain, $"wavy {wavy}");
    }

    [Fact]
    public void DoubleAndTripleBondsDrawMoreInk()
    {
        DepictionAtom A(int i, double x) => new(i, "C", x, 0, 0, 0, null, false, HydrogenPosition.Right) { AtomicNumber = 6 };
        int Ink(BondOrder order, DoubleBondPlacement placement)
        {
            var d = new MoleculeDepiction([A(0, 0), A(1, 1.5)], [new DepictionBond(0, 1, order, BondStereoDisplay.None, placement)]);
            using var bmp = Molecules.Render(d, 200, 120, MoleculeRenderOptions.Light with { MaxBondLength = 100 }, background: SKColors.White);
            var n = 0;
            for (var y = 0; y < 120; y++)
            {
                for (var x = 0; x < 200; x++)
                {
                    if (bmp.GetPixel(x, y).Red < 200)
                        n++;
                }
            }

            return n;
        }

        var single = Ink(BondOrder.Single, DoubleBondPlacement.Center);
        Assert.True(Ink(BondOrder.Double, DoubleBondPlacement.Center) > single * 1.7);
        Assert.True(Ink(BondOrder.Double, DoubleBondPlacement.Left) > single * 1.4);
        Assert.True(Ink(BondOrder.Triple, DoubleBondPlacement.Center) > single * 2.5);
    }

    [Fact]
    public void LeftPlacementIsAboveForBondPointingRight()
    {
        // model: bond along +x; "left" side is +y (up) -> smaller canvas y
        DepictionAtom A(int i, double x) => new(i, "C", x, 0, 0, 0, null, false, HydrogenPosition.Right) { AtomicNumber = 6 };
        var d = new MoleculeDepiction([A(0, 0), A(1, 1.5)], [new DepictionBond(0, 1, BondOrder.Double, BondStereoDisplay.None, DoubleBondPlacement.Left)]);
        var options = MoleculeRenderOptions.Light with { MaxBondLength = 100 };
        var r = new MoleculeRenderer().Layout(new SKRect(0, 0, 200, 120), d, options);
        using var bmp = Molecules.Render(d, 200, 120, options, background: SKColors.White);

        var x = (int)((r.AtomPositions[0].X + r.AtomPositions[1].X) / 2);
        var y = (int)r.AtomPositions[0].Y;
        var offset = (int)Math.Round(options.MultipleBondSpacing * r.BondLength);
        int Ink(int from, int to)
        {
            var n = 0;
            for (var yy = from; yy <= to; yy++)
            {
                if (bmp.GetPixel(x, yy).Red < 200)
                    n++;
            }

            return n;
        }

        Assert.True(Ink(y - offset - 3, y - offset + 3) > 0, "second line should be above");
        Assert.Equal(0, Ink(y + 3, y + offset + 3));
    }

    [Fact]
    public void SvgExportProducesDocument()
    {
        var svg = new MoleculeImageExporter().RenderSvg(Molecules.Depict("CC(=O)O"), 300, 200, highlights: new MoleculeHighlights { AtomWeights = [0, 0.3f, 0.6f, 1] });

        Assert.Contains("<svg", svg, StringComparison.Ordinal);
        Assert.Contains("</svg>", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidSizesThrow()
    {
        var exporter = new MoleculeImageExporter();
        Assert.Throws<ArgumentOutOfRangeException>(() => exporter.RenderPng(Molecules.Chain(2), 0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => exporter.RenderSvg(Molecules.Chain(2), 10, -1));
    }

    [Fact]
    public void CustomFontFamilyIsAccepted()
    {
        var png = new MoleculeImageExporter().RenderPng(Molecules.Depict("CCO"), 100, 100, MoleculeRenderOptions.Light with { FontFamily = "Arial" });

        Assert.NotEmpty(png);
    }
}

