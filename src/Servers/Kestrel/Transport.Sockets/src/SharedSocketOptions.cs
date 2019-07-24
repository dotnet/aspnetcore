using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
{
    public class SharedSocketOptions
    {
        /// <summary>
        /// Set to false to enable Nagle's algorithm for all connections.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool NoDelay { get; set; } = true;

        /// <summary>
        /// The maximum size before the transport will stop proactively reading from the transport.
        /// </summary>
        public long? MaxReadBufferSize { get; set; } = 1024 * 1024;

        /// <summary>
        /// The maximum size the transport will buffer outside of OS buffers before pausing writes.
        /// </summary>
        public long? MaxWriteBufferSize { get; set; } = 64 * 1024;

        internal Func<MemoryPool<byte>> MemoryPoolFactory { get; set; } = System.Buffers.SlabMemoryPoolFactory.Create;
    }
}
