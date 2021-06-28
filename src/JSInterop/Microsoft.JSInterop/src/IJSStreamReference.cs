// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Represents a reference to JavaScript data to be consumed through a <see cref="Stream"/>.
    /// </summary>
    public interface IJSStreamReference : IAsyncDisposable
    {
        /// <summary>
        /// Length of the <see cref="Stream"/> provided by JavaScript.
        /// </summary>
        long Length { get; }

        /// <summary>
        /// Opens a <see cref="Stream"/> with the <see cref="JSRuntime"/> for the current data reference.
        /// </summary>
        /// <param name="maxAllowedSize">Maximum number of bytes permitted to be read from JavaScript.</param>
        /// <param name="pauseIncomingBytesThreshold">The number of unconsumed bytes to accept from JS before blocking. A value of zero prevents JS from blocking, allowing .NET to receive an unlimited number of bytes.</param>
        /// <param name="resumeIncomingBytesThreshold">The number of unflushed bytes at which point JS stops blocking.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken" /> for cancelling read.</param>
        /// <returns><see cref="Stream"/> which can provide data associated with the current data reference.</returns>
        ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000, long pauseIncomingBytesThreshold = -1, long resumeIncomingBytesThreshold = -1, CancellationToken cancellationToken = default);
    }
}
