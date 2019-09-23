// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure
{
    public abstract class ServerTestBase<TServerFixture>
        : BrowserTestBase,
        IClassFixture<TServerFixture>
        where TServerFixture: ServerFixture
    {
        public string ServerPathBase => "/subdir";

        protected readonly TServerFixture _serverFixture;

        public ServerTestBase(
            BrowserFixture browserFixture,
            TServerFixture serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, output)
        {
            _serverFixture = serverFixture;
        }

        public void Navigate(string relativeUrl, bool noReload = false)
        {
            Browser.Navigate(_serverFixture.RootUri, relativeUrl, noReload);
        }

        protected override void InitializeAsyncCore()
        {
            // Clear logs - we check these during tests in some cases.
            // Make sure each test starts clean.
            ((IJavaScriptExecutor)Browser).ExecuteScript("console.clear()");
        }
    }
}
