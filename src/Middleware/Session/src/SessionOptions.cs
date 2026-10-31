// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Session;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Represents the session state options for the application.
/// </summary>
public class SessionOptions
{
    private CookieBuilder _cookieBuilder = new SessionCookieBuilder();

    /// <summary>
    /// Determines the settings used to create the cookie.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><see cref="CookieBuilder.Name"/> defaults to <see cref="SessionDefaults.CookieName"/>.</description></item>
    /// <item><description><see cref="CookieBuilder.Path"/> defaults to <see cref="SessionDefaults.CookiePath"/>.</description></item>
    /// <item><description><see cref="CookieBuilder.SameSite"/> defaults to <see cref="SameSiteMode.Lax"/>.</description></item>
    /// <item><description><see cref="CookieBuilder.HttpOnly"/> defaults to <c>true</c>.</description></item>
    /// <item><description><see cref="CookieBuilder.IsEssential"/> defaults to <c>false</c>.</description></item>
    /// </list>
    /// </remarks>
    public CookieBuilder Cookie
    {
        get => _cookieBuilder;
        set => _cookieBuilder = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// The IdleTimeout indicates how long the session can be idle before its contents are abandoned. Each session access
    /// resets the timeout. Note this only applies to the content of the session, not the cookie.
    /// </summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(20);

    /// <summary>
    /// The maximum amount of time allowed to load a session from the store or to commit it back to the store.
    /// Note this may only apply to asynchronous operations. This timeout can be disabled using <see cref="Timeout.InfiniteTimeSpan"/>.
    /// </summary>
    public TimeSpan IOTimeout { get; set; } = TimeSpan.FromMinutes(1);

    private sealed class SessionCookieBuilder : CookieBuilder
    {
        public SessionCookieBuilder()
        {
            Name = SessionDefaults.CookieName;
            Path = SessionDefaults.CookiePath;
            SecurePolicy = CookieSecurePolicy.None;
            SameSite = SameSiteMode.Lax;
            HttpOnly = true;
            // Session is considered non-essential as it's designed for ephemeral data.
            IsEssential = false;
        }

        public override TimeSpan? Expiration
        {
            get => null;
            set => throw new InvalidOperationException(nameof(Expiration) + " cannot be set for the cookie defined by " + nameof(SessionOptions));
        }
    }
}
