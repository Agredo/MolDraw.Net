using MolDraw.Core.Ncdk;
using MolDraw.Depiction;

namespace MolDraw.Core.Tests;

public class MoleculeLoaderTests
{
    private readonly MoleculeLoader _loader = new();

    [Theory]
    [InlineData("CC(", 2, "Unclosed branch")]
    [InlineData("C1CC", 3, "Unclosed ring")]
    [InlineData("Xx", 0, "nexpected character")]
    [InlineData("C%", 1, "must follow '%'")]
    public void InvalidSmilesThrowsWithPosition(string smiles, int position, string detailsFragment)
    {
        var ex = Assert.Throws<MoleculeParseException>(() => _loader.ParseSmiles(smiles));

        Assert.Equal(smiles, ex.Input);
        Assert.Equal(position, ex.Position);
        Assert.Contains(detailsFragment, ex.Details, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(smiles, ex.Message, StringComparison.Ordinal);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public void InvalidAromaticSystemThrowsWithoutPosition()
    {
        var ex = Assert.Throws<MoleculeParseException>(() => _loader.ParseSmiles("c1cccc1"));

        Assert.Null(ex.Position);
        Assert.Contains("kekul", ex.Details, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryParseReportsErrorsWithoutThrowing()
    {
        Assert.False(_loader.TryParseSmiles("C(C", out var mol, out var error));
        Assert.Null(mol);
        Assert.False(string.IsNullOrWhiteSpace(error));

        Assert.False(_loader.TryParseSmiles(null, out _, out error));
        Assert.NotNull(error);

        Assert.True(_loader.TryParseSmiles("CCO", out mol, out error));
        Assert.NotNull(mol);
        Assert.Null(error);
        Assert.Equal(3, mol.Atoms.Count);
    }

    [Fact]
    public void NullSmilesThrowsArgumentNull()
        => Assert.Throws<ArgumentNullException>(() => _loader.ParseSmiles(null!));

    [Fact]
    public void LoadsMolFileWithCoordinates()
    {
        using var stream = TestData.Stream(TestData.EthanolMolV2000);

        var mol = _loader.Load(stream, MoleculeFormat.MolFile);

        Assert.Equal(["C", "C", "O"], mol.Atoms.Select(a => a.Symbol).ToArray());
        Assert.Equal(1.2990, mol.Atoms[1].Point2D!.Value.X, 3);
    }

    [Fact]
    public void LoadsMolFileWithExplicitHydrogensSuppressed()
    {
        using var stream = TestData.Stream(TestData.MethanolWithHydrogensMol);

        var mol = _loader.Load(stream, MoleculeFormat.MolFile);

        Assert.Equal(["C", "O"], mol.Atoms.Select(a => a.Symbol).ToArray());
        Assert.Equal(3, mol.Atoms[0].ImplicitHydrogenCount);
        Assert.Equal(1, mol.Atoms[1].ImplicitHydrogenCount);
    }

    [Fact]
    public void LoadsAllSdfRecords()
    {
        using var stream = TestData.Stream(TestData.EthanolMolV2000 + "$$$$\n" + TestData.EthanolMolV2000 + "> <NAME>\nethanol\n\n$$$$\n");

        var all = _loader.LoadAll(stream, MoleculeFormat.Sdf);

        Assert.Equal(2, all.Count);
        Assert.All(all, m => Assert.Equal(3, m.Atoms.Count));
    }

    [Fact]
    public void LoadsSmilesFileWithTitles()
    {
        using var stream = TestData.Stream("# comment\nCCO ethanol\n\nc1ccccc1\tbenzene\n");

        var all = _loader.LoadAll(stream, MoleculeFormat.Smiles);

        Assert.Equal([3, 6], all.Select(m => m.Atoms.Count).ToArray());
        using var stream2 = TestData.Stream("CCO ethanol\nCC\n");
        Assert.Equal(3, _loader.Load(stream2, MoleculeFormat.Smiles).Atoms.Count);
    }

    [Fact]
    public void BrokenMolFileThrowsParseException()
    {
        using var stream = TestData.Stream("garbage\n\n\n  x  y  z\nM  END\n");

        Assert.Throws<MoleculeParseException>(() => _loader.Load(stream, MoleculeFormat.MolFile));
    }

    [Fact]
    public void EmptySdfThrowsParseException()
    {
        using var stream = TestData.Stream("\n\n");

        Assert.Throws<MoleculeParseException>(() => _loader.Load(stream, MoleculeFormat.Sdf));
    }

    [Fact]
    public async Task LoaderIsThreadSafe()
    {
        var smiles = RdkitReference.Cases.Select(c => c.Smiles).ToArray();
        var builder = new DepictionBuilder();
        var tasks = Enumerable.Range(0, 32).Select(i => Task.Run(() =>
        {
            var s = smiles[i % smiles.Length];
            var d = builder.Build(_loader.ParseSmiles(s));
            return (s, d.Atoms.Count);
        }));

        var results = await Task.WhenAll(tasks);

        foreach (var (s, count) in results)
            Assert.Equal(RdkitReference.Cases.First(c => c.Smiles == s).Symbols.Length, count);
    }
}
