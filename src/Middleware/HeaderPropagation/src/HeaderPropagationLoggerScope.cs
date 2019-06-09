// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    internal class HeaderPropagationLoggerScope : IReadOnlyList<KeyValuePair<string, object>>
    {
        private readonly List<string> _headerNames;
        private readonly IDictionary<string, StringValues> _headerValues;
        private string _cachedToString;

        public HeaderPropagationLoggerScope(List<string> headerNames, IDictionary<string, StringValues> headerValues)
        {
            _headerNames = headerNames ?? throw new ArgumentNullException(nameof(headerNames));
            _headerValues = headerValues ?? throw new ArgumentNullException(nameof(headerValues));
        }

        public int Count => _headerNames.Count;

        public KeyValuePair<string, object> this[int index]
        {
            get
            {
                var headerName = _headerNames[index];
                _headerValues.TryGetValue(headerName, out var value);
                return new KeyValuePair<string, object>(headerName, value);
            }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            if (_cachedToString == null)
            {
                var sb = new StringBuilder();

                for (int i = 0; i < Count; i++)
                {
                    if (i > 0) sb.Append(' ');

                    var headerName = _headerNames[i];
                    _headerValues.TryGetValue(headerName, out var value);

                    sb.Append(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}:{1}",
                        headerName, value.ToString()));
                }

                _cachedToString = sb.ToString();
            }

            return _cachedToString;
        }
    }
}
