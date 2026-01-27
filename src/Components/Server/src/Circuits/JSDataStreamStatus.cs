// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Server.Circuits;

/// <summary>
/// Represents the status of receiving a chunk of data for a JS data stream.
/// </summary>
internal enum JSDataStreamStatus
{
    /// <summary>
    /// The chunk was successfully received and written to the stream.
    /// </summary>
    Success = 0,

    /// <summary>
    /// The chunk could not be written due to backpressure (pipe buffer is full).
    /// The sender should retry after a delay.
    /// </summary>
    Backpressure = 1,

    /// <summary>
    /// The stream is no longer available (disposed or cancelled).
    /// </summary>
    StreamDead = 2,
}
