using MolDraw.Core.Ncdk;
using MolDraw.Depiction;

namespace MolDraw.Core.Tests;

public class AtomOrderTests
{
    private readonly MoleculeLoader _loader = new();
    private readonly DepictionBuilder _builder = new();

    [Theory]
    [MemberData(nameof(RdkitReference.Indices), MemberType = typeof(RdkitReference))]
    public void ParsedMoleculeMatchesRdkitAtomOrder(int caseIndex)
    {
        var (smiles, symbols, charges, isotopes, hydrogens) = RdkitReference.Cases[caseIndex];

        var mol = _loader.ParseSmiles(smiles);

        Assert.Equal(symbols, mol.Atoms.Select(a => a.Symbol).ToArray());
        Assert.Equal(charges, mol.Atoms.Select(a => a.FormalCharge ?? 0).ToArray());
        Assert.Equal(isotopes, mol.Atoms.Select(a => a.MassNumber).ToArray());
        Assert.Equal(hydrogens, mol.Atoms.Select(a => a.ImplicitHydrogenCount ?? 0).ToArray());
    }

    [Theory]
    [MemberData(nameof(RdkitReference.Indices), MemberType = typeof(RdkitReference))]
    public void DepictionMatchesRdkitAtomOrder(int caseIndex)
    {
        var (smiles, symbols, charges, isotopes, hydrogens) = RdkitReference.Cases[caseIndex];

        var depiction = _builder.Build(_loader.ParseSmiles(smiles));

        Assert.Equal(symbols, depiction.Atoms.Select(a => a.Symbol).ToArray());
        Assert.Equal(charges, depiction.Atoms.Select(a => a.Charge).ToArray());
        Assert.Equal(isotopes, depiction.Atoms.Select(a => a.MassNumber).ToArray());
        Assert.Equal(hydrogens, depiction.Atoms.Select(a => a.ImplicitHydrogens).ToArray());
        Assert.Equal(Enumerable.Range(0, symbols.Length), depiction.Atoms.Select(a => a.Index));
    }

    [Fact]
    public void BondIndicesFollowMolecule()
    {
        var mol = _loader.ParseSmiles("CC(=O)OC1=CC=CC=C1C(=O)O");
        var depiction = _builder.Build(mol);

        Assert.Equal(mol.Bonds.Count, depiction.Bonds.Count);
        for (var i = 0; i < mol.Bonds.Count; i++)
        {
            var b = depiction.Bonds[i];
            Assert.Equal(i, b.Index);
            var expected = new[] { mol.Bonds[i].Begin.Index, mol.Bonds[i].End.Index }.Order();
            Assert.Equal(expected, new[] { b.Begin, b.End }.Order());
        }
    }

    [Fact]
    public void KeepExplicitHydrogensOptionKeepsHydrogenAtoms()
    {
        var loader = new MoleculeLoader(new MoleculeLoaderOptions { KeepExplicitHydrogens = true });

        var mol = loader.ParseSmiles("[H]OC([H])([H])[H]");

        Assert.Equal(["H", "O", "C", "H", "H", "H"], mol.Atoms.Select(a => a.Symbol).ToArray());
        var depiction = _builder.Build(mol);
        Assert.Equal(6, depiction.Atoms.Count);
    }

    [Fact]
    public void BuildDoesNotModifyInput()
    {
        var mol = _loader.ParseSmiles("c1ccccc1O");
        Assert.All(mol.Atoms, a => Assert.Null(a.Point2D));

        _builder.Build(mol);

        Assert.All(mol.Atoms, a => Assert.Null(a.Point2D));
    }

    [Fact]
    public void DisconnectedFragmentsKeepOrder()
    {
        var depiction = _builder.Build(_loader.ParseSmiles("[Na+].[O-]C(=O)C"));

        Assert.Equal(["Na", "O", "C", "O", "C"], depiction.Atoms.Select(a => a.Symbol).ToArray());
        Assert.True(depiction.Atoms[0].ShowLabel);
        Assert.Equal(1, depiction.Atoms[0].Charge);
    }

    [Fact]
    public void EmptySmilesGivesEmptyDepiction()
    {
        var depiction = _builder.Build(_loader.ParseSmiles("  "));

        Assert.Empty(depiction.Atoms);
        Assert.Empty(depiction.Bonds);
    }
}
