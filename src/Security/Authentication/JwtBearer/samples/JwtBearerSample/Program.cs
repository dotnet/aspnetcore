// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace JwtBearerSample;

public static class Program
{
    public static Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webHostBuilder =>
            {
                webHostBuilder
                    .UseStartup<Startup>();
            })
            .Build();

        return host.RunAsync();
    }
}
