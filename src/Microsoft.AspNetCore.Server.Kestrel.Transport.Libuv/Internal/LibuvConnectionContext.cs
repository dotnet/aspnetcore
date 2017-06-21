// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    public class LibuvConnectionContext : IConnectionInformation
    {
        public LibuvConnectionContext()
        {
        }

        public LibuvConnectionContext(ListenerContext context)
        {
            ListenerContext = context;
        }

        public ListenerContext ListenerContext { get; set; }

        public IPEndPoint RemoteEndPoint { get; set; }
        public IPEndPoint LocalEndPoint { get; set; }

        public PipeFactory PipeFactory => ListenerContext.Thread.PipeFactory;
        public IScheduler InputWriterScheduler => ListenerContext.Thread;
        public IScheduler OutputReaderScheduler => ListenerContext.Thread;
    }
}