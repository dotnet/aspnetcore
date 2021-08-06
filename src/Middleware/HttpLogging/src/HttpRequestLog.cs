// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Text;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal readonly struct HttpRequestLog : IReadOnlyList<KeyValuePair<string, object?>>
    {
        private readonly List<KeyValuePair<string, object?>> _keyValues;
        private readonly string _cachedToString;

        internal static readonly Func<HttpRequestLog, Exception?, string> Callback = (state, exception) => state.ToString();

        public HttpRequestLog(List<KeyValuePair<string, object?>> keyValues)
        {
            _keyValues = keyValues;

            // Use 2kb as a rough average size for request headers
            var builder = new ValueStringBuilder(2 * 1024);
            var count = _keyValues.Count;
            builder.Append("Request:");
            builder.Append(Environment.NewLine);

            for (var i = 0; i < count - 1; i++)
            {
                var kvp = _keyValues[i];
                builder.Append(kvp.Key);
                builder.Append(": ");
                builder.Append(kvp.Value?.ToString());
                builder.Append(Environment.NewLine);
            }

            if (count > 0)
            {
                var kvp = _keyValues[count - 1];
                builder.Append(kvp.Key);
                builder.Append(": ");
                builder.Append(kvp.Value?.ToString());
            }

            _cachedToString = builder.ToString();
        }

        public KeyValuePair<string, object?> this[int index] => _keyValues[index];

        public int Count => _keyValues.Count;

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            var count = _keyValues.Count;
            for (var i = 0; i < count; i++)
            {
                yield return _keyValues[i];
            }
        }

        public override string ToString()
        {
            return _cachedToString;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
