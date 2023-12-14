// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Server.Kestrel.Core;

/// <summary>
/// Limits only applicable to HTTP/3 connections.
/// </summary>
public class Http3Limits
{
    private int _headerTableSize;
    private int _maxRequestHeaderFieldSize = 32 * 1024; // Matches MaxRequestHeadersTotalSize

    /// <summary>
    /// Limits the size of the header compression table, in octets, the QPACK decoder on the server can use.
    /// <para>
    /// Value must be greater than 0, defaults to 0.
    /// </para>
    /// </summary>
    // TODO: Make public https://github.com/dotnet/aspnetcore/issues/26666
    internal int HeaderTableSize
    {
        get => _headerTableSize;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, CoreStrings.GreaterThanZeroRequired);
            }

            _headerTableSize = value;
        }
    }

    /// <summary>
    /// Indicates the size of the maximum allowed size of a request header field sequence. This limit applies to both name and value sequences in their compressed and uncompressed representations.
    /// <para>
    /// Value must be greater than 0, defaults to 2^14 (16,384).
    /// </para>
    /// </summary>
    public int MaxRequestHeaderFieldSize
    {
        get => _maxRequestHeaderFieldSize;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, CoreStrings.GreaterThanZeroRequired);
            }

            _maxRequestHeaderFieldSize = value;
        }
    }

    internal void Serialize(Utf8JsonWriter writer)
    {
        writer.WritePropertyName(nameof(HeaderTableSize));
        writer.WriteNumberValue(HeaderTableSize);

        writer.WritePropertyName(nameof(MaxRequestHeaderFieldSize));
        writer.WriteNumberValue(MaxRequestHeaderFieldSize);
    }
}
