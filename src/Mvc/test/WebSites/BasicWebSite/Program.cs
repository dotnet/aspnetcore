// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;

namespace BasicWebSite;

public class Program
{
    public static void Main(string[] args)
    {
#pragma warning disable ASPDEPR008 // Type or member is obsolete
        using var host = CreateWebHostBuilder(args).Build();
#pragma warning restore ASPDEPR008 // Type or member is obsolete
        host.Run();
    }

    // Using WebHostBuilder to keep some test coverage in WebApplicationFactory tests
    public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
#pragma warning disable ASPDEPR004 // Type or member is obsolete
        new WebHostBuilder()
#pragma warning restore ASPDEPR004 // Type or member is obsolete
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseStartup<StartupWithoutEndpointRouting>()
            .UseKestrel()
            .UseIISIntegration();
}

public class TestService
{
    public string Message { get; set; }
}
