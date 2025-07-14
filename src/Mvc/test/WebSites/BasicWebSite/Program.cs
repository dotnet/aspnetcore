// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;

namespace BasicWebSite;

public class Program
{
    public static void Main(string[] args)
    {
        using var host = CreateHostBuilder(args).Build();
        host.Run();
    }

    // This method now returns IHostBuilder and uses the new pattern with HostBuilder and ConfigureWebHost
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<StartupWithoutEndpointRouting>()
                    .UseKestrel()
                    .UseIISIntegration();
            });
}

public class TestService
{
    public string Message { get; set; }
}
