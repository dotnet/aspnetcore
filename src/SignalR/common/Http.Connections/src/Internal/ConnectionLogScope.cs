// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Globalization;

namespace Microsoft.AspNetCore.Http.Connections.Internal;

internal sealed class ConnectionLogScope : IReadOnlyList<KeyValuePair<string, object?>>
{
    private string? _cachedToString;

    public string? ConnectionId { get; set; }

    public ConnectionLogScope(string? connectionId)
    {
        ConnectionId = connectionId;
    }

    public KeyValuePair<string, object?> this[int index]
    {
        get
        {
            if (Count == 1 && index == 0)
            {
                return new KeyValuePair<string, object?>("TransportConnectionId", ConnectionId);
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public int Count => string.IsNullOrEmpty(ConnectionId) ? 0 : 1;

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
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

    public override string? ToString()
    {
        if (_cachedToString == null)
        {
            if (!string.IsNullOrEmpty(ConnectionId))
            {
                _cachedToString = string.Format(
                    CultureInfo.InvariantCulture,
                    "TransportConnectionId:{0}",
                    ConnectionId);
            }
        }

        return _cachedToString;
    }
}
