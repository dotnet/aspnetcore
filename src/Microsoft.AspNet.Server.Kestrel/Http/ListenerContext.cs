// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class ListenerContext : ServiceContext
    {
        public ListenerContext()
        {
        }

        public ListenerContext(ServiceContext serviceContext) 
            : base(serviceContext)
        {
        }

        public ListenerContext(ListenerContext listenerContext)
            : base(listenerContext)
        {
            Thread = listenerContext.Thread;
            Application = listenerContext.Application;
            Memory = listenerContext.Memory;
            Memory2 = listenerContext.Memory2;
            Log = listenerContext.Log;
        }

        public KestrelThread Thread { get; set; }

        public Func<Frame, Task> Application { get; set; }

        public MemoryPool2 Memory2 { get; set; }
    }
}
