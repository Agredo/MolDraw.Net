using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MolDraw.Core.Ncdk;
using MolDraw.Depiction;

namespace MolDraw.Core;

/// <summary>Dependency injection registration for MolDraw.Core (the NCDK chemistry engine).</summary>
public static class MolDrawCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the NCDK engine: <see cref="IMoleculeLoader"/>, <see cref="IDepictionBuilder"/> and
    /// <see cref="IChemistryEngine"/> as singletons (all stateless and thread-safe).
    /// Existing registrations are kept, so an <see cref="IChemistryEngine"/> registered before this call wins.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="loaderOptions">Optional loader options.</param>
    public static IServiceCollection AddMolDrawCore(this IServiceCollection services, MoleculeLoaderOptions? loaderOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IMoleculeLoader>(_ => new MoleculeLoader(loaderOptions));
        services.TryAddSingleton<IDepictionBuilder, DepictionBuilder>();
        services.TryAddSingleton<IChemistryEngine>(sp => new NcdkDepictionEngine(
            sp.GetService<IMoleculeLoader>(),
            sp.GetService<IDepictionBuilder>()));
        return services;
    }
}
