using MolDraw.Core.Internal;
using MolDraw.Core.Ncdk;
using MolDraw.Depiction;
using NCDK;
using NCDK.Layout;
using Microsoft.Extensions.DependencyInjection;
using BondOrder = MolDraw.Depiction.BondOrder;

namespace MolDraw.Core.Tests;

public class DepictionBuilderTests
{
    private readonly MoleculeLoader _loader = new();
    private readonly DepictionBuilder _builder = new();

    [Fact]
    public void GeneratesFiniteCoordinatesWhenMissing()
    {
        var depiction = _builder.Build(_loader.ParseSmiles("CN1C=NC2=C1C(=O)N(C(=O)N2C)C"));

        Assert.All(depiction.Atoms, a => Assert.True(double.IsFinite(a.X) && double.IsFinite(a.Y)));
        var median = depiction.GetMedianBondLength();
        Assert.NotNull(median);
        Assert.Equal(1.5, median.Value, 2);
        Assert.Equal(depiction.Atoms.Count, depiction.Atoms.Select(a => (Math.Round(a.X, 3), Math.Round(a.Y, 3))).Distinct().Count());
    }

    [Fact]
    public void KeepsExistingCoordinates()
    {
        using var stream = TestData.Stream(TestData.EthanolMolV2000);
        var depiction = _builder.Build(_loader.Load(stream, MoleculeFormat.MolFile));

        Assert.Equal([0.0, 1.299, 2.598], depiction.Atoms.Select(a => Math.Round(a.X, 3)).ToArray());
        Assert.Equal([0.0, 0.75, 0.0], depiction.Atoms.Select(a => Math.Round(a.Y, 3)).ToArray());
    }

    [Fact]
    public void ForceLayoutRecomputesCoordinates()
    {
        using var stream = TestData.Stream(TestData.EthanolMolV2000);
        var depiction = _builder.Build(_loader.Load(stream, MoleculeFormat.MolFile), new DepictionBuildOptions { ForceLayout = true });

        Assert.Equal(1.5, depiction.GetMedianBondLength()!.Value, 2);
    }

    [Fact]
    public void ZeroCoordinatesTriggerLayout()
    {
        var mol = TestData.Mol([("C", 0, 0), ("C", 0, 0), ("O", 0, 0)], [(1, 2, 1), (2, 3, 1)]);
        using var stream = TestData.Stream(mol);

        var depiction = _builder.Build(_loader.Load(stream, MoleculeFormat.MolFile));

        Assert.Equal(1.5, depiction.GetMedianBondLength()!.Value, 2);
    }

    [Fact]
    public void AromaticBenzeneHasAromaticBondsKekuleOrdersAndAromaticRing()
    {
        var depiction = _builder.Build(_loader.ParseSmiles("c1ccccc1"));

        Assert.All(depiction.Bonds, b => Assert.Equal(BondOrder.Aromatic, b.Order));
        Assert.Equal(3, depiction.Bonds.Count(b => b.KekuleOrder == BondOrder.Double));
        Assert.Equal(3, depiction.Bonds.Count(b => b.KekuleOrder == BondOrder.Single));
        var ring = Assert.Single(depiction.Rings);
        Assert.True(ring.IsAromatic);
        Assert.Equal(6, ring.AtomIndices.Count);
        Assert.All(depiction.Atoms, a => Assert.True(a.IsAromatic));
    }

    [Fact]
    public void KekuleInputIsPerceivedAsAromatic()
    {
        var depiction = _builder.Build(_loader.ParseSmiles("C1=CC=CC=C1"));

        Assert.All(depiction.Bonds, b => Assert.Equal(BondOrder.Aromatic, b.Order));
    }

    [Fact]
    public void AromaticityPerceptionCanBeDisabled()
    {
        var depiction = _builder.Build(_loader.ParseSmiles("C1=CC=CC=C1"), new DepictionBuildOptions { PerceiveAromaticity = false });

        Assert.Equal(3, depiction.Bonds.Count(b => b.Order == BondOrder.Double));
        Assert.False(depiction.Rings[0].IsAromatic);
    }

    [Fact]
    public void RingDoubleBondsPointTowardsRingCentre()
    {
        var depiction = _builder.Build(_loader.ParseSmiles("C1=CCCCC1"));

        var db = Assert.Single(depiction.Bonds, b => b.Order == BondOrder.Double);
        Assert.NotEqual(DoubleBondPlacement.Center, db.Placement);
        var a = depiction.Atoms[db.Begin];
        var e = depiction.Atoms[db.End];
        var cx = depiction.Atoms.Average(x => x.X);
        var cy = depiction.Atoms.Average(x => x.Y);
        var cross = ((e.X - a.X) * (cy - a.Y)) - ((e.Y - a.Y) * (cx - a.X));
        Assert.Equal(cross > 0 ? DoubleBondPlacement.Left : DoubleBondPlacement.Right, db.Placement);
    }

