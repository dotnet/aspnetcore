// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;

namespace RazorWebSite;

public class Program
{
    public static void Main(string[] args)
    {
        using var host = CreateHost(args)
            .Build();

        host.Run();
    }

    public static IHostBuilder CreateHost(string[] args) =>
        new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<Startup>()
                    .UseKestrel()
                    .UseIISIntegration();
            });
}
