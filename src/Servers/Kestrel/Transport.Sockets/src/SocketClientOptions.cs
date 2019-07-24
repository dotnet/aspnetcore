using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
{
    public class SocketClientOptions : SharedSocketOptions
    {
        /// <summary>
        /// Determines where read and write callbacks run.
        /// </summary>
        public PipeScheduler Scheduler { get; set; } = new IOQueue();
    }
}
