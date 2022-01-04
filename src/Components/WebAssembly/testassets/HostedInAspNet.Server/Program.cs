// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace HostedInAspNet.Server;

public class Program
{
    public static void Main(string[] args)
    {
        BuildWebHost(args).Run();
    }

    public static IHost BuildWebHost(string[] args) =>
        Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webHostBuilder =>
        {
            // We require this line because we run in Production environment
            // and static web assets are only on by default during development.
            webHostBuilder.UseStaticWebAssets();

            webHostBuilder.UseStartup<Startup>();
        })
        .Build();
}
