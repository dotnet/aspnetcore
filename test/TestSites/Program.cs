// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;

namespace TestSites
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseDefaultConfiguration(args)
                .UseIISPlatformHandlerUrl()
                .UseStartup("TestSites")
                .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                .Build();

            host.Run();
        }
    }
}
