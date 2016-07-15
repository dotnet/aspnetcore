// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
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
            ServerAddress = listenerContext.ServerAddress;
            Thread = listenerContext.Thread;
        }

        public ServerAddress ServerAddress { get; set; }

        public KestrelThread Thread { get; set; }
    }
}
