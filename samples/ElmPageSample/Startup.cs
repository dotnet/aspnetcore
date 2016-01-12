// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ElmPageSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddElm();

            services.ConfigureElm(elmOptions =>
            {
                elmOptions.Filter = (loggerName, loglevel) => loglevel == LogLevel.Debug;
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseElmPage();

            app.UseElmCapture();

            app.UseMiddleware<HelloWorldMiddleware>();
        }

        public static void Main(string[] args)
        {
            var application = new WebApplicationBuilder()
                .UseConfiguration(WebApplicationConfiguration.GetDefault(args))
                .UseIISPlatformHandlerUrl()
                .UseStartup<Startup>()
                .Build();

            application.Run();
        }
    }
}
