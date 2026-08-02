// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// An interface that can provide data protection services for data which has been persisted
/// to long-term storage.
/// </summary>
public interface IPersistedDataProtector : IDataProtector
{
    /// <summary>
    /// Cryptographically unprotects a piece of data, optionally ignoring failures due to
    /// revocation of the cryptographic keys used to protect the payload.
    /// </summary>
    /// <param name="protectedData">The protected data to unprotect.</param>
    /// <param name="ignoreRevocationErrors">'true' if the payload should be unprotected even
    /// if the cryptographic key used to protect it has been revoked (due to potential compromise),
    /// 'false' if revocation should fail the unprotect operation.</param>
    /// <param name="requiresMigration">'true' if the data should be reprotected before being
    /// persisted back to long-term storage, 'false' otherwise. Migration might be requested
    /// when the default protection key has changed, for instance.</param>
    /// <param name="wasRevoked">'true' if the cryptographic key used to protect this payload
    /// has been revoked, 'false' otherwise. Payloads whose keys have been revoked should be
    /// treated as suspect unless the application has separate assurance that the payload
    /// has not been tampered with.</param>
    /// <returns>The plaintext form of the protected data.</returns>
    /// <remarks>
    /// Implementations should throw CryptographicException if the protected data is
    /// invalid or malformed.
    /// </remarks>
    byte[] DangerousUnprotect(byte[] protectedData, bool ignoreRevocationErrors, out bool requiresMigration, out bool wasRevoked);
}
