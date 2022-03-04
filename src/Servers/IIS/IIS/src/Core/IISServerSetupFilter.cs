// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Server.IIS.Core
{
    internal class IISServerSetupFilter : IStartupFilter
    {
        private readonly string _virtualPath;

        public IISServerSetupFilter(string virtualPath)
        {
            _virtualPath = virtualPath;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                var server = app.ApplicationServices.GetService<IServer>();
                if (server?.GetType() != typeof(IISHttpServer))
                {
                    throw new InvalidOperationException("Application is running inside IIS process but is not configured to use IIS server.");
                }

                app.UsePathBase(_virtualPath);
                next(app);
            };
        }
    }
}
