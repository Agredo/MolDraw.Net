using Microsoft.Extensions.DependencyInjection;
using MolDraw.Core.Ncdk;
using MolDraw.Depiction;

namespace MolDraw.Core.Tests;

public class ChemistryEngineTests
{
    [Fact]
    public void AddMolDrawCoreRegistersNcdkEngine()
    {
        using var provider = new ServiceCollection().AddMolDrawCore().BuildServiceProvider();

        var engine = provider.GetRequiredService<IChemistryEngine>();

        Assert.IsType<NcdkDepictionEngine>(engine);
        Assert.Equal("NCDK", engine.Name);
    }

    [Fact]
    public void PreviouslyRegisteredEngineIsKept()
    {
        var custom = new FixedEngine();

        using var provider = new ServiceCollection()
            .AddSingleton<IChemistryEngine>(custom)
            .AddMolDrawCore()
            .BuildServiceProvider();

        Assert.Same(custom, provider.GetRequiredService<IChemistryEngine>());
    }

    [Fact]
    public void EngineDepictsSmilesThroughTheSeam()
    {
        IChemistryEngine engine = new NcdkDepictionEngine();

        var depiction = engine.Depict("c1ccccc1O");

        Assert.Equal(7, depiction.Atoms.Count);
        Assert.NotEmpty(depiction.Bonds);
        Assert.NotEmpty(depiction.Rings);
        Assert.True(depiction.GetMedianBondLength() > 1.0);
    }

    [Fact]
    public void EngineReportedParseErrors()
    {
        IChemistryEngine engine = new NcdkDepictionEngine();

        var ex = Assert.Throws<MoleculeParseException>(() => engine.Depict("CC("));

        Assert.Equal("CC(", ex.Input);
        Assert.Equal(2, ex.Position);
    }

    [Fact]
    public void ConsumerCanDepictWithAnyEngine()
    {
        // A renderer only needs the engine-neutral result; here it comes from a non-NCDK implementation.
        using var provider = new ServiceCollection()
            .AddSingleton<IChemistryEngine>(new FixedEngine())
            .BuildServiceProvider();

        var depiction = provider.GetRequiredService<IChemistryEngine>().Depict("CCO");

        Assert.Equal(2, depiction.Atoms.Count);
        Assert.Single(depiction.Bonds);
    }

    private sealed class FixedEngine : IChemistryEngine
    {
        public string Name => "Fixed";

        // A non-NCDK implementation proves that consumers only need the engine-neutral depiction model.
        public MoleculeDepiction Depict(string smiles, DepictionBuildOptions? options = null) =>
            new(
                [
                    new DepictionAtom(0, "C", 0, 0, 3, 0, null, false, HydrogenPosition.Right),
                    new DepictionAtom(1, "C", 1.5, 0, 3, 0, null, false, HydrogenPosition.Right),
                ],
                [new DepictionBond(0, 1, BondOrder.Single, BondStereoDisplay.None, DoubleBondPlacement.Center)]);
    }
}
