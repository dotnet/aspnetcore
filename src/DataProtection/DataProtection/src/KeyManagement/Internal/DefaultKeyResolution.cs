// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement.Internal
{
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
        public IKey DefaultKey;

        /// <summary>
        /// The fallback key, which should be used only if the caller is configured not to
        /// honor the <see cref="ShouldGenerateNewKey"/> property. This property may
        /// be null if there is no viable fallback key.
        /// </summary>
        /// <remarks>
        /// If this property is non-null, its <see cref="IKey.CreateEncryptor()"/> method will succeed
        /// so is appropriate for use with deferred keys.
        /// </remarks>
        public IKey FallbackKey;

        /// <summary>
        /// 'true' if a new key should be persisted to the keyring, 'false' otherwise.
        /// This value may be 'true' even if a valid default key was found.
        /// </summary>
        public bool ShouldGenerateNewKey;
    }
}
