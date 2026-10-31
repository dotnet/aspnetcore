// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Adds support for SignOutAsync
/// </summary>
public abstract class SignOutAuthenticationHandler<TOptions> : AuthenticationHandler<TOptions>, IAuthenticationSignOutHandler
    where TOptions : AuthenticationSchemeOptions, new()
{
    /// <summary>
    /// Initializes a new instance of <see cref="SignOutAuthenticationHandler{TOptions}"/>.
    /// </summary>
    /// <param name="options">The monitor for the options instance.</param>
    /// <param name="logger">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="encoder">The <see cref="UrlEncoder"/>.</param>
    /// <param name="clock">The <see cref="ISystemClock"/>.</param>
    [Obsolete("ISystemClock is obsolete, use TimeProvider on AuthenticationSchemeOptions instead.")]
    public SignOutAuthenticationHandler(IOptionsMonitor<TOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
    { }

    /// <summary>
    /// Initializes a new instance of <see cref="SignOutAuthenticationHandler{TOptions}"/>.
    /// </summary>
    /// <param name="options">The monitor for the options instance.</param>
    /// <param name="logger">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="encoder">The <see cref="UrlEncoder"/>.</param>
    public SignOutAuthenticationHandler(IOptionsMonitor<TOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
    { }

    /// <inheritdoc/>
    public virtual Task SignOutAsync(AuthenticationProperties? properties)
    {
        var target = ResolveTarget(Options.ForwardSignOut);
        return (target != null)
            ? Context.SignOutAsync(target, properties)
            : HandleSignOutAsync(properties ?? new AuthenticationProperties());
    }

    /// <summary>
    /// Override this method to handle SignOut.
    /// </summary>
    /// <param name="properties"></param>
    protected abstract Task HandleSignOutAsync(AuthenticationProperties? properties);
}
