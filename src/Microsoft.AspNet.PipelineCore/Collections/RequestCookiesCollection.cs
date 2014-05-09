// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNet.Http;
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