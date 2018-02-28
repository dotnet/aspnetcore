// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    public class LibuvConnectionContext : TransportConnection
    {
        public LibuvConnectionContext(ListenerContext context)
        {
            ListenerContext = context;
        }

        public ListenerContext ListenerContext { get; set; }

        public override MemoryPool<byte> MemoryPool => ListenerContext.Thread.MemoryPool;
        public override PipeScheduler InputWriterScheduler => ListenerContext.Thread;
        public override PipeScheduler OutputReaderScheduler => ListenerContext.Thread;
    }
}
