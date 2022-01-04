// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Wasm.Authentication.Server;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseSetting(WebHostDefaults.ApplicationKey, typeof(Program).Assembly.GetName().Name);

                // We require this line because we run in Production environment
                // and static web assets are only on by default during development.
                webBuilder.UseStaticWebAssets();
                webBuilder.UseStartup<Startup>();
            });
}
