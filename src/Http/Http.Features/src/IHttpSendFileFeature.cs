// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Provides an efficient mechanism for transferring files from disk to the network.
    /// </summary>
    [Obsolete("Use IHttpResponseBodyFeature instead.", error: true)]
    public interface IHttpSendFileFeature
    {
        /// <summary>
        /// Sends the requested file in the response body. This may bypass the IHttpResponseFeature.Body
        /// <see cref="Stream"/>. A response may include multiple writes.
        /// </summary>
        /// <param name="path">The full disk path to the file.</param>
        /// <param name="offset">The offset in the file to start at.</param>
        /// <param name="count">The number of bytes to send, or null to send the remainder of the file.</param>
        /// <param name="cancellation">A <see cref="CancellationToken"/> used to abort the transmission.</param>
        /// <returns></returns>
        Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellation);
    }
}
