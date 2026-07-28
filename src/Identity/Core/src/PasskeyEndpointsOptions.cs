// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
/// The document is served automatically once <c>AddPasskeyEndpoints</c> has been called on the
/// service collection and at least one endpoint has been configured.
/// </para>
/// <para>
/// Each value may be an absolute URL, which is advertised unchanged, or a path relative to the
/// application, which is resolved against the current request.
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
/// </code>
/// A request to <c>https://contoso.com/.well-known/passkey-endpoints</c> then responds with:
/// <code>
/// {
///   "enroll": "https://contoso.com/Account/Manage/Passkeys",
///   "manage": "https://contoso.com/Account/Manage/Passkeys"
/// }
/// </code>
/// </example>
public class PasskeyEndpointsOptions
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
    /// Relative values are resolved against the scheme and host of the request, together with any
    /// path base supplied by the server, such as an IIS virtual directory. A path base established
    /// in the middleware pipeline by <c>UsePathBase</c> is not visible when this document is
    /// served, because the document is requested at the root of the origin. Applications that use
    /// <c>UsePathBase</c> must therefore supply an absolute URL.
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
    /// Relative values are resolved against the scheme and host of the request, together with any
    /// path base supplied by the server, such as an IIS virtual directory. A path base established
    /// in the middleware pipeline by <c>UsePathBase</c> is not visible when this document is
    /// served, because the document is requested at the root of the origin. Applications that use
    /// <c>UsePathBase</c> must therefore supply an absolute URL.
    /// </para>
    /// <para>
    /// If left <see langword="null"/>, the <c>manage</c> member is omitted from the document.
    /// </para>
    /// </remarks>
    public string? Manage { get; set; }
}
