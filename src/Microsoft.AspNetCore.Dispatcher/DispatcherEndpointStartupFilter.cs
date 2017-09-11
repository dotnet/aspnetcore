// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class DispatcherEndpointStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder =>
            {
                next(builder);
                if (builder.Properties.ContainsKey("Dispatcher"))
                {
                    builder.UseMiddleware<EndpointMiddleware>();
                }
            };
        }
    }
}
