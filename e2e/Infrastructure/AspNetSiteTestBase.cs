// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using OpenQA.Selenium;
using System;
using Xunit;

namespace Blazor.E2ETest.Infrastructure
{
    public class AspNetSiteTestBase<TStartup>
        : IClassFixture<BrowserFixture>, IClassFixture<AspNetServerFixture>
    {
        public IWebDriver Browser { get; }

        private Uri _serverRootUri;

        public AspNetSiteTestBase(
            BrowserFixture browserFixture,
            AspNetServerFixture serverFixture)
        {
            Browser = browserFixture.Browser;

            var serverRootUriString = serverFixture.StartAndGetUrl(typeof(TStartup));
            _serverRootUri = new Uri(serverRootUriString);
        }

        public void Navigate(string relativeUrl)
        {
            var absoluteUrl = new Uri(_serverRootUri, relativeUrl);
            Browser.Navigate().GoToUrl(absoluteUrl);
        }
    }
}
