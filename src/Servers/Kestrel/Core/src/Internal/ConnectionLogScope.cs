// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Globalization;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal readonly struct ConnectionLogScope : IReadOnlyList<KeyValuePair<string, object>>
    {
        private readonly string _connectionId;

        private readonly string _cachedToString;

        public ConnectionLogScope(string connectionId)
        {
            _connectionId = connectionId;
            _cachedToString = string.Format(
                CultureInfo.InvariantCulture,
                "ConnectionId:{0}",
                _connectionId);
        }

        public KeyValuePair<string, object> this[int index]
        {
            get
            {
                if (index == 0)
                {
                    return new KeyValuePair<string, object>("ConnectionId", _connectionId);
                }

                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public int Count => 1;

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            for (var i = 0; i < Count; ++i)
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
            return _cachedToString;
        }
    }
}
