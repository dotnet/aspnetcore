// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Connections.Features;

/// <summary>
/// Supports aborting individual sides of a connection stream.
/// </summary>
public interface IStreamAbortFeature
{
    /// <summary>
    /// Abort the read side of the connection stream.
    /// </summary>
    /// <param name="errorCode">The error code to send with the abort.</param>
    /// <param name="abortReason">A <see cref="ConnectionAbortedException"/> describing the reason to abort the read side of the connection stream.</param>
    void AbortRead(long errorCode, ConnectionAbortedException abortReason);

    /// <summary>
    /// Abort the write side of the connection stream.
    /// </summary>
    /// <param name="errorCode">The error code to send with the abort.</param>
    /// <param name="abortReason">A <see cref="ConnectionAbortedException"/> describing the reason to abort the write side of the connection stream.</param>
    void AbortWrite(long errorCode, ConnectionAbortedException abortReason);
}
