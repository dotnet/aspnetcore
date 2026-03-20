// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Antiforgery.CrossOrigin;
using Microsoft.AspNetCore.Antiforgery.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        services.AddAntiforgery((AntiforgeryOptions _) => { }, (CrossOriginAntiforgeryOptions _) => { });

        return services;
    }

    /// <summary>
    /// Adds antiforgery services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="setupAction">An <see cref="Action{AntiforgeryOptions}"/> to configure the provided <see cref="AntiforgeryOptions"/>.</param>
    /// <param name="crossOriginSetupAction">An <see cref="Action{CrossOriginAntiforgeryOptions}"/> to configure the provided <see cref="CrossOriginAntiforgeryOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddAntiforgery(this IServiceCollection services, Action<AntiforgeryOptions> setupAction, Action<CrossOriginAntiforgeryOptions> crossOriginSetupAction)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(setupAction);
        ArgumentNullException.ThrowIfNull(crossOriginSetupAction);

        services.AddAntiforgery((AntiforgeryOptions _) => { }); // token-based
        services.AddCrossOriginAntiforgery((CrossOriginAntiforgeryOptions _) => { }); // cross-origin

        services.Configure(setupAction);
        services.Configure(crossOriginSetupAction);
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

        services.AddDataProtection();

        // Don't overwrite any options setups that a user may have added.
        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IConfigureOptions<AntiforgeryOptions>, AntiforgeryOptionsSetup>());

        // Token-based antiforgery
        services.TryAddSingleton<IAntiforgery, DefaultAntiforgery>();
        services.TryAddSingleton<IAntiforgeryTokenGenerator, DefaultAntiforgeryTokenGenerator>();
        services.TryAddSingleton<IAntiforgeryTokenSerializer, DefaultAntiforgeryTokenSerializer>();
        services.TryAddSingleton<IAntiforgeryTokenStore, DefaultAntiforgeryTokenStore>();
        services.TryAddSingleton<IClaimUidExtractor, DefaultClaimUidExtractor>();
        services.TryAddSingleton<IAntiforgeryAdditionalDataProvider, DefaultAntiforgeryAdditionalDataProvider>();

        return services;
    }

    /// <summary>
    /// Adds cross-origin antiforgery services to the specified <see cref="IServiceCollection" />.
    /// Only registers cross-origin validation; does not register token-based antiforgery or data protection services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddCrossOriginAntiforgery(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<ICrossOriginAntiforgery, CrossOriginRequestValidator>();

        return services;
    }

    /// <summary>
    /// Adds cross-origin antiforgery services to the specified <see cref="IServiceCollection" />.
    /// Only registers cross-origin validation; does not register token-based antiforgery or data protection services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="setupAction">An <see cref="Action{CrossOriginAntiforgeryOptions}"/> to configure the provided <see cref="CrossOriginAntiforgeryOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddCrossOriginAntiforgery(this IServiceCollection services, Action<CrossOriginAntiforgeryOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(setupAction);

        services.AddCrossOriginAntiforgery();
        services.Configure(setupAction);
        return services;
    }
}
