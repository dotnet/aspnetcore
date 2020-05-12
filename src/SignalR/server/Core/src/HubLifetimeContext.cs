// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SignalR
{
    // TODO: naming
    public class HubLifetimeContext
    {
        public HubLifetimeContext(HubCallerContext context, IServiceProvider serviceProvider, Hub hub)
        {
            Hub = hub;
            ServiceProvider = serviceProvider;
            Context = context;
        }

        /// <summary>
        /// Gets the context for the active Hub connection and caller.
        /// </summary>
        public HubCallerContext Context { get; }

        /// <summary>
        /// Gets the Hub instance.
        /// </summary>
        public Hub Hub { get; }

        public IServiceProvider ServiceProvider { get; }
    }
}
