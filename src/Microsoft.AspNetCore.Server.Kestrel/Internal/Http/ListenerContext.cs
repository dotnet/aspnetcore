// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public class ListenerContext
    {
        public ListenerContext(ServiceContext serviceContext)
        {
            ServiceContext = serviceContext;
        }

        public ServiceContext ServiceContext { get; set; }

        public ServerAddress ServerAddress { get; set; }

        public KestrelThread Thread { get; set; }

        public KestrelServerOptions ServerOptions => ServiceContext.ServerOptions;
    }
}
