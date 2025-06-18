// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Used to specify requirements regarding authenticator attributes.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-authenticatorselectioncriteria"/>.
/// </remarks>
public sealed class AuthenticatorSelectionCriteria
{
    /// <summary>
    /// Gets or sets the authenticator attachment.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dom-authenticatorselectioncriteria-authenticatorattachment"/>.
    /// </remarks>
    public string? AuthenticatorAttachment { get; set; }

    /// <summary>
    /// Gets or sets the extent to which the server desires to create a client-side discoverable credential.
    /// Supported values are "discouraged", "preferred", or "required".
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dom-authenticatorselectioncriteria-residentkey"/>
    /// </remarks>
    public string? ResidentKey { get; set; }

    /// <summary>
    /// Gets whether a resident key is required.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dom-authenticatorselectioncriteria-requireresidentkey"/>.
    /// </remarks>
    public bool RequireResidentKey => string.Equals("required", ResidentKey, StringComparison.Ordinal);

    /// <summary>
    /// Gets or sets the user verification requirement.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dom-authenticatorselectioncriteria-userverification"/>.
    /// </remarks>
    public string UserVerification { get; set; } = "preferred";
}
