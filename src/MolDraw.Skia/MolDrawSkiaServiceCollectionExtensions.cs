using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MolDraw.Skia;

/// <summary>Dependency injection registration for MolDraw.Skia.</summary>
public static class MolDrawSkiaServiceCollectionExtensions
{
    /// <summary>Registers <see cref="IMoleculeRenderer"/> and <see cref="MoleculeImageExporter"/> as singletons (thread-safe).</summary>
    /// <param name="services">Service collection.</param>
    public static IServiceCollection AddMolDrawSkia(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IMoleculeRenderer, MoleculeRenderer>();
        services.TryAddSingleton(sp => new MoleculeImageExporter(sp.GetRequiredService<IMoleculeRenderer>()));
        return services;
    }
}
