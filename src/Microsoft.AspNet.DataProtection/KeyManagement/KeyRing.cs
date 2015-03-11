// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNet.DataProtection.AuthenticatedEncryption;

namespace Microsoft.AspNet.DataProtection.KeyManagement
{
    /// <summary>
    /// A basic implementation of <see cref="IKeyRing"/>.
    /// </summary>
    internal sealed class KeyRing : IKeyRing
    {
        private readonly KeyHolder _defaultKeyHolder;
        private readonly Dictionary<Guid, KeyHolder> _keyIdToKeyHolderMap;

        public KeyRing(Guid defaultKeyId, IEnumerable<IKey> keys)
        {
            _keyIdToKeyHolderMap = new Dictionary<Guid, KeyHolder>();
            foreach (IKey key in keys)
            {
                _keyIdToKeyHolderMap.Add(key.KeyId, new KeyHolder(key));
            }

            DefaultKeyId = defaultKeyId;
            _defaultKeyHolder = _keyIdToKeyHolderMap[defaultKeyId];
        }
        
        public IAuthenticatedEncryptor DefaultAuthenticatedEncryptor
        {
            get
            {
                bool unused;
                return _defaultKeyHolder.GetEncryptorInstance(out unused);
            }
        }

        public Guid DefaultKeyId { get; }

        public IAuthenticatedEncryptor GetAuthenticatedEncryptorByKeyId(Guid keyId, out bool isRevoked)
        {
            isRevoked = false;
            KeyHolder holder;
            _keyIdToKeyHolderMap.TryGetValue(keyId, out holder);
            return holder?.GetEncryptorInstance(out isRevoked);
        }

        // used for providing lazy activation of the authenticated encryptor instance
        private sealed class KeyHolder
        {
            private readonly IKey _key;
            private IAuthenticatedEncryptor _encryptor;

            internal KeyHolder(IKey key)
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
