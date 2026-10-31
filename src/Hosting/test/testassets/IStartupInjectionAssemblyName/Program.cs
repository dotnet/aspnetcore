// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IStartupInjectionAssemblyName;

public class Program
{
    public static void Main(string[] args)
    {
#pragma warning disable ASPDEPR008 // IWebHost is obsolete
        var webHost = CreateWebHostBuilder(args).Build();
#pragma warning restore ASPDEPR008 // IWebHost is obsolete
        var applicationName = webHost.Services.GetRequiredService<IHostEnvironment>().ApplicationName;
        Console.WriteLine(applicationName);
    }

    // Do not change the signature of this method. It's used for tests.
    private static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
#pragma warning disable ASPDEPR004 // Type or member is obsolete
        new WebHostBuilder()
        .SuppressStatusMessages(true)
        .ConfigureServices(services => services.AddSingleton<IStartup, Startup>());
#pragma warning restore ASPDEPR004 // Type or member is obsolete
}
