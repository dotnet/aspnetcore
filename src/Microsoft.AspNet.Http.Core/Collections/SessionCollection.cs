// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Interfaces;

namespace Microsoft.AspNet.Http.Core.Collections
{
    public class SessionCollection : ISessionCollection
    {
        private readonly ISession _session;

        public SessionCollection(ISession session)
        {
            _session = session;
        }

        public byte[] this[string key]
        {
            get
            {
                byte[] value;
                TryGetValue(key, out value);
                return value;
            }
            set
            {
                if (value == null)
                {
                    Remove(key);
                }
                else
                {
                    Set(key, new ArraySegment<byte>(value));
                }
            }
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return _session.TryGetValue(key, out value);
        }

        public void Set(string key, ArraySegment<byte> value)
        {
            _session.Set(key, value);
        }

        public void Remove(string key)
        {
            _session.Remove(key);
        }

        public void Clear()
        {
            _session.Clear();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<string, byte[]>> GetEnumerator()
        {
            foreach (var key in _session.Keys)
            {
                yield return new KeyValuePair<string, byte[]>(key, this[key]);
            }
        }
    }
}