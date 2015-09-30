// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Server.Kestrel.Filter;
using Microsoft.AspNet.Server.Kestrel.Http;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.Dnx.Runtime;

namespace Microsoft.AspNet.Server.Kestrel
{
    public class ServiceContext
    {
        public ServiceContext()
        {
            Memory = new MemoryPool();
        }

        public ServiceContext(ServiceContext context)
        {
            AppShutdown = context.AppShutdown;
            Memory = context.Memory;
            Log = context.Log;
            DateHeaderValueManager = context.DateHeaderValueManager;
            ConnectionFilter = context.ConnectionFilter;
        }

        public IApplicationShutdown AppShutdown { get; set; }

        public IMemoryPool Memory { get; set; }

        public IKestrelTrace Log { get; set; }

        public DateHeaderValueManager DateHeaderValueManager { get; set; }

        public IConnectionFilter ConnectionFilter { get; set; }
    }
}
