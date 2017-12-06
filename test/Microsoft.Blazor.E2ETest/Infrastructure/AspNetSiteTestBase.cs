// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using OpenQA.Selenium;
using System;
using Xunit;

namespace Microsoft.Blazor.E2ETest.Infrastructure
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

            if (!serverFixture.IsStarted)
            {
                serverFixture.Start(typeof(TStartup));
            }

            _serverRootUri = serverFixture.RootUri;
        }

        public void Navigate(string relativeUrl, bool noReload = false)
        {
            var absoluteUrl = new Uri(_serverRootUri, relativeUrl);

            if (noReload)
            {
                var existingUrl = Browser.Url;
                if (string.Equals(existingUrl, absoluteUrl.AbsoluteUri, StringComparison.Ordinal))
                {
                    return;
                }
            }

            Browser.Navigate().GoToUrl(absoluteUrl);
        }
    }
}
