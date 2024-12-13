// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic;

/// <summary>
/// Options for Quic based connections.
/// </summary>
public sealed class QuicTransportOptions
{
    private long _defaultStreamErrorCode;
    private long _defaultCloseErrorCode;

    /// <summary>
    /// The maximum number of concurrent bi-directional streams per connection.
    /// </summary>
    [RequiresPreviewFeatures]
    public int MaxBidirectionalStreamCount { get; set; } = 100;

    /// <summary>
    /// The maximum number of concurrent inbound uni-directional streams per connection.
    /// </summary>
    [RequiresPreviewFeatures]
    public int MaxUnidirectionalStreamCount { get; set; } = 10;

    /// <summary>
    /// The maximum read size.
    /// </summary>
    [RequiresPreviewFeatures]
    public long? MaxReadBufferSize { get; set; } = 1024 * 1024;

    /// <summary>
    /// The maximum write size.
    /// </summary>
    [RequiresPreviewFeatures]
    public long? MaxWriteBufferSize { get; set; } = 64 * 1024;

    /// <summary>
    /// The maximum length of the pending connection queue.
    /// </summary>
    public int Backlog { get; set; } = 512;

    /// <summary>
    /// Error code used when the stream needs to abort the read or write side of the stream internally.
    /// </summary>
    public long DefaultStreamErrorCode
    {
        get => _defaultStreamErrorCode;
        set
        {
            ValidateErrorCode(value);
            _defaultStreamErrorCode = value;
        }
    }

    /// <summary>
    /// Error code used when an open connection is disposed.
    /// </summary>
    public long DefaultCloseErrorCode
    {
        get => _defaultCloseErrorCode;
        set
        {
            ValidateErrorCode(value);
            _defaultCloseErrorCode = value;
        }
    }

    internal static void ValidateErrorCode(long errorCode)
    {
        const long MinErrorCode = 0;
        const long MaxErrorCode = (1L << 62) - 1;

        if (errorCode < MinErrorCode || errorCode > MaxErrorCode)
        {
            // Print the values in hex since the max is unintelligible in decimal
            throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, $"A value between 0x{MinErrorCode:x} and 0x{MaxErrorCode:x} is required.");
        }
    }

    internal TimeProvider TimeProvider = TimeProvider.System;
}
