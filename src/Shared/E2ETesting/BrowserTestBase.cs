// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.E2ETesting
{
    [CaptureSeleniumLogs]
    public class BrowserTestBase : IClassFixture<BrowserFixture>, IAsyncLifetime
    {
        private static readonly AsyncLocal<ILogs> _logs = new();
        private static readonly AsyncLocal<ITestOutputHelper> _output = new();

        private ExceptionDispatchInfo _exceptionDispatchInfo;
        private IWebDriver _browser;

        public BrowserTestBase(BrowserFixture browserFixture, ITestOutputHelper output)
        {
            BrowserFixture = browserFixture;
            _output.Value = output;
        }

        public IWebDriver Browser
        {
            get
            {
                if (_exceptionDispatchInfo != null)
                {
                    _exceptionDispatchInfo.Throw();
                    throw _exceptionDispatchInfo.SourceException;
                }

                return _browser;
            }
            set
            {
                _browser = value;
            }
        }

        public static ILogs Logs => _logs.Value;

        public static ITestOutputHelper Output => _output.Value;

        public BrowserFixture BrowserFixture { get; }

        public Task DisposeAsync()
        {
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
            try
            {
                var (browser, logs) = await BrowserFixture.GetOrCreateBrowserAsync(Output, isolationContext);
                _logs.Value = logs;

                Browser = browser;
            }
            catch (Exception ex)
            {
                _exceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
                throw;
            }
        }
    }
}