    [Fact]
    public void AromaticKekuleDoubleBondsArePlacedInsideRing()
    {
        var depiction = _builder.Build(_loader.ParseSmiles("c1ccccc1"));

        Assert.All(depiction.Bonds.Where(b => b.KekuleOrder == BondOrder.Double), b => Assert.NotEqual(DoubleBondPlacement.Center, b.Placement));
    }

    [Fact]
    public void KetoneDoubleBondIsCentred()
    {
        var depiction = _builder.Build(_loader.ParseSmiles("CC(=O)C"));

        Assert.Equal(DoubleBondPlacement.Center, Assert.Single(depiction.Bonds, b => b.Order == BondOrder.Double).Placement);
    }

    [Fact]
    public void ChargesIsotopesAndLabelsAreMapped()
    {
        var depiction = _builder.Build(_loader.ParseSmiles("[13CH3][N+](C)(C)CC([O-])=O"));

        Assert.Equal(13, depiction.Atoms[0].MassNumber);
        Assert.True(depiction.Atoms[0].ShowLabel);
        Assert.Equal(1, depiction.Atoms[1].Charge);
        Assert.True(depiction.Atoms[1].ShowLabel);
        Assert.False(depiction.Atoms[2].ShowLabel);
        Assert.Equal(-1, depiction.Atoms[6].Charge);
        Assert.Equal(6, depiction.Atoms[0].AtomicNumber);
        Assert.Equal(7, depiction.Atoms[1].AtomicNumber);
    }

    [Theory]
    [InlineData(CarbonLabelMode.Default, 0)]
    [InlineData(CarbonLabelMode.Terminal, 2)]
    [InlineData(CarbonLabelMode.All, 3)]
    public void CarbonLabelModes(CarbonLabelMode mode, int labelledCarbons)
    {
        var depiction = _builder.Build(_loader.ParseSmiles("CCC"), new DepictionBuildOptions { CarbonLabels = mode });

        Assert.Equal(labelledCarbons, depiction.Atoms.Count(a => a.ShowLabel));
    }

    [Fact]
    public void IsolatedCarbonIsLabelled()
        => Assert.True(_builder.Build(_loader.ParseSmiles("C")).Atoms[0].ShowLabel);

    [Fact]
    public void RadicalsAreMapped()
    {
        var mol = TestData.Mol([("C", 0, 0), ("C", 1.5, 0)], [(1, 2, 1)]).Replace("M  END", "M  RAD  1   1   2\nM  END", StringComparison.Ordinal);
        using var stream = TestData.Stream(mol);

        var depiction = _builder.Build(_loader.Load(stream, MoleculeFormat.MolFile));

        Assert.Equal(1, depiction.Atoms[0].Radicals);
        Assert.True(depiction.Atoms[0].ShowLabel);
        Assert.Equal(0, depiction.Atoms[1].Radicals);
    }

    [Fact]
    public void TetrahedralStereoProducesWedgeAtStereocentre()
    {
        var depiction = _builder.Build(_loader.ParseSmiles("C[C@@H](C(=O)O)N"));

        var wedge = Assert.Single(depiction.Bonds, b => b.Stereo != BondStereoDisplay.None);
        Assert.Equal(1, wedge.Begin);
        Assert.Contains(wedge.Stereo, new[] { BondStereoDisplay.Wedge, BondStereoDisplay.Hash });
    }

    [Fact]
    public void EnantiomersHaveOppositeWedgeDisplayOrGeometry()
    {
        var l = _builder.Build(_loader.ParseSmiles("C[C@@H](C(=O)O)N"));
        var d = _builder.Build(_loader.ParseSmiles("C[C@H](C(=O)O)N"));

        var wl = l.Bonds.Single(b => b.Stereo != BondStereoDisplay.None);
        var wd = d.Bonds.Single(b => b.Stereo != BondStereoDisplay.None);
        var sameGeometry = l.Atoms.Zip(d.Atoms).All(p => Math.Abs(p.First.X - p.Second.X) < 1e-6 && Math.Abs(p.First.Y - p.Second.Y) < 1e-6);
        if (sameGeometry && wl.End == wd.End)
            Assert.NotEqual(wl.Stereo, wd.Stereo);
    }

    [Fact]
    public void CholesterolHasWedgesForAllStereocentres()
    {
        var depiction = _builder.Build(_loader.ParseSmiles(RdkitReference.Cases[3].Smiles));

        Assert.Equal(8, depiction.Bonds.Count(b => b.Stereo is BondStereoDisplay.Wedge or BondStereoDisplay.Hash));
        Assert.Equal(4, depiction.Rings.Count);
    }

