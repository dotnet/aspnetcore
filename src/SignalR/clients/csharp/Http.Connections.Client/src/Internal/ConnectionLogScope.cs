// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal;

internal sealed class ConnectionLogScope : IReadOnlyList<KeyValuePair<string, object?>>
{
    // Name chosen so as not to collide with Kestrel's "ConnectionId"
    private const string ClientConnectionIdKey = "ClientConnectionId";

    private string? _cachedToString;
    private string? _connectionId;

    public string? ConnectionId
    {
        get => _connectionId;
        set
        {
            _cachedToString = null;
            _connectionId = value;
        }
    }

    public KeyValuePair<string, object?> this[int index]
    {
        get
        {
            if (Count == 1 && index == 0)
            {
                return new KeyValuePair<string, object?>(ClientConnectionIdKey, ConnectionId);
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

    public override string ToString()
    {
        if (_cachedToString == null)
        {
            if (!string.IsNullOrEmpty(ConnectionId))
            {
                _cachedToString = FormattableString.Invariant($"{ClientConnectionIdKey}:{ConnectionId}");
            }
        }

        return _cachedToString ?? string.Empty;
    }
}
