// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Dbsc;

/// <summary>
/// Options for the Device Bound Session Credentials authentication handler.
/// </summary>
/// <remarks>
/// <para><see cref="AuthenticationSchemeOptions.TimeProvider"/> controls DBSC challenge and session-cookie timestamps.</para>
/// <para><see cref="Events"/> configures callbacks for DBSC processing.</para>
/// <para><see cref="AuthenticationSchemeOptions.EventsType"/> can resolve a <see cref="DbscEvents"/> instance from dependency injection per request.</para>
/// <para><see cref="AuthenticationSchemeOptions.ForwardAuthenticate"/> retains the base authentication forwarding behavior.</para>
/// <para><see cref="AuthenticationSchemeOptions.ForwardChallenge"/> retains the base challenge forwarding behavior.</para>
/// <para><see cref="AuthenticationSchemeOptions.ForwardForbid"/> retains the base forbid forwarding behavior.</para>
/// <para><see cref="AuthenticationSchemeOptions.ForwardSignIn"/> is not dispatched because the handler does not support sign-in.</para>
/// <para><see cref="AuthenticationSchemeOptions.ForwardSignOut"/> is not dispatched because the handler does not support sign-out.</para>
/// <para><see cref="AuthenticationSchemeOptions.ClaimsIssuer"/> is not applicable because DBSC copies principals rather than issuing claims.</para>
/// </remarks>
[Experimental("ASP0031", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public class DbscOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Creates an instance of the options initialized with the default values.
    /// </summary>
    public DbscOptions()
    {
        Events = new DbscEvents();
    }

    /// <summary>
    /// Gets or sets the events invoked by the DBSC authentication handler.
    /// </summary>
    public new DbscEvents Events
    {
        get => (DbscEvents)base.Events!;
        set => base.Events = value;
    }

    /// <summary>
    /// Gets or sets the source authentication scheme that issues the long-lived cookie protected by DBSC.
    /// </summary>
    public string SourceScheme { get; set; } = CookieAuthenticationDefaults.AuthenticationScheme;

    internal string? RefreshScheme { get; set; }

    internal string? SessionScheme { get; set; }

    /// <summary>
    /// Gets or sets the nonempty application-local, root-relative request path for the registration endpoint.
    /// The path must begin with <c>/</c> and is relative to the application's base path. Full URLs,
    /// current-directory-relative URL references, network-path references, query strings, and fragments are unsupported.
    /// The effective <see cref="HttpRequest.PathBase"/> is prepended when the path is advertised to the browser.
    /// Although the DBSC protocol permits absolute and cross-origin same-site endpoint references, they are outside
    /// this component's local request-handler model.
    /// Defaults to <c>/.well-known/dbsc/registration</c>.
    /// </summary>
    public PathString RegistrationPath { get; set; } = DbscDefaults.RegistrationPath;

    /// <summary>
    /// Gets or sets the nonempty application-local, root-relative request path for the refresh endpoint.
    /// The path must begin with <c>/</c> and is relative to the application's base path. Full URLs,
    /// current-directory-relative URL references, network-path references, query strings, and fragments are unsupported.
    /// The effective <see cref="HttpRequest.PathBase"/> is prepended when the path is advertised to the browser.
    /// Although the DBSC protocol permits absolute and cross-origin same-site endpoint references, they are outside
    /// this component's local request-handler model.
    /// Defaults to <c>/.well-known/dbsc/refresh</c>.
    /// </summary>
    public PathString RefreshPath { get; set; } = DbscDefaults.RefreshPath;

    /// <summary>
    /// Checks that the options are valid for a specific scheme.
    /// </summary>
    /// <param name="scheme">The scheme being validated.</param>
    public override void Validate(string scheme)
    {
        base.Validate(scheme);

        if (string.IsNullOrEmpty(SourceScheme))
        {
            throw CreateValidationException(
                nameof(SourceScheme),
                $"The {nameof(SourceScheme)} for scheme '{scheme}' must be nonempty.");
        }

        if (string.Equals(SourceScheme, scheme, StringComparison.Ordinal))
        {
            throw CreateValidationException(
                nameof(SourceScheme),
                $"The {nameof(SourceScheme)} for scheme '{scheme}' must differ from the DBSC scheme itself.");
        }

        if (string.Equals(SourceScheme, RefreshScheme, StringComparison.Ordinal))
        {
            throw CreateValidationException(
                nameof(SourceScheme),
                $"The {nameof(SourceScheme)} for scheme '{scheme}' must differ from {nameof(RefreshScheme)}.");
        }

        if (string.Equals(SourceScheme, SessionScheme, StringComparison.Ordinal))
        {
            throw CreateValidationException(
                nameof(SourceScheme),
                $"The {nameof(SourceScheme)} for scheme '{scheme}' must differ from {nameof(SessionScheme)}.");
        }

        if (ShortLivedCookieExpiration <= TimeSpan.Zero)
        {
            throw CreateValidationException(
                nameof(ShortLivedCookieExpiration),
                $"The {nameof(ShortLivedCookieExpiration)} for scheme '{scheme}' must be positive.");
        }

        if (ChallengeMaxAge <= TimeSpan.Zero)
        {
            throw CreateValidationException(
                nameof(ChallengeMaxAge),
                $"The {nameof(ChallengeMaxAge)} for scheme '{scheme}' must be positive.");
        }

        for (var index = 0; index < ScopeSpecifications.Count; index++)
        {
            var scopeSpecification = ScopeSpecifications[index];
            ValidateScopeSpecificationMember(scopeSpecification.Type, nameof(DbscScopeRule.Type), index, scheme);
            ValidateScopeSpecificationMember(scopeSpecification.Domain, nameof(DbscScopeRule.Domain), index, scheme);
            ValidateScopeSpecificationMember(scopeSpecification.Path, nameof(DbscScopeRule.Path), index, scheme);
        }

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
    public IList<DbscScopeRule> ScopeSpecifications { get; } = new List<DbscScopeRule>();

    /// <summary>
    /// Gets the list of allowed refresh initiator host patterns.
    /// </summary>
    public IList<string> AllowedRefreshInitiators { get; } = new List<string>();

    private static ArgumentException CreatePathCollisionException(string parameterName, string scheme)
        => new(
            $"The {nameof(RefreshPath)} for scheme '{scheme}' must differ from {nameof(RegistrationPath)}.",
            parameterName);

    private static ArgumentException CreateValidationException(string parameterName, string message)
        => new(message, parameterName);

    private static void ValidateScopeSpecificationMember(string value, string parameterName, int index, string scheme)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException(
                $"The {parameterName} for scope specification at index {index} for scheme '{scheme}' must be nonempty.",
                parameterName);
        }
    }

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
