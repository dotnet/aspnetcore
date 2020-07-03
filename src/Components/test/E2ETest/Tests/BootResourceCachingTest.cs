// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HostedInAspNet.Server;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class BootResourceCachingTest
        : ServerTestBase<AspNetSiteServerFixture>
    {
        // The cache name is derived from the application's base href value (in this case, '/')
        private const string CacheName = "blazor-resources-/";

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
        [QuarantinedTest]
        public void CachesResourcesAfterFirstLoad()
        {
            // On the first load, we have to fetch everything
            Navigate("/");
            WaitUntilLoaded();
            var initialResourcesRequested = GetAndClearRequestedPaths();
            Assert.NotEmpty(initialResourcesRequested.Where(path => path.EndsWith("/blazor.boot.json")));
            Assert.NotEmpty(initialResourcesRequested.Where(path => path.EndsWith("/dotnet.wasm")));
            Assert.NotEmpty(initialResourcesRequested.Where(path => path.EndsWith(".js")));
            Assert.NotEmpty(initialResourcesRequested.Where(path => path.EndsWith(".dll")));

            // On subsequent loads, we skip the items referenced from blazor.boot.json
            // which includes .dll files and dotnet.wasm
            Navigate("about:blank");
            Navigate("/");
            WaitUntilLoaded();
            var subsequentResourcesRequested = GetAndClearRequestedPaths();
            Assert.NotEmpty(initialResourcesRequested.Where(path => path.EndsWith("/blazor.boot.json")));
            Assert.Empty(subsequentResourcesRequested.Where(path => path.EndsWith("/dotnet.wasm")));
            Assert.NotEmpty(subsequentResourcesRequested.Where(path => path.EndsWith(".js")));
            Assert.Empty(subsequentResourcesRequested.Where(path => path.EndsWith(".dll")));
        }

        [Fact]
        [QuarantinedTest]
        public void IncrementallyUpdatesCache()
        {
            // Perform a first load to populate the cache
            Navigate("/");
            WaitUntilLoaded();
            var cacheEntryUrls1 = GetCacheEntryUrls();
            var cacheEntryForMsCorLib = cacheEntryUrls1.Single(url => url.Contains("/mscorlib.dll"));
            var cacheEntryForDotNetWasm = cacheEntryUrls1.Single(url => url.Contains("/dotnet.wasm"));
            var cacheEntryForDotNetWasmWithChangedHash = cacheEntryForDotNetWasm.Replace(".sha256-", ".sha256-different");

            // Remove some items we do need, and add an item we don't need
            RemoveCacheEntry(cacheEntryForMsCorLib);
            RemoveCacheEntry(cacheEntryForDotNetWasm);
            AddCacheEntry(cacheEntryForDotNetWasmWithChangedHash, "ignored content");
            var cacheEntryUrls2 = GetCacheEntryUrls();
            Assert.DoesNotContain(cacheEntryForMsCorLib, cacheEntryUrls2);
            Assert.DoesNotContain(cacheEntryForDotNetWasm, cacheEntryUrls2);
            Assert.Contains(cacheEntryForDotNetWasmWithChangedHash, cacheEntryUrls2);

            // On the next load, we'll fetch only the items we need (not things already cached)
            GetAndClearRequestedPaths();
            Navigate("about:blank");
            Navigate("/");
            WaitUntilLoaded();
            var subsequentResourcesRequested = GetAndClearRequestedPaths();
            Assert.Collection(subsequentResourcesRequested.Where(url => url.Contains(".dll")),
                requestedDll => Assert.Contains("/mscorlib.dll", requestedDll));
            Assert.Collection(subsequentResourcesRequested.Where(url => url.Contains(".wasm")),
                requestedDll => Assert.Contains("/dotnet.wasm", requestedDll));

            // We also update the cache (add new items, remove unnecessary items)
            var cacheEntryUrls3 = GetCacheEntryUrls();
            Assert.Contains(cacheEntryForMsCorLib, cacheEntryUrls3);
            Assert.Contains(cacheEntryForDotNetWasm, cacheEntryUrls3);
            Assert.DoesNotContain(cacheEntryForDotNetWasmWithChangedHash, cacheEntryUrls3);
        }

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
            new WebDriverWait(Browser, TimeSpan.FromSeconds(30)).Until(
                driver => driver.FindElement(By.TagName("h1")).Text == "Hello, world!");
        }
    }
}
