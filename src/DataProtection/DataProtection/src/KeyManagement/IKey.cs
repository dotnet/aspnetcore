// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

/// <summary>
/// The basic interface for representing an authenticated encryption key.
/// </summary>
public interface IKey
{
    /// <summary>
    /// The date at which encryptions with this key can begin taking place.
    /// </summary>
    DateTimeOffset ActivationDate { get; }

    /// <summary>
    /// The date on which this key was created.
    /// </summary>
    DateTimeOffset CreationDate { get; }

    /// <summary>
    /// The date after which encryptions with this key may no longer take place.
    /// </summary>
    /// <remarks>
    /// An expired key may still be used to decrypt existing payloads.
    /// </remarks>
    DateTimeOffset ExpirationDate { get; }

    /// <summary>
    /// Returns a value stating whether this key was revoked.
    /// </summary>
    /// <remarks>
    /// A revoked key may still be used to decrypt existing payloads, but the payloads
    /// must be treated as tampered unless the application has some other assurance
    /// that the payloads are authentic.
    /// </remarks>
    bool IsRevoked { get; }

    /// <summary>
    /// The id of the key.
    /// </summary>
    Guid KeyId { get; }

    /// <summary>
    /// Gets the <see cref="IAuthenticatedEncryptorDescriptor"/> instance associated with this key.
    /// </summary>
    IAuthenticatedEncryptorDescriptor Descriptor { get; }

    /// <summary>
    /// Creates an <see cref="IAuthenticatedEncryptor"/> instance that can be used to encrypt data
    /// to and decrypt data from this key.
    /// </summary>
    /// <returns>An <see cref="IAuthenticatedEncryptor"/>.</returns>
    IAuthenticatedEncryptor? CreateEncryptor();
}
