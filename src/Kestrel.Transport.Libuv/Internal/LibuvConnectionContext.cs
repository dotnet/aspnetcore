// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.IO.Pipelines;
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
        
        public override PipeFactory PipeFactory => ListenerContext.Thread.PipeFactory;
        public override IScheduler InputWriterScheduler => ListenerContext.Thread;
        public override IScheduler OutputReaderScheduler => ListenerContext.Thread;
    }
}
