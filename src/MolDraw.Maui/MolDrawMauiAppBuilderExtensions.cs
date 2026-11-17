using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MolDraw.Skia;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace MolDraw.Maui;

/// <summary>MAUI app builder integration.</summary>
public static class MolDrawMauiAppBuilderExtensions
{
    /// <summary>
    /// Registers SkiaSharp (<c>UseSkiaSharp()</c>) and the rendering services used by <see cref="MoleculeView"/>:
    /// <see cref="IMoleculeRenderer"/> and <see cref="IDepictionCache"/> (singletons, existing registrations are kept).
    /// </summary>
    /// <remarks>
    /// <see cref="MoleculeView.Smiles"/> additionally needs an <c>IChemistryEngine</c>; register one from a chemistry
    /// package, e.g. <c>builder.Services.AddMolDrawCore()</c> from <c>MolDraw.Core</c> (NCDK).
    /// </remarks>
    /// <param name="builder">The app builder.</param>
    /// <param name="cacheCapacity">Capacity of the SMILES depiction cache.</param>
    public static MauiAppBuilder UseMolDraw(this MauiAppBuilder builder, int cacheCapacity = 256)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseSkiaSharp();
        builder.Services.AddMolDrawSkia();
        builder.Services.TryAddSingleton<IDepictionCache>(_ => new DepictionCache(cacheCapacity));
        return builder;
    }
}
