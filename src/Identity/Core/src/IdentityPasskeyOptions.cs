// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Specifies options for passkey requirements.
/// </summary>
public class IdentityPasskeyOptions
{
    /// <summary>
    /// Gets or sets the time that the browser should wait for the authenticator to provide a passkey.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This option applies to both creating a new passkey and requesting an existing passkey.
    /// This is treated as a hint to the browser, and the browser may choose to ignore it.
    /// </para>
    /// <para>
    /// The default value is 5 minutes.
    /// </para>
    /// <para>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dom-publickeycredentialcreationoptions-timeout"/>
    /// and <see href="https://www.w3.org/TR/webauthn-3/#dom-publickeycredentialrequestoptions-timeout"/>.
    /// </para>
    /// </remarks>
    public TimeSpan AuthenticatorTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the size of the challenge in bytes sent to the client during attestation and assertion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This option applies to both creating a new passkey and requesting an existing passkey.
    /// </para>
    /// <para>
    /// The default value is 32 bytes.
    /// </para>
    /// <para>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dom-publickeycredentialcreationoptions-challenge"/>
    /// and <see href="https://www.w3.org/TR/webauthn-3/#dom-publickeycredentialrequestoptions-challenge"/>.
    /// </para>
    /// </remarks>
    public int ChallengeSize { get; set; } = 32;

    /// <summary>
    /// Gets or sets the effective domain of the server.
    /// This should be unique and will be used as the identity for the server.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This option applies to both creating a new passkey and requesting an existing passkey.
    /// </para>
    /// <para>
    /// If left <see langword="null"/>, the server's origin may be used instead.
    /// </para>
    /// <para>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#rp-id"/>.
    /// </para>
    /// </remarks>
    public string? ServerDomain { get; set; }

    /// <summary>
    /// Gets or sets the user verification requirement.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This option applies to both creating a new passkey and requesting an existing passkey.
    /// </para>
    /// <para>
    /// Possible values are "required", "preferred", and "discouraged".
    /// If set to <see langword="null"/>, the effective value is "preferred".
    /// </para>
    /// <para>
    /// The default value is "required".
    /// </para>
    /// <para>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#enumdef-userverificationrequirement"/>.
    /// </para>
    /// </remarks>
    public string? UserVerificationRequirement { get; set; } = "required";

    /// <summary>
    /// Gets or sets the extent to which the server desires to create a client-side discoverable credential.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This option only applies when creating a new passkey, and is not enforced on the server.
    /// </para>
    /// <para>
    /// Possible values are "discouraged", "preferred", "required", or <see langword="null"/>.
    /// If set to <see langword="null"/>, the effective value is "discouraged".
    /// </para>
    /// <para>
    /// The default value is "preferred".
    /// </para>
    /// <para>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#enumdef-residentkeyrequirement"/>.
    /// </para>
    /// </remarks>
    public string? ResidentKeyRequirement { get; set; } = "preferred";

    /// <summary>
    /// Gets or sets the attestation conveyance preference.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This option only applies when creating a new passkey, and already-registered passkeys are not affected by it.
    /// To validate the attestation statement of a passkey during passkey creation, provide a value for the
    /// <see cref="VerifyAttestationStatement"/> option.
    /// </para>
    /// <para>
    /// Possible values are "none", "indirect", "direct", and "enterprise".
    /// </para>
    /// <para>
    /// If left <see langword="null"/>, the browser defaults to "none".
    /// </para>
    /// <para>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#enumdef-attestationconveyancepreference"/>.
    /// </para>
    /// </remarks>
    public string? AttestationConveyancePreference { get; set; }

    /// <summary>
    /// Gets or sets the allowed authenticator attachment.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This option only applies when creating a new passkey, and already-registered passkeys are not affected by it.
    /// </para>
    /// <para>
    /// Possible values are "platform" and "cross-platform".
    /// </para>
    /// <para>
    /// If left <see langword="null"/>, any authenticator attachment modality is allowed.
    /// </para>
    /// <para>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#enumdef-authenticatorattachment"/>.
    /// </para>
    /// </remarks>
    public string? AuthenticatorAttachment { get; set; }

    /// <summary>
    /// Gets or sets a function that determines whether the given COSE algorithm identifier
    /// is allowed for passkey operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This option only applies when creating a new passkey, and already-registered passkeys are not affected by it.
    /// </para>
    /// <para>
    /// If left <see langword="null"/>, all supported algorithms are allowed.
    /// </para>
    /// <para>
    /// See <see href="https://www.iana.org/assignments/cose/cose.xhtml#algorithms"/>.
    /// </para>
    /// </remarks>
    public Func<int, bool>? IsAllowedAlgorithm { get; set; }

    /// <summary>
    /// Gets or sets a function that validates the origin of the request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This option applies to both creating a new passkey and requesting an existing passkey.
    /// </para>
    /// <para>
    /// If left <see langword="null"/>, cross-origin requests are disallowed, and the request is only
    /// considered valid if the request's origin header matches the credential's origin.
    /// </para>
    /// </remarks>
    public Func<PasskeyOriginValidationContext, ValueTask<bool>>? ValidateOrigin { get; set; }

    /// <summary>
    /// Gets or sets a function that verifies the attestation statement of a passkey.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This option only applies when creating a new passkey, and already-registered passkeys are not affected by it.
    /// </para>
    /// <para>
    /// If left <see langword="null"/>, this function does not perform any verification and always returns <see langword="true"/>.
    /// </para>
    /// </remarks>
    public Func<PasskeyAttestationStatementVerificationContext, ValueTask<bool>>? VerifyAttestationStatement { get; set; }
}
