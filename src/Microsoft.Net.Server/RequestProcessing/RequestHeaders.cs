// -----------------------------------------------------------------------
// <copyright file="RequestHeaders.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
// Copyright 2011-2012 Katana contributors
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.Net.Server
{
    internal partial class RequestHeaders : IDictionary<string, string[]>
    {
        private IDictionary<string, string[]> _extra;
        private NativeRequestContext _requestMemoryBlob;

        internal RequestHeaders(NativeRequestContext requestMemoryBlob)
        {
            _requestMemoryBlob = requestMemoryBlob;
        }

        private IDictionary<string, string[]> Extra
        {
            get
            {
                if (_extra == null)
                {
                    var newDict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                    GetUnknownHeaders(newDict);
                    Interlocked.CompareExchange(ref _extra, newDict, null);
                }
                return _extra;
            }
        }

        string[] IDictionary<string, string[]>.this[string key]
        {
            get
            {
                string[] value;
                return PropertiesTryGetValue(key, out value) ? value : Extra[key];
            }
            set
            {
                if (!PropertiesTrySetValue(key, value))
                {
                    Extra[key] = value;
                }
            }
        }

        private string GetKnownHeader(HttpSysRequestHeader header)
        {
            return UnsafeNclNativeMethods.HttpApi.GetKnownHeader(_requestMemoryBlob.RequestBuffer,
                _requestMemoryBlob.OriginalBlobAddress, (int)header);
        }

        private void GetUnknownHeaders(IDictionary<string, string[]> extra)
        {
            UnsafeNclNativeMethods.HttpApi.GetUnknownHeaders(extra, _requestMemoryBlob.RequestBuffer,
                _requestMemoryBlob.OriginalBlobAddress);
        }

        void IDictionary<string, string[]>.Add(string key, string[] value)
        {
            if (!PropertiesTrySetValue(key, value))
            {
                Extra.Add(key, value);
            }
        }

        bool IDictionary<string, string[]>.ContainsKey(string key)
        {
            return PropertiesContainsKey(key) || Extra.ContainsKey(key);
        }

        ICollection<string> IDictionary<string, string[]>.Keys
        {
            get { return PropertiesKeys().Concat(Extra.Keys).ToArray(); }
        }

        bool IDictionary<string, string[]>.Remove(string key)
        {
            // Although this is a mutating operation, Extra is used instead of StrongExtra,
            // because if a real dictionary has not been allocated the default behavior of the
            // nil dictionary is perfectly fine.
            return PropertiesTryRemove(key) || Extra.Remove(key);
        }

        bool IDictionary<string, string[]>.TryGetValue(string key, out string[] value)
        {
            return PropertiesTryGetValue(key, out value) || Extra.TryGetValue(key, out value);
        }

        ICollection<string[]> IDictionary<string, string[]>.Values
        {
            get { return PropertiesValues().Concat(Extra.Values).ToArray(); }
        }

        void ICollection<KeyValuePair<string, string[]>>.Add(KeyValuePair<string, string[]> item)
        {
            ((IDictionary<string, object>)this).Add(item.Key, item.Value);
        }

        void ICollection<KeyValuePair<string, string[]>>.Clear()
        {
            foreach (var key in PropertiesKeys())
            {
                PropertiesTryRemove(key);
            }
            Extra.Clear();
        }

        bool ICollection<KeyValuePair<string, string[]>>.Contains(KeyValuePair<string, string[]> item)
        {
            object value;
            return ((IDictionary<string, object>)this).TryGetValue(item.Key, out value) && Object.Equals(value, item.Value);
        }

        void ICollection<KeyValuePair<string, string[]>>.CopyTo(KeyValuePair<string, string[]>[] array, int arrayIndex)
        {
            PropertiesEnumerable().Concat(Extra).ToArray().CopyTo(array, arrayIndex);
        }

        int ICollection<KeyValuePair<string, string[]>>.Count
        {
            get { return PropertiesKeys().Count() + Extra.Count; }
        }

        bool ICollection<KeyValuePair<string, string[]>>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<KeyValuePair<string, string[]>>.Remove(KeyValuePair<string, string[]> item)
        {
            return ((IDictionary<string, string[]>)this).Contains(item) &&
                ((IDictionary<string, string[]>)this).Remove(item.Key);
        }

        IEnumerator<KeyValuePair<string, string[]>> IEnumerable<KeyValuePair<string, string[]>>.GetEnumerator()
        {
            return PropertiesEnumerable().Concat(Extra).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary<string, string[]>)this).GetEnumerator();
        }
    }
}
