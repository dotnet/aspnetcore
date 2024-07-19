// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;

public class TrimmingServerFixture<TStartup> : BasicTestAppServerSiteFixture<TStartup> where TStartup : class
{
    public readonly bool TestTrimmedApps = typeof(ToggleExecutionModeServerFixture<>).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .First(m => m.Key == "Microsoft.AspNetCore.E2ETesting.TestTrimmedOrMultithreadingApps")
        .Value == "true";

    public TrimmingServerFixture()
    {
        if (TestTrimmedApps)
        {
            BuildWebHostMethod = BuildPublishedWebHost;
            GetContentRootMethod = GetPublishedContentRoot;
        }
    }

    private static IHost BuildPublishedWebHost(string[] args) =>
        Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .ConfigureLogging((ctx, lb) =>
            {
                var sink = new TestSink();
                lb.AddProvider(new TestLoggerProvider(sink));
                lb.Services.AddSingleton(sink);
            })
            .ConfigureWebHostDefaults(webHostBuilder =>
            {
                webHostBuilder.UseStartup<TStartup>();
                // Avoid UseStaticAssets or we won't use the trimmed published output.
            })
            .Build();

    private static string GetPublishedContentRoot(Assembly assembly)
    {
        var contentRoot = Path.Combine(AppContext.BaseDirectory, "trimmed-or-threading", assembly.GetName().Name);

        if (!Directory.Exists(contentRoot))
        {
            throw new DirectoryNotFoundException($"Test is configured to use trimmed outputs, but trimmed outputs were not found in {contentRoot}.");
        }

        return contentRoot;
    }
}
