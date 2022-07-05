// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for setting up authorization services in a <see cref="WebApplicationBuilder" />.
/// </summary>
public static class AuthorizationWebApplicationBuilderExtensions
{
    /// <summary>
    /// Adds authorization services to the specified <see cref="WebApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/>.</param>
    /// <returns>A <see cref="AuthorizationBuilder"/> that can be used to further configure authentication.</returns>
    public static AuthorizationBuilder AddAuthorization(this WebApplicationBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.Services.AddAuthorizationBuilder();
    }

    /// <summary>
    /// Adds authorization services to the specified <see cref="WebApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/>.</param>
    /// <param name="configure">An action delegate to configure the provided <see cref="AuthorizationOptions"/>.</param>
    /// <returns>A <see cref="AuthorizationBuilder"/> that can be used to further configure authentication.</returns>
    public static AuthorizationBuilder AddAuthorization(this WebApplicationBuilder builder, Action<AuthorizationOptions> configure)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return new AuthorizationBuilder(builder.Services.AddAuthorization(configure));
    }
}
