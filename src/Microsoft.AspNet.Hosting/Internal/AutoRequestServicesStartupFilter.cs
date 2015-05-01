// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Startup;

namespace Microsoft.AspNet.Hosting.Internal
{
    public class AutoRequestServicesStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(IApplicationBuilder app, Action<IApplicationBuilder> next)
        {
            return builder =>
            {
                app.UseMiddleware<RequestServicesContainerMiddleware>();
                next(builder);
            };
        }
    }
}
