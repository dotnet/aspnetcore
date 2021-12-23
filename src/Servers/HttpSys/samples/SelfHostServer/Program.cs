// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Hosting;

namespace SelfHostServer;

public static class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseStartup<Startup>()
                .UseHttpSys(options =>
                {
                    options.UrlPrefixes.Add("http://localhost:5000");
                    // This is a pre-configured IIS express port. See the PackageTags in the csproj.
                    options.UrlPrefixes.Add("https://localhost:44319");
                    options.Authentication.Schemes = AuthenticationSchemes.None;
                    options.Authentication.AllowAnonymous = true;
                });
            });
}
