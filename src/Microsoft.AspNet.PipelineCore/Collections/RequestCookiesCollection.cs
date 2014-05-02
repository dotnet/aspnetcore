// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.PipelineCore.Infrastructure;

namespace Microsoft.AspNet.PipelineCore.Collections
{
    public class RequestCookiesCollection : IReadableStringCollection
    {
        private readonly IDictionary<string, string> _dictionary;

        public RequestCookiesCollection()
        {
            _dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator()
        {
            foreach (var pair in _dictionary)
            {
                yield return new KeyValuePair<string, string[]>(pair.Key, new[] { pair.Value });
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public string this[string key]
        {
            get { return Get(key); }
        }

        public string Get(string key)
        {
            string value;
            return _dictionary.TryGetValue(key, out value) ? value : null;
        }

        public IList<string> GetValues(string key)
        {
            string value;
            return _dictionary.TryGetValue(key, out value) ? new[] { value } : null;
        }

        private static readonly char[] SemicolonAndComma = { ';', ',' };

        public void Reparse(string cookiesHeader)
        {
            _dictionary.Clear();
            ParsingHelpers.ParseDelimited(cookiesHeader, SemicolonAndComma, AddCookieCallback, _dictionary);
        }

        private static readonly Action<string, string, object> AddCookieCallback = (name, value, state) =>
        {
            var dictionary = (IDictionary<string, string>)state;
            if (!dictionary.ContainsKey(name))
            {
                dictionary.Add(name, value);
            }
        };
    }
}