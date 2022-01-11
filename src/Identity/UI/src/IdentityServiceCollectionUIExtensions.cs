// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Identity;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Default UI extensions to <see cref="IServiceCollection"/>.
/// </summary>
public static class IdentityServiceCollectionUIExtensions
{
    /// <summary>
    /// Adds a set of common identity services to the application, including a default UI, token providers,
    /// and configures authentication to use identity cookies.
    /// </summary>
    /// <remarks>
    /// In order to use the default UI, the application must be using <see cref="Microsoft.AspNetCore.Mvc"/>,
    /// <see cref="Microsoft.AspNetCore.StaticFiles"/> and contain a <c>_LoginPartial</c> partial view that
    /// can be found by the application.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IdentityBuilder"/>.</returns>
    public static IdentityBuilder AddDefaultIdentity<TUser>(this IServiceCollection services) where TUser : class
        => services.AddDefaultIdentity<TUser>(_ => { });

    /// <summary>
    /// Adds a set of common identity services to the application, including a default UI, token providers,
    /// and configures authentication to use identity cookies.
    /// </summary>
    /// <remarks>
    /// In order to use the default UI, the application must be using <see cref="Microsoft.AspNetCore.Mvc"/>,
    /// <see cref="Microsoft.AspNetCore.StaticFiles"/> and contain a <c>_LoginPartial</c> partial view that
    /// can be found by the application.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configureOptions">Configures the <see cref="IdentityOptions"/>.</param>
    /// <returns>The <see cref="IdentityBuilder"/>.</returns>
    public static IdentityBuilder AddDefaultIdentity<TUser>(this IServiceCollection services, Action<IdentityOptions> configureOptions) where TUser : class
    {
        services.AddAuthentication(o =>
        {
            o.DefaultScheme = IdentityConstants.ApplicationScheme;
            o.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        })
        .AddIdentityCookies(o => { });

        return services.AddIdentityCore<TUser>(o =>
        {
            o.Stores.MaxLengthForKeys = 128;
            configureOptions?.Invoke(o);
        })
            .AddDefaultUI()
            .AddDefaultTokenProviders();
    }
}
