// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Templates.Test.Helpers;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using Xunit.Abstractions;

namespace Templates.Test.Infrastructure
{
    public class BrowserFixture : IDisposable
    {
        public IWebDriver Browser { get; }

        public ILogs Logs { get; }

        public ITestOutputHelper Output { get; set; }

        public BrowserFixture()
        {
            if(WebDriverFactory.HostSupportsBrowserAutomation)
            {
                var opts = new ChromeOptions();
                opts.AcceptInsecureCertificates = true;

                // Comment this out if you want to watch or interact with the browser (e.g., for debugging)
                opts.AddArgument("--headless");

                // Log errors
                opts.SetLoggingPreference(LogType.Browser, LogLevel.All);

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
                    var driver = new RemoteWebDriver(opts);
                    Browser = driver;
                    Logs = new RemoteLogs(driver);
                }
                catch (WebDriverException ex)
                {
                    var message =
                        "Failed to connect to the web driver. Please see the readme and follow the instructions to install selenium." +
                        "Remember to start the web driver with `selenium-standalone start` before running the end-to-end tests.";
                    throw new InvalidOperationException(message, ex);
                }
            }
        }

        public void Dispose()
        {
            if(Browser != null)
            {
                Browser.Dispose();
            }
        }
    }
}
