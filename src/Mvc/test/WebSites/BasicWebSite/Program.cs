// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                .UseStartup<StartupWithoutEndpointRouting>()
                .UseKestrel()
                .UseIISIntegration();
    }

    public class TestService
    {
        public string Message { get; set; }
    }
}
