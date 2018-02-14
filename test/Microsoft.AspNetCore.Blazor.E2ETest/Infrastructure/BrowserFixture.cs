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

            // On Windows/Linux, we don't need to set opts.BinaryLocation
            // But for Travis Mac builds we do
            var binaryLocation = Environment.GetEnvironmentVariable("TEST_CHROME_BINARY");
            if (!string.IsNullOrEmpty(binaryLocation))
            {
                opts.BinaryLocation = binaryLocation;
                Console.WriteLine($"Set {nameof(ChromeOptions)}.{nameof(opts.BinaryLocation)} to {binaryLocation}");
            }

            try
            {
                Browser = new RemoteWebDriver(opts);
            }
            catch (WebDriverException ex)
            {
                var message =
                    "Failed to connect to the web driver. Please see the readme and follow the instructions to install selenium." +
                    "Remember to start the web driver with `selenium-standalone start` before running the end-to-end tests.";
                throw new InvalidOperationException(message, ex);
            }
        }

        public void Dispose()
        {
            Browser.Dispose();
        }
    }
}
