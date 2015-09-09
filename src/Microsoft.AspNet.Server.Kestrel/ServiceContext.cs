// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Server.Kestrel.Http;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.Dnx.Runtime;

namespace Microsoft.AspNet.Server.Kestrel
{
    public class ServiceContext
    {
        public IApplicationShutdown AppShutdown { get; set; }

        public IMemoryPool Memory { get; set; }

        public IKestrelTrace Log { get; set; }
    }
}
