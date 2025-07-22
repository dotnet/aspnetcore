// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents data associated with a passkey.
/// </summary>
public class IdentityPasskeyData
{
    /// <summary>
    /// Gets or sets the public key associated with this passkey.
    /// </summary>
    public virtual byte[] PublicKey { get; set; } = default!;

    /// <summary>
    /// Gets or sets the friendly name for this passkey.
    /// </summary>
    public virtual string? Name { get; set; }

    /// <summary>
    /// Gets or sets the time this passkey was created.
    /// </summary>
    public virtual DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the signature counter for this passkey.
    /// </summary>
    public virtual uint SignCount { get; set; }

    /// <summary>
    /// Gets or sets the transports supported by this passkey.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#enumdef-authenticatortransport"/>.
    /// </remarks>
    public virtual string[]? Transports { get; set; }

    /// <summary>
    /// Gets or sets whether the passkey has a verified user.
    /// </summary>
    public virtual bool IsUserVerified { get; set; }

    /// <summary>
    /// Gets or sets whether the passkey is eligible for backup.
    /// </summary>
    public virtual bool IsBackupEligible { get; set; }

    /// <summary>
    /// Gets or sets whether the passkey is currently backed up.
    /// </summary>
    public virtual bool IsBackedUp { get; set; }

    /// <summary>
    /// Gets or sets the attestation object associated with this passkey.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#attestation-object"/>.
    /// </remarks>
    public virtual byte[] AttestationObject { get; set; } = default!;

    /// <summary>
    /// Gets or sets the collected client data JSON associated with this passkey.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-collectedclientdata"/>.
    /// </remarks>
    public virtual byte[] ClientDataJson { get; set; } = default!;
}
