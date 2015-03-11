// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel;

namespace Microsoft.AspNet.DataProtection.KeyManagement
{
    /// <summary>
    /// The basic implementation of <see cref="IKey"/>.
    /// </summary>
    internal sealed class Key : IKey
    {
        private readonly IAuthenticatedEncryptorDescriptor _descriptor;

        public Key(Guid keyId, DateTimeOffset creationDate, DateTimeOffset activationDate, DateTimeOffset expirationDate, IAuthenticatedEncryptorDescriptor descriptor)
        {
            KeyId = keyId;
            CreationDate = creationDate;
            ActivationDate = activationDate;
            ExpirationDate = expirationDate;

            _descriptor = descriptor;
        }

        public DateTimeOffset ActivationDate { get; }

        public DateTimeOffset CreationDate { get; }

        public DateTimeOffset ExpirationDate { get; }

        public bool IsRevoked { get; private set; }

        public Guid KeyId { get; }

        public IAuthenticatedEncryptor CreateEncryptorInstance()
        {
            return _descriptor.CreateEncryptorInstance();
        }

        internal void SetRevoked()
        {
            IsRevoked = true;
        }
    }
}
