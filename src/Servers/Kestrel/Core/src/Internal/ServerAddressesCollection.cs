// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class ServerAddressesCollection : ICollection<string>
    {
        private readonly List<string> _addresses = new List<string>();
        private readonly PublicServerAddressesCollection _publicCollection;

        public ServerAddressesCollection()
        {
            _publicCollection = new PublicServerAddressesCollection(this);
        }

        public ICollection<string> PublicCollection => _publicCollection;

        public bool IsReadOnly => false;

        public int Count
        {
            get
            {
                lock (_addresses)
                {
                    return _addresses.Count;
                }
            }
        }

        public void PreventPublicMutation()
        {
            lock (_addresses)
            {
                _publicCollection.IsReadOnly = true;
            }
        }

        public void Add(string item)
        {
            lock (_addresses)
            {
                _addresses.Add(item);
            }
        }

        public bool Remove(string item)
        {
            lock (_addresses)
            {
                return _addresses.Remove(item);
            }
        }

        public void Clear()
        {
            lock (_addresses)
            {
                _addresses.Clear();
            }
        }

        public void InternalAdd(string item)
        {
            lock (_addresses)
            {
                _addresses.Add(item);
            }
        }

        public bool InternalRemove(string item)
        {
            lock (_addresses)
            {
                return _addresses.Remove(item);
            }
        }

        public void InternalClear()
        {
            lock (_addresses)
            {
                _addresses.Clear();
            }
        }

        public bool Contains(string item)
        {
            lock (_addresses)
            {
                return _addresses.Contains(item);
            }
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            lock (_addresses)
            {
                _addresses.CopyTo(array, arrayIndex);
            }
        }

        public IEnumerator<string> GetEnumerator()
        {
            lock (_addresses)
            {
                // Copy inside the lock.
                return new List<string>(_addresses).GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class PublicServerAddressesCollection : ICollection<string>
        {
            private readonly ServerAddressesCollection _addressesCollection;
            private readonly object _addressesLock;

            public PublicServerAddressesCollection(ServerAddressesCollection addresses)
            {
                _addressesCollection = addresses;
                _addressesLock = addresses._addresses;
            }

            public bool IsReadOnly { get; set; }

            public int Count => _addressesCollection.Count;

            public void Add(string item)
            {
                lock (_addressesLock)
                {
                    ThrowIfReadonly();
                    _addressesCollection.Add(item);
                }
            }

            public bool Remove(string item)
            {
                lock (_addressesLock)
                {
                    ThrowIfReadonly();
                    return _addressesCollection.Remove(item);
                }
            }

            public void Clear()
            {
                lock (_addressesLock)
                {
                    ThrowIfReadonly();
                    _addressesCollection.Clear();
                }
            }

            public bool Contains(string item)
            {
                return _addressesCollection.Contains(item);
            }

            public void CopyTo(string[] array, int arrayIndex)
            {
                _addressesCollection.CopyTo(array, arrayIndex);
            }

            public IEnumerator<string> GetEnumerator()
            {
                return _addressesCollection.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _addressesCollection.GetEnumerator();
            }

            [StackTraceHidden]
            private void ThrowIfReadonly()
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException($"{nameof(IServerAddressesFeature)}.{nameof(IServerAddressesFeature.Addresses)} cannot be modified after the server has started.");
                }
            }
        }
    }
}
