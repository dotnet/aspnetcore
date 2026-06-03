// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Cookies;

/// <summary>
/// Configuration options for Device Bound Session Credentials (DBSC).
/// DBSC binds session cookies to a device using cryptographic key pairs,
/// preventing session cookie theft and exfiltration.
/// </summary>
/// <example>
/// <code>
/// builder.Services.AddAuthentication()
///     .AddCookie(options =>
///     {
///         options.DeviceBoundSession = new DeviceBoundSessionOptions
///         {
///             Enabled = true,
///             RegistrationPath = new PathString("/.well-known/dbsc/registration"),
///             RefreshPath = new PathString("/.well-known/dbsc/refresh"),
///         };
///     });
/// </code>
/// </example>
public class DeviceBoundSessionOptions
{
    /// <summary>
    /// Gets or sets whether Device Bound Session Credentials are enabled.
    /// Default is <c>false</c>.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the path for the DBSC registration endpoint.
    /// The browser POSTs the public key to this path after receiving the
    /// <c>Secure-Session-Registration</c> header.
    /// </summary>
    /// <remarks>
    /// This path must be same-site with the cookie's domain.
    /// </remarks>
    public PathString RegistrationPath { get; set; } = new PathString("/.well-known/dbsc/registration");

    /// <summary>
    /// Gets or sets the path for the DBSC refresh endpoint.
    /// The browser POSTs to this path when the short-lived cookie expires
    /// to prove possession of the private key.
    /// </summary>
    /// <remarks>
    /// This path must be same-site with the cookie's domain.
    /// </remarks>
    public PathString RefreshPath { get; set; } = new PathString("/.well-known/dbsc/refresh");

    /// <summary>
    /// Gets or sets the expiration time for the short-lived (bound) cookie.
    /// When this cookie expires, the browser must prove possession of the
    /// device-bound private key to obtain a new one.
    /// </summary>
    /// <remarks>
    /// Default is 10 minutes. Shorter values increase security but also increase
    /// refresh frequency (and TPM/network load).
    /// </remarks>
    public TimeSpan ShortLivedCookieExpiration { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets or sets the name of the short-lived (bound) cookie.
    /// This is the cookie that DBSC monitors for expiration.
    /// </summary>
    /// <remarks>
    /// If not set, defaults to the authentication cookie name with a <c>__dbsc</c> suffix.
    /// </remarks>
    public string? ShortLivedCookieName { get; set; }

    /// <summary>
    /// Gets or sets the supported signing algorithms for DBSC key pairs.
    /// Default is ES256 and RS256.
    /// </summary>
    /// <remarks>
    /// The algorithms are listed in order of preference. The browser will select
    /// the first algorithm it supports from this list.
    /// </remarks>
    public IList<string> SupportedAlgorithms { get; set; } = new List<string> { "ES256", "RS256" };

    /// <summary>
    /// Gets or sets whether the session scope should include the entire site
    /// (all subdomains) or just the origin.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the DBSC session applies to all subdomains of the registrable domain.
    /// When <c>false</c> (default), it applies only to the exact origin.
    /// </remarks>
    public bool IncludeSite { get; set; }

    /// <summary>
    /// Gets or sets the scope specifications for the session.
    /// These define include/exclude rules for specific domain/path patterns.
    /// </summary>
    public IList<DeviceBoundSessionScopeRule> ScopeSpecifications { get; set; } = new List<DeviceBoundSessionScopeRule>();

    /// <summary>
    /// Gets or sets the hosts allowed to initiate DBSC refreshes from
    /// cross-origin contexts.
    /// </summary>
    public IList<string> AllowedRefreshInitiators { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the maximum age for a challenge before it is considered stale.
    /// Default is 5 minutes.
    /// </summary>
    public TimeSpan ChallengeMaxAge { get; set; } = TimeSpan.FromMinutes(5);
}
