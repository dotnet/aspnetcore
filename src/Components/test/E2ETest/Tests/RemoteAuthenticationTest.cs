// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class RemoteAuthenticationTest :
    ServerTestBase<BasicTestAppServerSiteFixture<RemoteAuthenticationStartup>>
{
    public readonly bool TestTrimmedApps = typeof(ToggleExecutionModeServerFixture<>).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .First(m => m.Key == "Microsoft.AspNetCore.E2ETesting.TestTrimmedApps")
        .Value == "true";

    public RemoteAuthenticationTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RemoteAuthenticationStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        serverFixture.ApplicationAssembly = typeof(RemoteAuthenticationStartup).Assembly;

        if (TestTrimmedApps)
        {
            serverFixture.BuildWebHostMethod = BuildPublishedWebHost;
            serverFixture.GetContentRootMethod = GetPublishedContentRoot;
        }
    }

    [Fact]
    public void NavigateToLogin_PreservesExtraQueryParams()
    {
        // If the preservedExtraQueryParams passed to NavigateToLogin by RedirectToLogin gets trimmed,
        // the OIDC endpoints will fail to authenticate the user.
        Navigate("/subdir/test-remote-authentication");

        var heading = Browser.Exists(By.TagName("h1"));
        Browser.Equal("Hello, Jane Doe!", () => heading.Text);
    }

    private static IHost BuildPublishedWebHost(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging((ctx, lb) =>
            {
                TestSink sink = new TestSink();
                lb.AddProvider(new TestLoggerProvider(sink));
                lb.Services.AddSingleton(sink);
            })
            .ConfigureWebHostDefaults(webHostBuilder =>
            {
                webHostBuilder.UseStartup<RemoteAuthenticationStartup>();
                // Avoid UseStaticAssets or we won't use the trimmed published output.
            })
            .Build();

    private static string GetPublishedContentRoot(Assembly assembly)
    {
        var contentRoot = Path.Combine(AppContext.BaseDirectory, "trimmed", assembly.GetName().Name);

        if (!Directory.Exists(contentRoot))
        {
            throw new DirectoryNotFoundException($"Test is configured to use trimmed outputs, but trimmed outputs were not found in {contentRoot}.");
        }

        return contentRoot;
    }
}
