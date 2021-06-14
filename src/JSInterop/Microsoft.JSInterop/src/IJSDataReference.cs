// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Represents a reference to JavaScript data to be consumed through a stream.
    /// </summary>
    public interface IJSDataReference : IAsyncDisposable
    {
        /// <summary>
        /// Length of the stream provided by JavaScript.
        /// </summary>
        public long Length { get; }

        /// <summary>
        /// Initiatializes a <see cref="Stream"/> with the <see cref="JSRuntime"/> for the current data reference.
        /// </summary>
        /// <param name="maxAllowedSize">Maximum number of bytes permitted to be read from JavaScript.</param>
        /// <param name="maxBufferSize">Amount of bytes to buffer before flushing.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken" /> for cancelling read.</param>
        /// <returns>Stream which can provide data associated with the current data reference.</returns>
        public Task<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000, long maxBufferSize = 100 * 1024, CancellationToken cancellationToken = default);
    }
}
