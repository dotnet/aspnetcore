// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace DispatcherSample.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var webHost = GetWebHostBuilder().Build();
            webHost.Run();
        }

        // For unit testing
        public static IWebHostBuilder GetWebHostBuilder()
        {
            return new WebHostBuilder()
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<Startup>();
        }
    }
}
