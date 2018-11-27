// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace BasicWebSite
{
    public class Program
    {
        public static void Main(string[] args) => CreateWebHostBuilder(args).Build().Run();

        // Do not change. This is the pattern our test infrastructure uses to initialize a IWebHostBuilder from
        // a users app.
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .ConfigureServices(s => s.AddSingleton(new TestService { Message = "true" }))
                .UseKestrel()
                .UseIISIntegration();
    }

    public class TestService
    {
        public string Message { get; set; }
    }
}
