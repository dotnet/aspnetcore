// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNet.Security.DataProtection.AuthenticatedEncryption;

namespace Microsoft.AspNet.Security.DataProtection.KeyManagement
{
    internal sealed class KeyRing : IKeyRing
    {
        private readonly AuthenticatedEncryptorHolder _defaultEncryptorHolder;
        private readonly Dictionary<Guid, AuthenticatedEncryptorHolder> _keyToEncryptorMap;

        public KeyRing(Guid defaultKeyId, IKey[] keys)
        {
            DefaultKeyId = defaultKeyId;
            _keyToEncryptorMap = CreateEncryptorMap(defaultKeyId, keys, out _defaultEncryptorHolder);
        }

        public KeyRing(Guid defaultKeyId, KeyRing other)
        {
            DefaultKeyId = defaultKeyId;
            _keyToEncryptorMap = other._keyToEncryptorMap;
            _defaultEncryptorHolder = _keyToEncryptorMap[defaultKeyId];
        }

        public IAuthenticatedEncryptor DefaultAuthenticatedEncryptor
        {
            get
            {
                bool unused;
                return _defaultEncryptorHolder.GetEncryptorInstance(out unused);
            }
        }

        public Guid DefaultKeyId { get; private set; }

        private static Dictionary<Guid, AuthenticatedEncryptorHolder> CreateEncryptorMap(Guid defaultKeyId, IKey[] keys, out AuthenticatedEncryptorHolder defaultEncryptorHolder)
        {
            defaultEncryptorHolder = null;

            var encryptorMap = new Dictionary<Guid, AuthenticatedEncryptorHolder>(keys.Length);
            foreach (var key in keys)
            {
                var holder = new AuthenticatedEncryptorHolder(key);
                encryptorMap.Add(key.KeyId, holder);
                if (key.KeyId == defaultKeyId)
                {
                    defaultEncryptorHolder = holder;
                }
            }
            return encryptorMap;
        }

        public IAuthenticatedEncryptor GetAuthenticatedEncryptorByKeyId(Guid keyId, out bool isRevoked)
        {
            isRevoked = false;
            AuthenticatedEncryptorHolder holder;
            _keyToEncryptorMap.TryGetValue(keyId, out holder);
            return holder?.GetEncryptorInstance(out isRevoked);
        }

        private sealed class AuthenticatedEncryptorHolder
        {
            private readonly IKey _key;
            private IAuthenticatedEncryptor _encryptor;

            internal AuthenticatedEncryptorHolder(IKey key)
            {
                _key = key;
            }

            internal IAuthenticatedEncryptor GetEncryptorInstance(out bool isRevoked)
            {
                // simple double-check lock pattern
                // we can't use LazyInitializer<T> because we don't have a simple value factory
                IAuthenticatedEncryptor encryptor = Volatile.Read(ref _encryptor);
                if (encryptor == null)
                {
                    lock (this)
                    {
                        encryptor = Volatile.Read(ref _encryptor);
                        if (encryptor == null)
                        {
                            encryptor = _key.CreateEncryptorInstance();
                            Volatile.Write(ref _encryptor, encryptor);
                        }
                    }
                }
                isRevoked = _key.IsRevoked;
                return encryptor;
            }
        }
    }
}
