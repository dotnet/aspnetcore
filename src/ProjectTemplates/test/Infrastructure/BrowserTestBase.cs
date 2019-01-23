// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test.Infrastructure
{
    [CaptureSeleniumLogs]
    public class BrowserTestBase : TemplateTestBase, IClassFixture<BrowserFixture>
    {
        private static readonly AsyncLocal<IWebDriver> _browser = new AsyncLocal<IWebDriver>();
        private static readonly AsyncLocal<ILogs> _logs = new AsyncLocal<ILogs>();

        public static IWebDriver Browser => _browser.Value;

        public static ILogs Logs => _logs.Value;

        public BrowserTestBase(BrowserFixture browserFixture, ITestOutputHelper output) : base(output)
        {
            _browser.Value = browserFixture.Browser;
            _logs.Value = browserFixture.Logs;
        }
    }
}
