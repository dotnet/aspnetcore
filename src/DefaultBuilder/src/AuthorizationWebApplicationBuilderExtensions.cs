// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for setting up authorization services in an <see cref="WebApplicationBuilder" />.
/// </summary>
public static class AuthorizationWebApplicationBuilderExtensions
{
    /// <summary>
    /// Registers services required by authentication services.
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
}
