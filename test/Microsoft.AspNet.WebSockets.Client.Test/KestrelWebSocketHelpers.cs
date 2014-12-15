// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.Runtime.Infrastructure;

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
            var services = HostingServices.Create(CallContextServiceLocator.Locator?.ServiceProvider, config)
                .BuildServiceProvider();

            var context = new HostingContext()
            {
                Services = services,
                Configuration = config,
                ServerName = "Kestrel",
                ApplicationStartup = startup,
            };

            var engine = services.GetRequiredService<IHostingEngine>();
            return engine.Start(context);
        }
    }
}