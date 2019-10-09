// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.E2ETesting
{
    [CaptureSeleniumLogs]
    public class BrowserTestBase : IClassFixture<BrowserFixture>, IAsyncLifetime
    {
        private static readonly AsyncLocal<IWebDriver> _asyncBrowser = new AsyncLocal<IWebDriver>();
        private static readonly AsyncLocal<ILogs> _logs = new AsyncLocal<ILogs>();
        private static readonly AsyncLocal<ITestOutputHelper> _output = new AsyncLocal<ITestOutputHelper>();

        // Limit the number of concurrent browser tests.
        private readonly static int MaxConcurrentBrowsers = Environment.ProcessorCount * 2;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(MaxConcurrentBrowsers);
        private bool _semaphoreHeld;

        public BrowserTestBase(BrowserFixture browserFixture, ITestOutputHelper output)
        {
            BrowserFixture = browserFixture;
            _output.Value = output;
        }

        public IWebDriver Browser { get; set; }

        public static IWebDriver BrowserAccessor => _asyncBrowser.Value;

        public static ILogs Logs => _logs.Value;

        public static ITestOutputHelper Output => _output.Value;

        public BrowserFixture BrowserFixture { get; }

        public Task DisposeAsync()
        {
            if (_semaphoreHeld)
            {
                _semaphore.Release();
            }

            return Task.CompletedTask;
        }

        public virtual Task InitializeAsync()
        {
            return InitializeAsync("");
        }

        public virtual async Task InitializeAsync(string isolationContext)
        {
            await InitializeBrowser(isolationContext);

            InitializeAsyncCore();
        }

        protected virtual void InitializeAsyncCore()
        {
        }

        protected async Task InitializeBrowser(string isolationContext)
        {
            await _semaphore.WaitAsync(TimeSpan.FromMinutes(30));
            _semaphoreHeld = true;

            var (browser, logs) = await BrowserFixture.GetOrCreateBrowserAsync(Output, isolationContext);
            _asyncBrowser.Value = browser;
            _logs.Value = logs;

            Browser = browser;
        }
    }
}
