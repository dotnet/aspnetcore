// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using HostedInAspNet.Server;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

// Disabling parallelism for these tests because of flakiness
[CollectionDefinition(nameof(BootResourceCachingTest), DisableParallelization = true)]
[Collection(nameof(BootResourceCachingTest))]
public partial class BootResourceCachingTest
    : ServerTestBase<AspNetSiteServerFixture>
{
    public BootResourceCachingTest(
        BrowserFixture browserFixture,
        AspNetSiteServerFixture serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        serverFixture.BuildWebHostMethod = Program.BuildWebHost;
    }

    public override Task InitializeAsync()
    {
        return base.InitializeAsync(Guid.NewGuid().ToString());
    }

    [Fact]
    public void CachesResourcesAfterFirstLoad()
    {
        // On the first load, we have to fetch everything
        Navigate("/");
        WaitUntilLoaded();
        var initialResourcesRequested = GetAndClearRequestedPaths();
        Assert.NotEmpty(initialResourcesRequested.Where(path =>
            path.Contains("/dotnet.native.", StringComparison.Ordinal) &&
            path.EndsWith(".wasm", StringComparison.Ordinal)));
        Assert.NotEmpty(initialResourcesRequested.Where(path => path.EndsWith(".js", StringComparison.Ordinal)));
        Assert.NotEmpty(initialResourcesRequested.Where(path =>
            !path.Contains("/dotnet.native.", StringComparison.Ordinal) &&
            path.EndsWith(".wasm", StringComparison.Ordinal)));

        Navigate("about:blank");
        Browser.Equal(string.Empty, () => Browser.Title);
        Navigate("/");
        WaitUntilLoaded();
        var subsequentResourcesRequested = GetAndClearRequestedPaths();
        Assert.Empty(subsequentResourcesRequested);
    }

    private IReadOnlyCollection<string> GetAndClearRequestedPaths()
    {
        var requestLog = _serverFixture.Host.Services.GetRequiredService<BootResourceRequestLog>();
        var result = requestLog.RequestPathsWithNewContent.ToList();
        requestLog.Clear();
        return result;
    }

    private void WaitUntilLoaded()
    {
        var element = Browser.Exists(By.TagName("h1"), TimeSpan.FromSeconds(30));
        Browser.Equal("Hello, world!", () => element.Text);
    }
}
