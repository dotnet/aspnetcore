// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal class HttpResponseLog : IReadOnlyList<KeyValuePair<string, object?>>
    {
        private readonly List<KeyValuePair<string, object?>> _keyValues;
        private string? _cachedToString;

        internal static readonly Func<object, Exception?, string> Callback = (state, exception) => ((HttpResponseLog)state).ToString();

        public HttpResponseLog(List<KeyValuePair<string, object?>> keyValues)
        {
            _keyValues = keyValues;
        }

        public KeyValuePair<string, object?> this[int index] => _keyValues[index];

        public int Count => _keyValues.Count;

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return _keyValues[i];
            }
        }

        public override string ToString()
        {
            if (_cachedToString == null)
            {
                // TODO new line separated list?
                _cachedToString = "";
            }

            return _cachedToString;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
