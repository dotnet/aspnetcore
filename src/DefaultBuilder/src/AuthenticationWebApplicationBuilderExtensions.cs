// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for setting up authentication services in an <see cref="WebApplicationBuilder" />.
/// </summary>
public static class AuthenticationWebApplicationBuilderExtensions
{
    /// <summary>
    /// Registers services required by authentication services.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/>.</param>
    /// <returns>A <see cref="AuthenticationBuilder"/> that can be used to further configure authentication.</returns>
    public static AuthenticationBuilder AddAuthentication(this WebApplicationBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.Services.AddAuthentication();
    }

    /// <summary>
    /// Registers services required by authentication services. <paramref name="defaultScheme"/> specifies the name of the
    /// scheme to use by default when a specific scheme isn't requested.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/>.</param>
    /// <param name="defaultScheme">The default scheme used as a fallback for all other schemes.</param>
    /// <returns>A <see cref="AuthenticationBuilder"/> that can be used to further configure authentication.</returns>
    public static AuthenticationBuilder AddAuthentication(this WebApplicationBuilder builder, string defaultScheme)
        => builder.AddAuthentication(o => o.DefaultScheme = defaultScheme);

    /// <summary>
    /// Registers services required by authentication services and configures <see cref="AuthenticationOptions"/>.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/>.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="AuthenticationOptions"/>.</param>
    /// <returns>A <see cref="AuthenticationBuilder"/> that can be used to further configure authentication.</returns>
    public static AuthenticationBuilder AddAuthentication(this WebApplicationBuilder builder, Action<AuthenticationOptions> configureOptions)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        return builder.AddAuthentication(configureOptions);
    }
}
