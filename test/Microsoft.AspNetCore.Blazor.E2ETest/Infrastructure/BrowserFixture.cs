// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using System;

namespace Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure
{
    public class BrowserFixture : IDisposable
    {
        public IWebDriver Browser { get; }

        public BrowserFixture()
        {
            var opts = new ChromeOptions();
            opts.AddArgument("--headless");
            Browser = new RemoteWebDriver(opts);
        }

        public void Dispose()
        {
            Browser.Dispose();
        }
    }
}
