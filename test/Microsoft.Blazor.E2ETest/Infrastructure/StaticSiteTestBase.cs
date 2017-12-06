// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using OpenQA.Selenium;
using System;
using System.IO;
using Xunit;

namespace Microsoft.Blazor.E2ETest.Infrastructure
{
    public class StaticSiteTestBase
        : IClassFixture<BrowserFixture>, IClassFixture<StaticServerFixture>
    {
        public IWebDriver Browser { get; }

        private Uri _serverRootUri;

        public StaticSiteTestBase(
            BrowserFixture browserFixture,
            StaticServerFixture serverFixture,
            string sampleSiteName)
        {
            Browser = browserFixture.Browser;

            // Start a static files web server for the specified directory
            var serverRootUriString = serverFixture.StartAndGetUrl(sampleSiteName);
            _serverRootUri = new Uri(serverRootUriString);
        }

        public void Navigate(string relativeUrl)
        {
            var absoluteUrl = new Uri(_serverRootUri, relativeUrl);
            Browser.Navigate().GoToUrl(absoluteUrl);
        }
    }
}
