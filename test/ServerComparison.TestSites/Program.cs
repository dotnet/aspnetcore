// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;

namespace ServerComparison.TestSites
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                // We set the server by name before default args so that command line arguments can override it.
                // This is used to allow deployers to choose the server for testing.
                .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                .UseDefaultHostingConfiguration(args)
                .UseIISIntegration()
                .UseStartup("ServerComparison.TestSites")
                .Build();

            host.Run();
        }
    }
}

