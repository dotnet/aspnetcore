// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
public struct DefaultKeyResolution
{
    /// <summary>
    /// The default key, may be null if no key is a good default candidate.
    /// </summary>
    /// <remarks>
    /// If this property is non-null, its <see cref="IKey.CreateEncryptor()"/> method will succeed
    /// so is appropriate for use with deferred keys.
    /// </remarks>
    public IKey? DefaultKey;

    /// <summary>
    /// The fallback key, which should be used only if the caller is configured not to
    /// honor the <see cref="ShouldGenerateNewKey"/> property. This property may
    /// be null if there is no viable fallback key.
    /// </summary>
    /// <remarks>
    /// If this property is non-null, its <see cref="IKey.CreateEncryptor()"/> method will succeed
    /// so is appropriate for use with deferred keys.
    /// </remarks>
    public IKey? FallbackKey;

    /// <summary>
    /// True if the caller should generate and persist a new key to the keyring.
    /// False if the caller should determine for itself whether to generate a new key.
    /// This value may be 'true' even if a valid default key was found.
    /// </summary>
    /// <remarks>
    /// Does not reflect the time to expiration of the default key, if there is one.
    /// </remarks>
    public bool ShouldGenerateNewKey;
}
