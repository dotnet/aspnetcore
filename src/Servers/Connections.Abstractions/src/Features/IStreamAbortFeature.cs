// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Connections.Features
{
    /// <summary>
    /// Supports aborting individual sides of a connection stream.
    /// </summary>
    public interface IStreamAbortFeature
    {
        /// <summary>
        /// Abort the read side of the connection stream.
        /// </summary>
        /// <param name="errorCode">The error code to send with the abort.</param>
        /// <param name="abortReason">An optional <see cref="ConnectionAbortedException"/> describing the reason to abort the read side of the connection stream.</param>
        void AbortRead(long errorCode, ConnectionAbortedException abortReason);

        /// <summary>
        /// Abort the write side of the connection stream.
        /// </summary>
        /// <param name="errorCode">The error code to send with the abort.</param>
        /// <param name="abortReason">An optional <see cref="ConnectionAbortedException"/> describing the reason to abort the write side of the connection stream.</param>
        void AbortWrite(long errorCode, ConnectionAbortedException abortReason);
    }
}
