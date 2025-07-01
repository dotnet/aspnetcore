// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Specifies options for passkey requirements.
/// </summary>
public class PasskeyOptions
{
    /// <summary>
    /// Gets or sets the time that the server is willing to wait for a passkey operation to complete.
    /// </summary>
    /// <remarks>
    /// The default value is 5 minutes.
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dom-publickeycredentialcreationoptions-timeout"/>
    /// and <see href="https://www.w3.org/TR/webauthn-3/#dom-publickeycredentialrequestoptions-timeout"/>.
    /// </remarks>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The size of the challenge in bytes sent to the client during WebAuthn attestation and assertion.
    /// </summary>
    /// <remarks>
    /// The default value is 32 bytes.
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dom-publickeycredentialcreationoptions-challenge"/>
    /// and <see href="https://www.w3.org/TR/webauthn-3/#dom-publickeycredentialrequestoptions-challenge"/>.
    /// </remarks>
    public int ChallengeSize { get; set; } = 32;

    /// <summary>
    /// The effective domain of the server. Should be unique and will be used as the identity for the server.
    /// </summary>
    /// <remarks>
    /// If left <see langword="null"/>, the server's origin may be used instead.
    /// See <see href="https://www.w3.org/TR/webauthn-3/#rp-id"/>.
    /// </remarks>
    public string? ServerDomain { get; set; }

    /// <summary>
    /// Gets or sets the user verification requirement.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#enumdef-userverificationrequirement"/>.
    /// Possible values are "required", "preferred", and "discouraged".
    /// The default value is "preferred".
    /// </remarks>
    public string? UserVerificationRequirement { get; set; }

    /// <summary>
    /// Gets or sets the extent to which the server desires to create a client-side discoverable credential.
    /// Supported values are "discouraged", "preferred", or "required".
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#enumdef-residentkeyrequirement"/>.
    /// </remarks>
    public string? ResidentKeyRequirement { get; set; }

    /// <summary>
    /// Gets or sets the attestation conveyance preference.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#enumdef-attestationconveyancepreference"/>.
    /// The default value is "none".
    /// </remarks>
    public string? AttestationConveyancePreference { get; set; }

    /// <summary>
    /// Gets or sets the authenticator attachment.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#enumdef-authenticatorattachment"/>.
    /// </remarks>
    public string? AuthenticatorAttachment { get; set; }

    /// <summary>
    /// Gets or sets a function that determines whether the given COSE algorithm identifier
    /// is allowed for passkey operations.
    /// </summary>
    /// <remarks>
    /// If <see langword="null"/> all supported algorithms are allowed.
    /// See <see href="https://www.iana.org/assignments/cose/cose.xhtml#algorithms"/>.
    /// </remarks>
    public Func<int, bool>? IsAllowedAlgorithm { get; set; }

    /// <summary>
    /// Gets or sets a function that validates the origin of the request.
    /// </summary>
    /// <remarks>
    /// By default, this function disallows cross-origin requests and checks
    /// that the request's origin header matches the credential's origin.
    /// </remarks>
    public Func<PasskeyOriginValidationContext, Task<bool>> ValidateOrigin { get; set; } = DefaultValidateOrigin;

    /// <summary>
    /// Gets or sets a function that verifies the attestation statement of a passkey.
    /// </summary>
    /// <remarks>
    /// By default, this function does not perform any verification and always returns <see langword="true"/>.
    /// </remarks>
    public Func<PasskeyAttestationStatementVerificationContext, Task<bool>> VerifyAttestationStatement { get; set; } = DefaultVerifyAttestationStatement;

    private static Task<bool> DefaultValidateOrigin(PasskeyOriginValidationContext context)
    {
        var result = IsValidOrigin();
        return Task.FromResult(result);

        bool IsValidOrigin()
        {
            if (string.IsNullOrEmpty(context.Origin) ||
                context.CrossOrigin ||
                !Uri.TryCreate(context.Origin, UriKind.Absolute, out var originUri))
            {
                return false;
            }

            // Uri.Equals correctly handles string comparands.
            return context.HttpContext.Request.Headers.Origin is [var origin] && originUri.Equals(origin);
        }
    }

    private static Task<bool> DefaultVerifyAttestationStatement(PasskeyAttestationStatementVerificationContext context)
        => Task.FromResult(true);
}
