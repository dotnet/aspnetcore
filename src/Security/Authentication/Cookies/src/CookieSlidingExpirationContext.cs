// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Cookies;

/// <summary>
/// Context object passed to the CookieAuthenticationEvents OnCheckSlidingExpiration method.
/// </summary>
public class CookieSlidingExpirationContext : PrincipalContext<CookieAuthenticationOptions>
{
    /// <summary>
    /// Creates a new instance of the context object.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="scheme"></param>
    /// <param name="ticket">Contains the initial values for identity and extra data</param>
    /// <param name="elapsedTime"></param>
    /// <param name="remainingTime"></param>
    /// <param name="options"></param>
    public CookieSlidingExpirationContext(HttpContext context, AuthenticationScheme scheme, CookieAuthenticationOptions options,
        AuthenticationTicket ticket, TimeSpan elapsedTime, TimeSpan remainingTime)
        : base(context, scheme, options, ticket?.Properties)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        Principal = ticket.Principal;
        ElapsedTime = elapsedTime;
        RemainingTime = remainingTime;
    }

    /// <summary>
    /// The amount of time that has elapsed since the cookie was issued or renewed.
    /// </summary>
    public TimeSpan ElapsedTime { get; }

    /// <summary>
    /// The amount of time left until the cookie expires.
    /// </summary>
    public TimeSpan RemainingTime { get; }

    /// <summary>
    /// If true, the cookie will be renewed. The initial value will be true if the elapsed time
    /// is greater than the remaining time (e.g. more than 50% expired).
    /// </summary>
    public bool ShouldRenew { get; set; }
}
