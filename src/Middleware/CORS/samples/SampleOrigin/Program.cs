// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace SampleOrigin
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://+:9001", "http://+:9002")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureLogging(factory => factory.AddConsole())
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
