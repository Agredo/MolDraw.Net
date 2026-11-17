using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace MolDraw.Skia.Tests;

public class ColormapTests
{
    [Fact]
    public void EndpointsAndMidpoint()
    {
        var map = Colormap.Viridis;

        Assert.Equal(new SKColor(0x44, 0x01, 0x54), map.Map(0));
        Assert.Equal(new SKColor(0xFD, 0xE7, 0x25), map.Map(1));
        Assert.Equal(map.Stops[8], map.Map(0.5f));
        Assert.Equal(map.Map(0), map.Map(-3));
        Assert.Equal(map.Map(1), map.Map(42));
        Assert.Equal(map.Map(0), map.Map(float.NaN));
    }

    [Fact]
    public void CustomInterpolatesLinearly()
    {
        var map = Colormap.Custom("bw", SKColors.Black, SKColors.White);

        Assert.Equal(new SKColor(128, 128, 128), map.Map(0.5f));
        Assert.Equal(SKColors.White, map.Reversed().Map(0));
        Assert.Throws<ArgumentException>(() => Colormap.Custom("x", SKColors.Red));
    }

    [Fact]
    public void BuiltInMapsAreDistinct()
    {
        Assert.Equal(5, Colormap.BuiltIn.Select(m => m.Name).Distinct().Count());
        Assert.All(Colormap.BuiltIn, m => Assert.Equal(17, m.Stops.Count));
    }

    [Fact]
    public void HighlightsResolveColours()
    {
        var h = new MoleculeHighlights { AtomWeights = [0.25f, float.NaN], AtomColors = new Dictionary<int, SKColor> { [3] = SKColors.Red } };

        Assert.Equal(Colormap.Viridis.Map(0.25f), h.GetAtomColor(0, 0, out var w));
        Assert.Equal(0.25f, w);
        Assert.Null(h.GetAtomColor(1, 0, out _));
        Assert.Null(h.GetAtomColor(2, 0, out _));
        Assert.Equal(SKColors.Red, h.GetAtomColor(3, 0, out _));
        Assert.Null(h.GetAtomColor(0, 0.5f, out _));
        Assert.True(MoleculeHighlights.None.IsEmpty);
        Assert.False(h.IsEmpty);
    }

    [Fact]
    public void DependencyInjectionRegistersRenderer()
    {
        using var provider = new ServiceCollection().AddMolDrawSkia().BuildServiceProvider();

        Assert.IsType<MoleculeRenderer>(provider.GetRequiredService<IMoleculeRenderer>());
        Assert.NotNull(provider.GetRequiredService<MoleculeImageExporter>());
    }
}
