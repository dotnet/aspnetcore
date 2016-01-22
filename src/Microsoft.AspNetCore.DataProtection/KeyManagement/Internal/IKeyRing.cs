// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement.Internal
{
    /// <summary>
    /// The basic interface for accessing a read-only keyring.
    /// </summary>
    public interface IKeyRing
    {
        /// <summary>
        /// The authenticated encryptor that shall be used for new encryption operations.
        /// </summary>
        /// <remarks>
        /// Activation of the encryptor instance is deferred until first access.
        /// </remarks>
        IAuthenticatedEncryptor DefaultAuthenticatedEncryptor { get; }

        /// <summary>
        /// The id of the key associated with <see cref="DefaultAuthenticatedEncryptor"/>.
        /// </summary>
        Guid DefaultKeyId { get; }

        /// <summary>
        /// Returns an encryptor instance for the given key, or 'null' if the key with the
        /// specified id cannot be found in the keyring.
        /// </summary>
        /// <remarks>
        /// Activation of the encryptor instance is deferred until first access.
        /// </remarks>
        IAuthenticatedEncryptor GetAuthenticatedEncryptorByKeyId(Guid keyId, out bool isRevoked);
    }
}
