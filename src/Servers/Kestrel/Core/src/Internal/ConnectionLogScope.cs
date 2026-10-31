// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Globalization;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed class ConnectionLogScope : IReadOnlyList<KeyValuePair<string, object>>
{
    private readonly string _connectionId;

    private string? _cachedToString;

    public ConnectionLogScope(string connectionId)
    {
        _connectionId = connectionId;
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
        for (int i = 0; i < Count; ++i)
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
            _cachedToString = string.Format(
                CultureInfo.InvariantCulture,
                "ConnectionId:{0}",
                _connectionId);
        }

        return _cachedToString;
    }
}
