// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// Options for the Device Bound Session Credentials authentication handler.
/// </summary>
[Experimental("ASP0031", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
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
    /// Gets or sets the nonempty application-local, root-relative request path for the registration endpoint.
    /// The path must begin with <c>/</c> and is relative to the application's base path. Full URLs,
    /// current-directory-relative URL references, network-path references, query strings, and fragments are unsupported.
    /// The effective <see cref="HttpRequest.PathBase"/> is prepended when the path is advertised to the browser.
    /// Although the DBSC protocol permits absolute and cross-origin same-site endpoint references, they are outside
    /// this component's local request-handler model.
    /// Defaults to <c>/.well-known/dbsc/registration</c>.
    /// </summary>
    public PathString RegistrationPath { get; set; } = DeviceBoundSessionDefaults.RegistrationPath;

    /// <summary>
    /// Gets or sets the nonempty application-local, root-relative request path for the refresh endpoint.
    /// The path must begin with <c>/</c> and is relative to the application's base path. Full URLs,
    /// current-directory-relative URL references, network-path references, query strings, and fragments are unsupported.
    /// The effective <see cref="HttpRequest.PathBase"/> is prepended when the path is advertised to the browser.
    /// Although the DBSC protocol permits absolute and cross-origin same-site endpoint references, they are outside
    /// this component's local request-handler model.
    /// Defaults to <c>/.well-known/dbsc/refresh</c>.
    /// </summary>
    public PathString RefreshPath { get; set; } = DeviceBoundSessionDefaults.RefreshPath;

    /// <summary>
    /// Checks that the options are valid for a specific scheme.
    /// </summary>
    /// <param name="scheme">The scheme being validated.</param>
    public override void Validate(string scheme)
    {
        base.Validate(scheme);

        ValidatePath(RegistrationPath, nameof(RegistrationPath), scheme);
        ValidatePath(RefreshPath, nameof(RefreshPath), scheme);

        if (RegistrationPath.Equals(RefreshPath))
        {
            throw CreatePathCollisionException(nameof(RefreshPath), scheme);
        }
    }

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
    /// Gets or sets whether the browser session is scoped to the registration site rather than the registration origin.
    /// <see langword="false"/> creates an origin-scoped browser session. <see langword="true"/> creates a
    /// site-scoped browser session and allows same-site request origins to initiate DBSC refresh by default.
    /// With the current implementation, <see langword="true"/> requires the registration endpoint to be served
    /// from the registrable-domain host because the scope origin is derived from the registration request origin.
    /// Cookie <c>Domain</c> and applicability remain independently configured on the source cookie scheme.
    /// Defaults to <see langword="false"/>.
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

    private static ArgumentException CreatePathCollisionException(string parameterName, string scheme)
        => new(
            $"The {nameof(RefreshPath)} for scheme '{scheme}' must differ from {nameof(RegistrationPath)}.",
            parameterName);

    private static void ValidatePath(PathString path, string parameterName, string scheme)
    {
        if (!path.HasValue)
        {
            throw new ArgumentException(
                $"The {parameterName} for scheme '{scheme}' must be nonempty.",
                parameterName);
        }

        var value = path.Value;
        if (value.StartsWith("//", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"The {parameterName} for scheme '{scheme}' must not be a network-path reference beginning with '//'.",
                parameterName);
        }

        if (value.Contains('?', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"The {parameterName} for scheme '{scheme}' must not contain a query string ('?').",
                parameterName);
        }

        if (value.Contains('#', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"The {parameterName} for scheme '{scheme}' must not contain a fragment ('#').",
                parameterName);
        }
    }
}
