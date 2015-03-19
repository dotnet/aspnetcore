// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.AspNet.Http;
using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.AspNet.WebSockets.Client.Test
{
    public class KestrelWebSocketHelpers
    {
        public static IDisposable CreateServer(Func<HttpContext, Task> app)
        {
            Action<IApplicationBuilder> startup = builder =>
            {
                builder.Use(async (ct, next) =>
                {
                    try
                    {
                        // Kestrel does not return proper error responses:
                        // https://github.com/aspnet/KestrelHttpServer/issues/43
                        await next();
                    }
                    catch (Exception ex)
                    {
                        if (ct.Response.HeadersSent)
                        {
                            throw;
                        }

                        ct.Response.StatusCode = 500;
                        ct.Response.Headers.Clear();
                        await ct.Response.WriteAsync(ex.ToString());
                    }
                });
                builder.UseWebSockets();
                builder.Run(c => app(c));
            };

            var config = new Configuration();
            config.Add(new MemoryConfigurationSource());
            config.Set("server.urls", "http://localhost:54321");

            var context = new HostingContext()
            {
                Configuration = config,
                ServerFactoryLocation = "Kestrel",
                StartupMethods = new StartupMethods(startup, configureServices: null)
            };
            return new HostingEngine().Start(context);
        }
    }
}