// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
{
    public class SocketTransportOptions
    {
        /// <summary>
        /// The number of I/O queues used to process requests. Set to 0 to directly schedule I/O to the ThreadPool.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="Environment.ProcessorCount" /> rounded down and clamped between 1 and 16.
        /// </remarks>
        public int IOQueueCount { get; set; } = Math.Min(Environment.ProcessorCount, 16);

        /// <summary>
        /// Wait until there is data available to allocate a buffer. Setting this to false can increase throughput at the cost of increased memory usage.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool WaitForDataBeforeAllocatingBuffer { get; set; } = true;

        /// <summary>
        /// Set to false to enable Nagle's algorithm for all connections.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool NoDelay { get; set; } = true;

        /// <summary>
        /// The maximum length of the pending connection queue.
        /// </summary>
        /// <remarks>
        /// Defaults to 512.
        /// </remarks>
        public int Backlog { get; set; } = 512;

        public long? MaxReadBufferSize { get; set; } = 1024 * 1024;

        public long? MaxWriteBufferSize { get; set; } = 64 * 1024;

        internal Func<MemoryPool<byte>> MemoryPoolFactory { get; set; } = System.Buffers.SlabMemoryPoolFactory.Create;
    }
}
