// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv
{
    /// <summary>
    /// Provides programmatic configuration of Libuv transport features.
    /// </summary>
    public class LibuvTransportOptions
    {
        /// <summary>
        /// The number of libuv I/O threads used to process requests.
        /// </summary>
        /// <remarks>
        /// Defaults to half of <see cref="Environment.ProcessorCount" /> rounded down and clamped between 1 and 16.
        /// </remarks>
        public int ThreadCount { get; set; } = ProcessorThreadCount;

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
        /// Defaults to 128.
        /// </remarks>
        public int Backlog { get; set; } = 128;

        public long? MaxReadBufferSize { get; set; } = 1024 * 1024;

        public long? MaxWriteBufferSize { get; set; } = 64 * 1024;

        internal Func<MemoryPool<byte>> MemoryPoolFactory { get; set; } = System.Buffers.SlabMemoryPoolFactory.Create;

        private static int ProcessorThreadCount
        {
            get
            {
                // Actual core count would be a better number
                // rather than logical cores which includes hyper-threaded cores.
                // Divide by 2 for hyper-threading, and good defaults (still need threads to do webserving).
                var threadCount = Environment.ProcessorCount >> 1;

                if (threadCount < 1)
                {
                    // Ensure shifted value is at least one
                    return 1;
                }

                if (threadCount > 16)
                {
                    // Receive Side Scaling RSS Processor count currently maxes out at 16
                    // would be better to check the NIC's current hardware queues; but xplat...
                    return 16;
                }

                return threadCount;
            }
        }
    }
}
