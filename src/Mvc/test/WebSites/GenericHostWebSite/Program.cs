// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace GenericHostWebSite;

public class Program
{
    public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

    // Do not change. This is the pattern our test infrastructure uses to initialize a IHostBuilder from
    // a users app.
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        new HostBuilder()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseStartup<Startup>()
                    .UseKestrel()
                    .UseIISIntegration();
            });
}

public class TestGenericService
{
    public string Message { get; set; }
}
