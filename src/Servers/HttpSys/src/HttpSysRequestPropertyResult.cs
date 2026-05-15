// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.HttpSys;

/// <summary>
/// Result of an asynchronous <see cref="IHttpSysRequestPropertyFeature.TryGetRequestPropertyAsync"/> call.
/// </summary>
public readonly struct HttpSysRequestPropertyResult
{
    /// <summary>
    /// True if the property value was written into the caller-supplied output buffer.
    /// False if the buffer was too small; in that case <see cref="BytesReturned"/> contains the required size.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// On success: the number of bytes written into the caller-supplied output buffer.
    /// Use <c>output.Span[..BytesReturned]</c> (or <c>output[..BytesReturned]</c>) to read just the populated bytes.
    /// On failure (<see cref="Succeeded"/> is false): the size of the buffer required to hold the value.
    /// </summary>
    public int BytesReturned { get; init; }
}
