// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up antiforgery services in an <see cref="IServiceCollection" />.
/// </summary>
public static class AntiforgeryServiceCollectionExtensions
{
    /// <summary>
    /// Adds antiforgery services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddAntiforgery(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDataProtection();

        // Don't overwrite any options setups that a user may have added.
        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IConfigureOptions<AntiforgeryOptions>, AntiforgeryOptionsSetup>());

        services.TryAddSingleton<IAntiforgery, DefaultAntiforgery>();
        services.TryAddSingleton<IAntiforgeryTokenGenerator, DefaultAntiforgeryTokenGenerator>();
        services.TryAddSingleton<IAntiforgeryTokenSerializer, DefaultAntiforgeryTokenSerializer>();
        services.TryAddSingleton<IAntiforgeryTokenStore, DefaultAntiforgeryTokenStore>();
        services.TryAddSingleton<IClaimUidExtractor, DefaultClaimUidExtractor>();
        services.TryAddSingleton<IAntiforgeryAdditionalDataProvider, DefaultAntiforgeryAdditionalDataProvider>();
        services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

        services.TryAddSingleton<ObjectPool<AntiforgerySerializationContext>>(serviceProvider =>
        {
            var provider = serviceProvider.GetRequiredService<ObjectPoolProvider>();
            var policy = new AntiforgerySerializationContextPooledObjectPolicy();
            return provider.Create(policy);
        });

        return services;
    }

    /// <summary>
    /// Adds antiforgery services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="setupAction">An <see cref="Action{AntiforgeryOptions}"/> to configure the provided <see cref="AntiforgeryOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddAntiforgery(this IServiceCollection services, Action<AntiforgeryOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(setupAction);

        services.AddAntiforgery();
        services.Configure(setupAction);
        return services;
    }
}
