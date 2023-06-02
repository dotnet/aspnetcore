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

        return builder.AddBearerToken(IdentityConstants.BearerScheme, bearerOptions =>
        {
            bearerOptions.Events.OnSigningIn = HandleSigningIn<TUser>;
            configureOptions(bearerOptions);
        });
    }

    private static async Task HandleSigningIn<TUser>(SigningInContext signInContext)
        where TUser : class, new()
    {
        // Only validate the security stamp and refresh the user from the store during /refresh
        // not during the initial /login when the Principal is already newly created from the store.
        if (signInContext.Properties.RefreshToken is null)
        {
            return;
        }

        var signInManager = signInContext.HttpContext.RequestServices.GetRequiredService<SignInManager<TUser>>();

        // Reject the /refresh attempt if the security stamp validation fails which will result in a 401 challenge.
        if (await signInManager.ValidateSecurityStampAsync(signInContext.Principal) is not TUser user)
        {
            signInContext.Principal = null;
            return;
        }

        signInContext.Principal = await signInManager.CreateUserPrincipalAsync(user);
    }
}
