// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace IdentitySample;

public static class Program
{
    public static void Main(string[] args)
    {

        var host = CreateHostBuilder(args).Build();
        host.Run();
    }

    public static IWebHostBuilder CreateHostBuilder(string[] args) =>
        new WebHostBuilder()
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .UseStartup<Startup>();
}
