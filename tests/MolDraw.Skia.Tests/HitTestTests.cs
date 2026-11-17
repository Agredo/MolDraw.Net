using SkiaSharp;

namespace MolDraw.Skia.Tests;

public class HitTestTests
{
    private readonly RenderedMolecule _r = new MoleculeRenderer().Layout(new SKRect(0, 0, 400, 200), Molecules.Chain(3), MoleculeRenderOptions.Light);

    [Fact]
    public void HitsAtomAtItsPosition()
    {
        for (var i = 0; i < 3; i++)
            Assert.Equal(i, _r.HitTestAtom(_r.AtomPositions[i]));
    }

    [Fact]
    public void HitsAtomWithinRadius()
    {
        var p = _r.AtomPositions[1];
        Assert.Equal(1, _r.HitTestAtom(new SKPoint(p.X, p.Y + (_r.AtomRadius * 0.9f))));
        Assert.Null(_r.HitTestAtom(new SKPoint(p.X, p.Y + (_r.AtomRadius * 1.2f))));
        Assert.Equal(1, _r.HitTestAtom(new SKPoint(p.X, p.Y + (_r.AtomRadius * 1.2f)), tolerance: _r.AtomRadius));
    }

    [Fact]
    public void MissesFarAway()
        => Assert.Null(_r.HitTestAtom(new SKPoint(-100, -100)));

    [Fact]
    public void HitsBondAtMidpointButNotOnAtoms()
    {
        var a = _r.AtomPositions[0];
        var b = _r.AtomPositions[1];
        var mid = new SKPoint((a.X + b.X) / 2, (a.Y + b.Y) / 2);

        Assert.Equal(0, _r.HitTestBond(mid));
        Assert.Equal(0, _r.HitTestBond(new SKPoint(mid.X, mid.Y + (_r.BondHitWidth * 0.4f))));
        Assert.Null(_r.HitTestBond(new SKPoint(mid.X, mid.Y + _r.BondHitWidth * 2)));
        Assert.Null(_r.HitTestBond(a));
        Assert.Null(_r.HitTestAtom(mid));
    }

    [Fact]
    public void BondSegmentsMatchAtomPositions()
    {
        Assert.Equal(2, _r.BondSegments.Count);
        Assert.Equal(_r.AtomPositions[1], _r.BondSegments[0].End);
    }
}
