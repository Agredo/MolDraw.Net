using MolDraw.Core;
using MolDraw.Core.Ncdk;
using MolDraw.Depiction;
using SkiaSharp;

namespace MolDraw.Skia.Tests;

internal static class Molecules
{
    private static readonly MoleculeLoader Loader = new();
    private static readonly DepictionBuilder Builder = new();

    public static readonly (string Name, string Smiles)[] Samples =
    [
        ("caffeine", "CN1C=NC2=C1C(=O)N(C(=O)N2C)C"),
        ("aspirin", "CC(=O)OC1=CC=CC=C1C(=O)O"),
        ("glucose", "OC[C@H]1OC(O)[C@H](O)[C@@H](O)[C@@H]1O"),
        ("cholesterol", "C[C@H](CCCC(C)C)[C@H]1CC[C@@H]2[C@@]1(CC[C@H]3[C@H]2CC=C4[C@@]3(CC[C@@H](C4)O)C)C"),
        ("benzene", "c1ccccc1"),
        ("charged-isotope", "[13CH3][N+](C)(C)CC([O-])=O"),
        ("stereo", "C[C@@H](C(=O)O)N"),
    ];

    public static MoleculeDepiction Depict(string smiles) => Builder.Build(Loader.ParseSmiles(smiles));

    /// <summary>Straight chain along the x axis (bond length 1.5), no labels.</summary>
    public static MoleculeDepiction Chain(int atoms, double bondLength = 1.5)
    {
        var a = Enumerable.Range(0, atoms).Select(i => new DepictionAtom(i, "C", i * bondLength, 0, 2, 0, null, false, HydrogenPosition.Right) { AtomicNumber = 6 }).ToArray();
        var b = Enumerable.Range(0, atoms - 1).Select(i => new DepictionBond(i, i + 1, BondOrder.Single, BondStereoDisplay.None, DoubleBondPlacement.Center) { Index = i }).ToArray();
        return new MoleculeDepiction(a, b);
    }

    public static SKBitmap Render(MoleculeDepiction d, int w, int h, MoleculeRenderOptions? options = null, MoleculeHighlights? highlights = null, SKColor? background = null)
        => new MoleculeImageExporter().RenderBitmap(d, w, h, options, highlights, background);

    public static bool Near(SKColor a, SKColor b, int tolerance = 12)
        => Math.Abs(a.Red - b.Red) <= tolerance && Math.Abs(a.Green - b.Green) <= tolerance && Math.Abs(a.Blue - b.Blue) <= tolerance;
}
