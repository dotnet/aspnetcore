// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Endpoints;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Default extensions to <see cref="IServiceCollection"/> for <see cref="IdentityEndpointRouteBuilderExtensions.MapIdentity{TUser}(IEndpointRouteBuilder)"/>.
/// </summary>
public static class IdentityEndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds a set of common identity services to the application to support <see cref="IdentityEndpointRouteBuilderExtensions.MapIdentity{TUser}(IEndpointRouteBuilder)"/>
    /// and configures authentication to support identity bearer tokens and cookies.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IdentityBuilder"/>.</returns>
    public static IdentityBuilder AddIdentityEndpoints<TUser>(this IServiceCollection services)
        where TUser : class, new()
        => services.AddIdentityEndpoints<TUser>(_ => { });

    /// <summary>
    /// Adds a set of common identity services to the application to support <see cref="IdentityEndpointRouteBuilderExtensions.MapIdentity{TUser}(IEndpointRouteBuilder)"/>
    /// and configures authentication to support identity bearer tokens and cookies.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configureIdentityOptions">Configures the <see cref="IdentityOptions"/>.</param>
    /// <returns>The <see cref="IdentityBuilder"/>.</returns>
    public static IdentityBuilder AddIdentityEndpoints<TUser>(this IServiceCollection services, Action<IdentityOptions> configureIdentityOptions)
        where TUser : class, new()
        => services.AddIdentityEndpoints<TUser>(configureIdentityOptions, _ => { });

    /// <summary>
    /// Adds a set of common identity services to the application to support <see cref="IdentityEndpointRouteBuilderExtensions.MapIdentity{TUser}(IEndpointRouteBuilder)"/>
    /// and configures authentication to support identity bearer tokens and cookies.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configureIdentityOptions">Configures the <see cref="IdentityOptions"/>.</param>
    /// <param name="configureIdentityBearerOptions">Configures the <see cref="IdentityBearerAuthenticationOptions"/>.</param>
    /// <returns>The <see cref="IdentityBuilder"/>.</returns>
    public static IdentityBuilder AddIdentityEndpoints<TUser>(
        this IServiceCollection services,
        Action<IdentityOptions> configureIdentityOptions,
        Action<IdentityBearerAuthenticationOptions> configureIdentityBearerOptions)
        where TUser : class, new()
    {
        var (identityBuilder, authBuilder) = services.AddIdentityEndpointsCoreInternal<TUser>(configureIdentityOptions, options =>
        {
            options.BearerTokenMissingFallbackScheme = IdentityConstants.ApplicationScheme;
            configureIdentityBearerOptions(options);
        });

        identityBuilder.AddSignInManager();
        authBuilder.AddIdentityCookies();

        return identityBuilder;
    }

    /// <summary>
    /// Adds a set of common identity services to the application to support <see cref="IdentityEndpointRouteBuilderExtensions.MapIdentity{TUser}(IEndpointRouteBuilder)"/>
    /// and configures authentication to support identity bearer tokens but not cookies.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configureIdentityOptions">Configures the <see cref="IdentityOptions"/>.</param>
    /// <param name="configureIdentityBearerOptions">Configures the <see cref="IdentityBearerAuthenticationOptions"/>.</param>
    /// <returns>The <see cref="IdentityBuilder"/>.</returns>
    public static IdentityBuilder AddIdentityEndpointsCore<TUser>(
        this IServiceCollection services,
        Action<IdentityOptions> configureIdentityOptions,
        Action<IdentityBearerAuthenticationOptions> configureIdentityBearerOptions)
        where TUser : class, new()
    {
        var (identityBuilder, _) = services.AddIdentityEndpointsCoreInternal<TUser>(configureIdentityOptions, configureIdentityBearerOptions);
        return identityBuilder;
    }

    private static (IdentityBuilder, AuthenticationBuilder) AddIdentityEndpointsCoreInternal<TUser>(
        this IServiceCollection services,
        Action<IdentityOptions> configureIdentityOptions,
        Action<IdentityBearerAuthenticationOptions> configureIdentityBearerOptions)
        where TUser : class, new()
    {
        ArgumentNullException.ThrowIfNull(nameof(services));
        ArgumentNullException.ThrowIfNull(nameof(configureIdentityOptions));
        ArgumentNullException.ThrowIfNull(nameof(configureIdentityBearerOptions));

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<JsonOptions>, PostConfigureIdentityEndpointJsonOptions>());

        var identityBuilder = services.AddIdentityCore<TUser>(o =>
        {
            o.Stores.MaxLengthForKeys = 128;
            configureIdentityOptions(o);
        }).AddDefaultTokenProviders();

        var authBuilder = services.AddAuthentication(o =>
        {
            o.DefaultScheme = IdentityConstants.BearerScheme;
        })
        .AddScheme<IdentityBearerAuthenticationOptions, IdentityBearerAuthenticationHandler>(IdentityConstants.BearerScheme, configureIdentityBearerOptions);

        return (identityBuilder, authBuilder);
    }
}
