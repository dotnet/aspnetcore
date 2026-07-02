// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// Options for the Device Bound Session Credentials authentication handler.
/// </summary>
[Experimental("ASP0030", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public class DeviceBoundSessionOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Gets or sets the authentication scheme used for initial sign-in (the source of the long-lived cookie).
    /// During registration, the handler authenticates against this scheme to read the user's ticket.
    /// </summary>
    public string? RegistrationSourceScheme { get; set; }

    /// <summary>
    /// Gets or sets the authentication scheme for the refresh cookie (path-scoped stash).
    /// During refresh, the handler authenticates against this scheme to retrieve the ticket + public key.
    /// </summary>
    public string? RefreshScheme { get; set; }

    /// <summary>
    /// Gets or sets the authentication scheme used to stamp the short-lived session cookie.
    /// Both registration and refresh call <c>SignInAsync</c> on this scheme.
    /// </summary>
    public string? SessionScheme { get; set; }

    /// <summary>
    /// Gets or sets the path for the registration endpoint.
    /// Defaults to <c>/.well-known/dbsc/registration</c>.
    /// </summary>
    public PathString RegistrationPath { get; set; } = DeviceBoundSessionDefaults.RegistrationPath;

    /// <summary>
    /// Gets or sets the path for the refresh endpoint.
    /// Defaults to <c>/.well-known/dbsc/refresh</c>.
    /// </summary>
    public PathString RefreshPath { get; set; } = DeviceBoundSessionDefaults.RefreshPath;

    /// <summary>
    /// Gets or sets the expiration for the short-lived session cookie.
    /// Defaults to 10 minutes.
    /// </summary>
    public TimeSpan ShortLivedCookieExpiration { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets or sets the maximum age for challenges before they are considered stale.
    /// Defaults to 5 minutes.
    /// </summary>
    public TimeSpan ChallengeMaxAge { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets whether to include the entire site in the session scope.
    /// Defaults to <c>false</c> (origin-only scope).
    /// </summary>
    public bool IncludeSite { get; set; }

    /// <summary>
    /// Gets the list of scope specifications for the session.
    /// </summary>
    public IList<DeviceBoundSessionScopeRule> ScopeSpecifications { get; } = new List<DeviceBoundSessionScopeRule>();

    /// <summary>
    /// Gets the list of allowed refresh initiator host patterns.
    /// </summary>
    public IList<string> AllowedRefreshInitiators { get; } = new List<string>();
}
