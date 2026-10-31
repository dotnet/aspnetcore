// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents a passkey credential for a user in the identity system.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#credential-record"/>.
/// </remarks>
/// <typeparam name="TKey">The type used for the primary key for this passkey credential.</typeparam>
public class IdentityUserPasskey<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets the primary key of the user that owns this passkey.
    /// </summary>
    public virtual TKey UserId { get; set; } = default!;

    /// <summary>
    /// Gets or sets the credential ID for this passkey.
    /// </summary>
    public virtual byte[] CredentialId { get; set; } = default!;

    /// <summary>
    /// Gets or sets additional data associated with this passkey.
    /// </summary>
    public virtual IdentityPasskeyData Data { get; set; } = default!;
}
