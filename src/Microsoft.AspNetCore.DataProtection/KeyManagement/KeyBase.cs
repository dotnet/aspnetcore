// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement
{
    /// <summary>
    /// The basic implementation of <see cref="IKey"/>.
    /// </summary>
    internal abstract class KeyBase : IKey
    {
        private readonly Lazy<IAuthenticatedEncryptorDescriptor> _lazyDescriptor;

        public KeyBase(Guid keyId, DateTimeOffset creationDate, DateTimeOffset activationDate, DateTimeOffset expirationDate, Lazy<IAuthenticatedEncryptorDescriptor> lazyDescriptor)
        {
            KeyId = keyId;
            CreationDate = creationDate;
            ActivationDate = activationDate;
            ExpirationDate = expirationDate;
            _lazyDescriptor = lazyDescriptor;
        }

        public DateTimeOffset ActivationDate { get; }

        public DateTimeOffset CreationDate { get; }

        public DateTimeOffset ExpirationDate { get; }

        public bool IsRevoked { get; private set; }

        public Guid KeyId { get; }

        public IAuthenticatedEncryptorDescriptor Descriptor
        {
            get
            {
                return _lazyDescriptor.Value;
            }
        }

        internal void SetRevoked()
        {
            IsRevoked = true;
        }
    }
}
