// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Cors;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for configuring CORS using an <see cref="IMvcCoreBuilder"/>.
/// </summary>
public static class MvcCorsMvcCoreBuilderExtensions
{
    /// <summary>
    /// Configures <see cref="IMvcCoreBuilder"/> to use CORS.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    public static IMvcCoreBuilder AddCors(this IMvcCoreBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        AddCorsServices(builder.Services);
        return builder;
    }

    /// <summary>
    /// Configures <see cref="IMvcCoreBuilder"/> to use CORS.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <param name="setupAction">An <see cref="Action{MvcOptions}"/> to configure the provided <see cref="CorsOptions"/>.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    public static IMvcCoreBuilder AddCors(
        this IMvcCoreBuilder builder,
        Action<CorsOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(setupAction);

        AddCorsServices(builder.Services);
        builder.Services.Configure(setupAction);

        return builder;
    }

    /// <summary>
    /// Configures <see cref="CorsOptions"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <param name="setupAction">The configure action.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    public static IMvcCoreBuilder ConfigureCors(
        this IMvcCoreBuilder builder,
        Action<CorsOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(setupAction);

        builder.Services.Configure(setupAction);
        return builder;
    }

    // Internal for testing.
    internal static void AddCorsServices(IServiceCollection services)
    {
        services.AddCors();

        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IApplicationModelProvider, CorsApplicationModelProvider>());
        services.TryAddTransient<CorsAuthorizationFilter, CorsAuthorizationFilter>();
    }
}
