// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Adds support for SignInAsync
/// </summary>
public abstract class SignInAuthenticationHandler<TOptions> : SignOutAuthenticationHandler<TOptions>, IAuthenticationSignInHandler
    where TOptions : AuthenticationSchemeOptions, new()
{
    /// <summary>
    /// Initializes a new instance of <see cref="SignInAuthenticationHandler{TOptions}"/>.
    /// </summary>
    /// <param name="options">The monitor for the options instance.</param>
    /// <param name="logger">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="encoder">The <see cref="UrlEncoder"/>.</param>
    /// <param name="clock">The <see cref="ISystemClock"/>.</param>
    public SignInAuthenticationHandler(IOptionsMonitor<TOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
    { }

    /// <inheritdoc/>
    public virtual Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties? properties)
    {
        var target = ResolveTarget(Options.ForwardSignIn);
        return (target != null)
            ? Context.SignInAsync(target, user, properties)
            : HandleSignInAsync(user, properties ?? new AuthenticationProperties());
    }

    /// <summary>
    /// Override this method to handle SignIn.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="properties"></param>
    /// <returns>A Task.</returns>
    protected abstract Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties? properties);
}
