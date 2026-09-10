// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents flags for <see cref="AuthenticatorData"/>.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#authenticator-data"/>.
/// </remarks>
[Flags]
internal enum AuthenticatorDataFlags : byte
{
    /// <summary>
    /// Indicates that the user is present.
    /// </summary>
    UserPresent = 1 << 0,

    /// <summary>
    /// Indicates that the user is verified.
    /// </summary>
    UserVerified = 1 << 2,

    /// <summary>
    /// Indicates that the public key credential source is backup eligible.
    /// </summary>
    BackupEligible = 1 << 3,

    /// <summary>
    /// Indicates that the public key credential source is currently backed up.
    /// </summary>
    BackedUp = 1 << 4,

    /// <summary>
    /// Indicates that the authenticator added attested credential data.
    /// </summary>
    HasAttestedCredentialData = 1 << 6,

    /// <summary>
    /// Indicates that the authenticator data has extensions.
    /// </summary>
    HasExtensionData = 1 << 7,
}
