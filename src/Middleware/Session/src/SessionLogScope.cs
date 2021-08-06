// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Globalization;

namespace Microsoft.AspNetCore.Session
{
    internal readonly struct SessionLogScope : IReadOnlyList<KeyValuePair<string, object?>>
    {
        private readonly string _sessionId;
        private readonly string _cachedString;

        public SessionLogScope(string sessionId)
        {
            _sessionId = sessionId;
            _cachedString = string.Format(
               CultureInfo.InvariantCulture,
               "SessionId:{0}",
               _sessionId);
        }

        public KeyValuePair<string, object?> this[int index]
        {
            get
            {
                return index switch
                {
                    0 => new KeyValuePair<string, object?>("SessionId", _sessionId),
                    _ => throw new IndexOutOfRangeException(nameof(index)),
                };
            }
        }
        public int Count => 1;

        public override string ToString()
        {
            return _cachedString;
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            yield return this[0];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
