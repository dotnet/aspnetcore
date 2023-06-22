// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Extension methods to enable bearer token authentication for use with identity.
/// </summary>
public static class IdentityAuthenticationBuilderExtensions
{
    /// <summary>
    /// Adds cookie authentication.
    /// </summary>
    /// <param name="builder">The current <see cref="AuthenticationBuilder"/> instance.</param>
    /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
    public static AuthenticationBuilder AddIdentityBearerToken<TUser>(this AuthenticationBuilder builder)
        where TUser : class, new()
        => builder.AddIdentityBearerToken<TUser>(o => { });

    /// <summary>
    /// Adds the cookie authentication needed for sign in manager.
    /// </summary>
    /// <param name="builder">The current <see cref="AuthenticationBuilder"/> instance.</param>
    /// <param name="configureOptions">Action used to configure the bearer token handler.</param>
    /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
    public static AuthenticationBuilder AddIdentityBearerToken<TUser>(this AuthenticationBuilder builder, Action<BearerTokenOptions> configureOptions)
        where TUser : class, new()
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        return builder.AddBearerToken(IdentityConstants.BearerScheme, configureOptions);
    }
}