    [Theory]
    [InlineData("C[C@@H](C(=O)O)N")]
    [InlineData("C[C@H](C(=O)O)N")]
    [InlineData("OC[C@H]1OC(O)[C@H](O)[C@@H](O)[C@@H]1O")]
    [InlineData("C[C@H](CCCC(C)C)[C@H]1CC[C@@H]2[C@@]1(CC[C@H]3[C@H]2CC=C4[C@@]3(CC[C@@H](C4)O)C)C")]
    [InlineData("N[C@@](C)(F)C(=O)O")]
    [InlineData("F[C@](Cl)(Br)I")]
    public void WedgeOrientationSignMatchesNcdk(string smiles)
    {
        // NCDK (NonplanarBonds) assigns wedges during layout - use it as ground truth for our fallback assigner.
        var mol = LayoutKeepingStereoElements(smiles);
        var checkedBonds = 0;
        foreach (var tc in mol.StereoElements.OfType<ITetrahedralChirality>())
        {
            foreach (var bond in mol.GetConnectedBonds(tc.ChiralAtom))
            {
                var stereo = bond.Stereo;
                if (stereo is not (BondStereo.Up or BondStereo.Down) || bond.Begin.Index != tc.ChiralAtom.Index)
                    continue;
                var other = bond.GetOther(tc.ChiralAtom);
                var orientation = WedgeAssigner.OrientationWithRaised(tc.ChiralAtom, tc.Ligands, other, stereo == BondStereo.Up ? 1 : -1);
                Assert.Equal(WedgeAssigner.ExpectedOrientation(tc.Stereo), orientation);
                checkedBonds++;
            }
        }

        Assert.True(checkedBonds > 0);
    }

    [Fact]
    public void MissingWedgesAreAssignedForExistingCoordinates()
    {
        var mol = LayoutKeepingStereoElements(RdkitReference.Cases[3].Smiles);
        var expected = mol.Bonds.Select(b => b.Stereo).ToArray();
        foreach (var b in mol.Bonds)
            b.Stereo = BondStereo.None;

        var depiction = _builder.Build(mol);

        var wedged = depiction.Bonds.Where(b => b.Stereo != BondStereoDisplay.None).ToList();
        Assert.Equal(8, wedged.Count);

        // every assigned wedge must reproduce the configuration
        foreach (var tc in mol.StereoElements.OfType<ITetrahedralChirality>())
        {
            var focus = tc.ChiralAtom.Index;
            var w = wedged.Single(b => b.Begin == focus);
            var raised = mol.Atoms[w.End];
            var orientation = WedgeAssigner.OrientationWithRaised(tc.ChiralAtom, tc.Ligands, raised, w.Stereo == BondStereoDisplay.Wedge ? 1 : -1);
            Assert.Equal(WedgeAssigner.ExpectedOrientation(tc.Stereo), orientation);
        }

        Assert.NotEmpty(expected);
    }

    /// <summary>
    /// NCDK 1.6.1's StructureDiagramGenerator corrupts the ligand list of tetrahedral centres with an implicit hydrogen
    /// (the focus is replaced by a duplicate ligand). Lay out a copy and transfer coordinates and wedges instead.
    /// </summary>
    private IAtomContainer LayoutKeepingStereoElements(string smiles)
    {
        var mol = _loader.ParseSmiles(smiles);
        var copy = _loader.ParseSmiles(smiles);
        new StructureDiagramGenerator().GenerateCoordinates(copy);
        for (var i = 0; i < mol.Atoms.Count; i++)
            mol.Atoms[i].Point2D = copy.Atoms[i].Point2D;
        for (var i = 0; i < mol.Bonds.Count; i++)
        {
            var src = copy.Bonds[i];
            var dst = mol.Bonds[i];
            dst.Stereo = src.Begin.Index == dst.Begin.Index ? src.Stereo : src.Stereo switch
            {
                BondStereo.Up => BondStereo.UpInverted,
                BondStereo.Down => BondStereo.DownInverted,
                _ => src.Stereo,
            };
        }

        return mol;
    }

    [Fact]
    public void HydrogenPositionsFollowNeighbours()
    {
        Assert.Equal(HydrogenPosition.Left, DepictionGeometry.HydrogenPositionFor(0, 0, [(1, 0)], "O"));
        Assert.Equal(HydrogenPosition.Right, DepictionGeometry.HydrogenPositionFor(0, 0, [(-1, 0)], "O"));
        Assert.Equal(HydrogenPosition.Below, DepictionGeometry.HydrogenPositionFor(0, 0, [(-1, 0.5), (1, 0.5)], "N"));
        Assert.Equal(HydrogenPosition.Above, DepictionGeometry.HydrogenPositionFor(0, 0, [(-1, -0.5), (1, -0.5)], "N"));
        Assert.Equal(HydrogenPosition.Left, DepictionGeometry.HydrogenPositionFor(0, 0, [], "O"));
        Assert.Equal(HydrogenPosition.Right, DepictionGeometry.HydrogenPositionFor(0, 0, [], "N"));
    }

    [Fact]
    public void DependencyInjectionRegistersServices()
    {
        using var provider = new ServiceCollection().AddMolDrawCore().BuildServiceProvider();

        Assert.IsType<MoleculeLoader>(provider.GetRequiredService<IMoleculeLoader>());
        Assert.IsType<DepictionBuilder>(provider.GetRequiredService<IDepictionBuilder>());
    }
}

