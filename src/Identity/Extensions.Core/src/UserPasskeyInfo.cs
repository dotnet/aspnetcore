// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Provides information for a user's passkey credential.
/// </summary>
public class UserPasskeyInfo
{
    /// <summary>
    /// Initializes a new instance of <see cref="UserPasskeyInfo"/>.
    /// </summary>
    /// <param name="credentialId">The credential ID for the passkey.</param>
    /// <param name="publicKey">The public key for the passkey.</param>
    /// <param name="name">The friendly name for the passkey.</param>
    /// <param name="createdAt">The time when the passkey was created.</param>
    /// <param name="signCount">The signature counter for the passkey.</param>
    /// <param name="attestationObject">The passkey's attestation object.</param>
    /// <param name="clientDataJson">The passkey's client data JSON.</param>
    /// <param name="transports">The transports supported by this passkey.</param>
    /// <param name="isUserVerified">Indicates if the passkey has a verified user.</param>
    /// <param name="isBackupEligible">Indicates if the passkey is eligible for backup.</param>
    /// <param name="isBackedUp">Indicates if the passkey is currently backed up.</param>
    public UserPasskeyInfo(
        byte[] credentialId,
        byte[] publicKey,
        string? name,
        DateTimeOffset createdAt,
        uint signCount,
        string[]? transports,
        bool isUserVerified,
        bool isBackupEligible,
        bool isBackedUp,
        byte[] attestationObject,
        byte[] clientDataJson)
    {
        CredentialId = credentialId;
        PublicKey = publicKey;
        Name = name;
        CreatedAt = createdAt;
        SignCount = signCount;
        Transports = transports;
        IsUserVerified = isUserVerified;
        IsBackupEligible = isBackupEligible;
        IsBackedUp = isBackedUp;
        AttestationObject = attestationObject;
        ClientDataJson = clientDataJson;
    }

    /// <summary>
    /// Gets the credential ID for this passkey.
    /// </summary>
    public byte[] CredentialId { get; }

    /// <summary>
    /// Gets the public key associated with this passkey.
    /// </summary>
    public byte[] PublicKey { get; }

    /// <summary>
    /// Gets or sets the friendly name for this passkey.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets the time this passkey was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets or sets the signature counter for this passkey.
    /// </summary>
    public uint SignCount { get; set; }

    /// <summary>
    /// Gets the transports supported by this passkey.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#enumdef-authenticatortransport"/>.
    /// </remarks>
    public string[]? Transports { get; }

    /// <summary>
    /// Gets or sets whether the passkey has a verified user.
    /// </summary>
    public bool IsUserVerified { get; set; }

    /// <summary>
    /// Gets whether the passkey is eligible for backup.
    /// </summary>
    public bool IsBackupEligible { get; }

    /// <summary>
    /// Gets or sets whether the passkey is currently backed up.
    /// </summary>
    public bool IsBackedUp { get; set; }

    /// <summary>
    /// Gets the attestation object associated with this passkey.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#attestation-object"/>.
    /// </remarks>
    public byte[] AttestationObject { get; }

    /// <summary>
    /// Gets the collected client data JSON associated with this passkey.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-collectedclientdata"/>.
    /// </remarks>
    public byte[] ClientDataJson { get; }
}
