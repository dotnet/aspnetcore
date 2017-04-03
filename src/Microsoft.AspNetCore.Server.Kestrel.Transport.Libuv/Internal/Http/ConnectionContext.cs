// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Transport;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public class ConnectionContext : IConnectionInformation
    {
        public ConnectionContext()
        {
        }

        public ConnectionContext(ListenerContext context)
        {
            ListenerContext = context;
        }

        public ListenerContext ListenerContext { get; set; }

        public ListenOptions ListenOptions => ListenerContext.ListenOptions;
        public IPEndPoint RemoteEndPoint { get; set; }
        public IPEndPoint LocalEndPoint { get; set; }

        public PipeFactory PipeFactory => ListenerContext.Thread.PipelineFactory;
        public IScheduler InputWriterScheduler => ListenerContext.Thread;
        public IScheduler OutputWriterScheduler => ListenerContext.Thread;

        public ITimeoutControl TimeoutControl { get; set; }
    }
}