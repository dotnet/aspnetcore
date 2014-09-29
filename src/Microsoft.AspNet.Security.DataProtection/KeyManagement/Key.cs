// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Security.DataProtection.AuthenticatedEncryption;

namespace Microsoft.AspNet.Security.DataProtection.KeyManagement
{
    internal sealed class Key : IKey
    {
        private readonly IAuthenticatedEncryptorConfiguration _encryptorConfiguration;

        public Key(Guid keyId, DateTimeOffset creationDate, DateTimeOffset activationDate, DateTimeOffset expirationDate, IAuthenticatedEncryptorConfiguration encryptorConfiguration)
        {
            KeyId = keyId;
            CreationDate = creationDate;
            ActivationDate = activationDate;
            ExpirationDate = expirationDate;

            _encryptorConfiguration = encryptorConfiguration;
        }

        public DateTimeOffset ActivationDate
        {
            get;
            private set;
        }

        public DateTimeOffset CreationDate
        {
            get;
            private set;
        }

        public DateTimeOffset ExpirationDate
        {
            get;
            private set;
        }

        public bool IsRevoked
        {
            get;
            private set;
        }

        public Guid KeyId
        {
            get;
            private set;
        }

        public IAuthenticatedEncryptor CreateEncryptorInstance()
        {
            return _encryptorConfiguration.CreateEncryptorInstance();
        }

        internal void SetRevoked()
        {
            IsRevoked = true;
        }
    }
}
