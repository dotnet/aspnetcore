// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Specifies the endpoints advertised by the well-known passkey endpoints document
/// served at <c>/.well-known/passkey-endpoints</c>.
/// </summary>
/// <remarks>
/// <para>
/// Credential managers fetch this document to discover whether a site supports passkeys and where
/// a user can create or manage them. This allows them to offer to upgrade a saved password to a
/// passkey without the user having to find the relevant page themselves.
/// </para>
/// <para>
/// The document is served once <c>MapWellKnownPasskeyEndpoints</c> has been called on the
/// application. Members left <see langword="null"/> are omitted.
/// </para>
/// <para>
/// Each value may be an absolute URL, which is advertised as it stands apart from being normalized,
/// or a path relative to the application, which is resolved against the current request.
/// </para>
/// <para>
/// See <see href="https://w3c.github.io/webappsec-passkey-endpoints/"/>.
/// </para>
/// </remarks>
/// <example>
/// The following example advertises the passkey management page of an application:
/// <code>
/// builder.Services.AddPasskeyEndpoints(options =>
/// {
///     options.Enroll = "/Account/Manage/Passkeys";
///     options.Manage = "/Account/Manage/Passkeys";
/// });
///
/// var app = builder.Build();
///
/// app.MapWellKnownPasskeyEndpoints();
/// </code>
/// A request to <c>https://contoso.com/.well-known/passkey-endpoints</c> then responds with:
/// <code>
/// {
///   "enroll": "https://contoso.com/Account/Manage/Passkeys",
///   "manage": "https://contoso.com/Account/Manage/Passkeys"
/// }
/// </code>
/// </example>
[Experimental("ASP0039", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public sealed class PasskeyEndpointsOptions
{
    /// <summary>
    /// Gets or sets the URL of the page where a user can create a new passkey.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This can be an absolute URL, or a path relative to the application such as
    /// <c>"/Account/Manage/Passkeys"</c>, which is resolved against the current request.
    /// </para>
    /// <para>
    /// Relative values are resolved against the scheme, host and path base of the request. Whatever
    /// is advertised is normalized, so it may differ from the configured value in casing, escaping
    /// or a default port.
    /// </para>
    /// <para>
    /// If left <see langword="null"/>, the <c>enroll</c> member is omitted from the document.
    /// </para>
    /// </remarks>
    public string? Enroll { get; set; }

    /// <summary>
    /// Gets or sets the URL of the page where a user can manage their existing passkeys.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This can be an absolute URL, or a path relative to the application such as
    /// <c>"/Account/Manage/Passkeys"</c>, which is resolved against the current request.
    /// </para>
    /// <para>
    /// Relative values are resolved against the scheme, host and path base of the request. Whatever
    /// is advertised is normalized, so it may differ from the configured value in casing, escaping
    /// or a default port.
    /// </para>
    /// <para>
    /// If left <see langword="null"/>, the <c>manage</c> member is omitted from the document.
    /// </para>
    /// </remarks>
    public string? Manage { get; set; }

    /// <summary>
    /// Gets or sets the URL of an informational page describing how the application uses the
    /// WebAuthn pseudo-random function (PRF) extension.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Applications that use the PRF extension to sign or encrypt user data should explain what
    /// deleting such a passkey means for that data, because a user may not expect a credential used
    /// to sign in to also protect their content. Credential managers can then warn the user and
    /// link to this page when they delete a passkey.
    /// </para>
    /// <para>
    /// This can be an absolute URL, or a path relative to the application such as
    /// <c>"/Help/Passkeys"</c>, which is resolved against the current request.
    /// </para>
    /// <para>
    /// Relative values are resolved against the scheme, host and path base of the request. Whatever
    /// is advertised is normalized, so it may differ from the configured value in casing, escaping
    /// or a default port.
    /// </para>
    /// <para>
    /// If left <see langword="null"/>, the <c>prfUsageDetails</c> member is omitted from the
    /// document.
    /// </para>
    /// </remarks>
    public string? PrfUsageDetails { get; set; }
}
