using MolDraw.Depiction;

namespace MolDraw.Core.Tests;

public class MoleculeDepictionTests
{
    private static DepictionAtom Atom(int i, double x = 0, double y = 0) => new(i, "C", x, y, 0, 0, null, false, HydrogenPosition.Right);

    [Fact]
    public void RejectsMismatchedAtomIndex()
        => Assert.Throws<ArgumentException>(() => new MoleculeDepiction([Atom(1)], []));

    [Fact]
    public void RejectsBondToUnknownAtom()
        => Assert.Throws<ArgumentException>(() => new MoleculeDepiction([Atom(0)], [new DepictionBond(0, 1, BondOrder.Single, BondStereoDisplay.None, DoubleBondPlacement.Center)]));

    [Fact]
    public void RejectsNonFiniteCoordinates()
        => Assert.Throws<ArgumentException>(() => new MoleculeDepiction([Atom(0, double.NaN)], []));

    [Fact]
    public void WithExpressionRevalidates()
    {
        var d = new MoleculeDepiction([Atom(0), Atom(1, 1)], [new DepictionBond(0, 1, BondOrder.Single, BondStereoDisplay.None, DoubleBondPlacement.Center)]);

        Assert.Throws<ArgumentException>(() => d with { Atoms = [Atom(0)] });
    }

    [Fact]
    public void BoundsAndMedianBondLength()
    {
        var d = new MoleculeDepiction(
            [Atom(0), Atom(1, 2), Atom(2, 2, 1)],
            [new DepictionBond(0, 1, BondOrder.Single, BondStereoDisplay.None, DoubleBondPlacement.Center), new DepictionBond(1, 2, BondOrder.Single, BondStereoDisplay.None, DoubleBondPlacement.Center) { Index = 1 }]);

        var bounds = d.GetBounds();
        Assert.Equal(2, bounds.Width);
        Assert.Equal(1, bounds.Height);
        Assert.Equal(1.5, d.GetMedianBondLength());
    }

    [Fact]
    public void KekuleOrderDefaultsToOrder()
        => Assert.Equal(BondOrder.Double, new DepictionBond(0, 1, BondOrder.Double, BondStereoDisplay.None, DoubleBondPlacement.Center).KekuleOrder);
}
