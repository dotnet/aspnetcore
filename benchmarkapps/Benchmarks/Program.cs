// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            GetWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder GetWebHostBuilder(string[] args)
        {
            var config = new ConfigurationBuilder()
                .Build();

            // Consoler logger has a major impact on perf results, so do not use
            // default builder.

            return new WebHostBuilder()
                .UseConfiguration(config)
                .UseKestrel()
                .UseStartup<Startup>();
        }
    }
}
