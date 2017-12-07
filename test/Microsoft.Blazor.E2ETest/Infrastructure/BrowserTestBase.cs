// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using OpenQA.Selenium;
using Xunit;

namespace Microsoft.Blazor.E2ETest.Infrastructure
{
    public class BrowserTestBase : IClassFixture<BrowserFixture>
    {
        public IWebDriver Browser { get; }

        public BrowserTestBase(BrowserFixture browserFixture)
        {
            Browser = browserFixture.Browser;
        }
    }
}
