// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    /// The default value is 1 minute.
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dom-publickeycredentialcreationoptions-timeout"/>
    /// and <see href="https://www.w3.org/TR/webauthn-3/#dom-publickeycredentialrequestoptions-timeout"/>.
    /// </remarks>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The size of the challenge in bytes sent to the client during WebAuthn attestation and assertion.
    /// </summary>
    /// <remarks>
    /// The default value is 16 bytes.
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dom-publickeycredentialcreationoptions-challenge"/>
    /// and <see href="https://www.w3.org/TR/webauthn-3/#dom-publickeycredentialrequestoptions-challenge"/>.
    /// </remarks>
    public int ChallengeSize { get; set; } = 16;

    /// <summary>
    /// The effective domain of the server. Should be unique and will be used as the identity for the server.
    /// </summary>
    /// <remarks>
    /// If left <see langword="null"/>, the server's origin may be used instead.
    /// See <see href="https://www.w3.org/TR/webauthn-3/#rp-id"/>.
    /// </remarks>
    public string? ServerDomain { get; set; }

    /// <summary>
    /// Gets or sets the allowed origins for credential registration and assertion.
    /// When specified, these origins are explicitly allowed in addition to any origins allowed by other settings.
    /// </summary>
    public IList<string> AllowedOrigins { get; set; } = [];

    /// <summary>
    /// Gets or sets whether the current server's origin should be allowed for credentials.
    /// When true, the origin of the current request will be automatically allowed.
    /// </summary>
    /// <remarks>
    /// The default value is <see langword="true"/>.
    /// </remarks>
    public bool AllowCurrentOrigin { get; set; } = true;

    /// <summary>
    /// Gets or sets whether credentials from cross-origin iframes should be allowed.
    /// </summary>
    /// <remarks>
    /// The default value is <see langword="false"/>.
    /// </remarks>
    public bool AllowCrossOriginIframes { get; set; }

    /// <summary>
    /// Whether or not to accept a backup eligible credential.
    /// </summary>
    /// <remarks>
    /// The default value is <see cref="CredentialBackupPolicy.Allowed"/>.
    /// </remarks>
    public CredentialBackupPolicy BackupEligibleCredentialPolicy { get; set; } = CredentialBackupPolicy.Allowed;

    /// <summary>
    /// Whether or not to accept a backed up credential.
    /// </summary>
    /// <remarks>
    /// The default value is <see cref="CredentialBackupPolicy.Allowed"/>.
    /// </remarks>
    public CredentialBackupPolicy BackedUpCredentialPolicy { get; set; } = CredentialBackupPolicy.Allowed;

    /// <summary>
    /// Represents the policy for credential backup eligibility and backup status.
    /// </summary>
    public enum CredentialBackupPolicy
    {
        /// <summary>
        /// Indicates that the credential backup eligibility or backup status is required.
        /// </summary>
        Required = 0,

        /// <summary>
        /// Indicates that the credential backup eligibility or backup status is allowed, but not required.
        /// </summary>
        Allowed = 1,

        /// <summary>
        /// Indicates that the credential backup eligibility or backup status is disallowed.
        /// </summary>
        Disallowed = 2,
    }
}
