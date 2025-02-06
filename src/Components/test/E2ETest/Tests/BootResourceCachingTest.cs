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
    // The cache name is derived from the application's base href value (in this case, '/')
    private const string CacheName = "dotnet-resources-/";

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
        Assert.NotEmpty(initialResourcesRequested.Where(path => path.EndsWith("/blazor.boot.json", StringComparison.Ordinal)));
        Assert.NotEmpty(initialResourcesRequested.Where(path =>
            path.Contains("/dotnet.native.", StringComparison.Ordinal) &&
            path.EndsWith(".wasm", StringComparison.Ordinal)));
        Assert.NotEmpty(initialResourcesRequested.Where(path => path.EndsWith(".js", StringComparison.Ordinal)));
        Assert.NotEmpty(initialResourcesRequested.Where(path =>
            !path.Contains("/dotnet.native.", StringComparison.Ordinal) &&
            path.EndsWith(".wasm", StringComparison.Ordinal)));

        // On subsequent loads, we skip the items referenced from blazor.boot.json
        // which includes .wasm (original .dll) files and dotnet.native.[fingerprint].wasm
        Navigate("about:blank");
        Browser.Equal(string.Empty, () => Browser.Title);
        Navigate("/");
        WaitUntilLoaded();
        var subsequentResourcesRequested = GetAndClearRequestedPaths();
        Assert.NotEmpty(initialResourcesRequested.Where(path => path.EndsWith("/blazor.boot.json", StringComparison.Ordinal)));
        Assert.DoesNotContain(subsequentResourcesRequested, path =>
            path.Contains("/dotnet.native.", StringComparison.Ordinal) &&
            path.EndsWith(".wasm", StringComparison.Ordinal));
        Assert.NotEmpty(subsequentResourcesRequested.Where(path => path.EndsWith(".js", StringComparison.Ordinal)));
        Assert.DoesNotContain(subsequentResourcesRequested, path =>
            !path.Contains("/dotnet.native.", StringComparison.Ordinal) &&
            path.EndsWith(".wasm", StringComparison.Ordinal));
    }

    [Fact]
    public async Task IncrementallyUpdatesCache()
    {
        // Perform a first load to populate the cache
        Navigate("/");
        WaitUntilLoaded();
        var cacheEntryUrls1 = GetCacheEntryUrls();
        var cacheEntryForComponentsDll = cacheEntryUrls1.Single(IsFingerprintedComponentsEntry);
        var cacheEntryForDotNetWasm = cacheEntryUrls1.Single(IsFingerprintedDotNetWasmEntry);
        var cacheEntryForDotNetWasmWithChangedHash = cacheEntryForDotNetWasm.Replace(".sha256-", ".sha256-different");

        // Remove some items we do need, and add an item we don't need
        RemoveCacheEntry(cacheEntryForComponentsDll);
        RemoveCacheEntry(cacheEntryForDotNetWasm);
        AddCacheEntry(cacheEntryForDotNetWasmWithChangedHash, "ignored content");
        var cacheEntryUrls2 = GetCacheEntryUrls();
        Assert.DoesNotContain(cacheEntryForComponentsDll, cacheEntryUrls2);
        Assert.DoesNotContain(cacheEntryForDotNetWasm, cacheEntryUrls2);
        Assert.Contains(cacheEntryForDotNetWasmWithChangedHash, cacheEntryUrls2);

        // On the next load, we'll fetch only the items we need (not things already cached)
        GetAndClearRequestedPaths();
        Navigate("about:blank");
        Browser.Equal(string.Empty, () => Browser.Title);
        Navigate("/");
        WaitUntilLoaded();
        var subsequentResourcesRequested = GetAndClearRequestedPaths();
        Assert.Collection(subsequentResourcesRequested.Where(url => url.Contains(".wasm")),
            requestedDll => Assert.True(IsFingerprintedComponentsEntry(requestedDll)),
            requestedDll => Assert.True(IsFingerprintedDotNetWasmEntry(requestedDll)));

        var cacheEntryUrls3 = GetCacheEntryUrls();
        // wait until the cache was cleaned, max 500ms
        for (var i = 0; i < 5; i++)
        {
            if (!cacheEntryUrls3.Contains(cacheEntryForDotNetWasmWithChangedHash))
            {
                break;
            }
            await Task.Delay(100); // wait for cache purge
            cacheEntryUrls3 = GetCacheEntryUrls();
        }
        Assert.Contains(cacheEntryForComponentsDll, cacheEntryUrls3);
        Assert.Contains(cacheEntryForDotNetWasm, cacheEntryUrls3);
        Assert.DoesNotContain(cacheEntryForDotNetWasmWithChangedHash, cacheEntryUrls3);
    }

    [GeneratedRegex("/Microsoft\\.AspNetCore\\.Components\\.\\w*\\.wasm")]
    private static partial Regex GetFingerprintedComponentsEntryRegex { get; }

    [GeneratedRegex("/dotnet\\.native\\.\\w*\\.wasm")]
    private static partial Regex GetFingerprintedDotNetWasmEntryRegex { get; }

    private static bool IsFingerprintedComponentsEntry(string url)
        => GetFingerprintedComponentsEntryRegex.IsMatch(url);

    private static bool IsFingerprintedDotNetWasmEntry(string url)
        => GetFingerprintedDotNetWasmEntryRegex.IsMatch(url);

    private IReadOnlyCollection<string> GetCacheEntryUrls()
    {
        var js = @"
                (async function(cacheName, completedCallback) {
                    const cache = await caches.open(cacheName);
                    const keys = await cache.keys();
                    const urls = keys.map(r => r.url);
                    completedCallback(urls);
                }).apply(null, arguments)";
        var jsExecutor = (IJavaScriptExecutor)Browser;
        var result = (IEnumerable<object>)jsExecutor.ExecuteAsyncScript(js, CacheName);
        return result.Cast<string>().ToList();
    }

    private void RemoveCacheEntry(string url)
    {
        var js = @"
                (async function(cacheName, urlToRemove, completedCallback) {
                    const cache = await caches.open(cacheName);
                    await cache.delete(urlToRemove);
                    completedCallback();
                }).apply(null, arguments)";
        ((IJavaScriptExecutor)Browser).ExecuteAsyncScript(js, CacheName, url);
    }

    private void AddCacheEntry(string url, string content)
    {
        var js = @"
                (async function(cacheName, urlToAdd, contentToAdd, completedCallback) {
                    const cache = await caches.open(cacheName);
                    await cache.put(urlToAdd, new Response(contentToAdd));
                    completedCallback();
                }).apply(null, arguments)";
        ((IJavaScriptExecutor)Browser).ExecuteAsyncScript(js, CacheName, url, content);
    }

    private IReadOnlyCollection<string> GetAndClearRequestedPaths()
    {
        var requestLog = _serverFixture.Host.Services.GetRequiredService<BootResourceRequestLog>();
        var result = requestLog.RequestPaths.ToList();
        requestLog.Clear();
        return result;
    }

    private void WaitUntilLoaded()
    {
        var element = Browser.Exists(By.TagName("h1"), TimeSpan.FromSeconds(30));
        Browser.Equal("Hello, world!", () => element.Text);
    }
}
