// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.E2ETesting
{
    public class BrowserFixture : IDisposable
    {
        public BrowserFixture(IMessageSink diagnosticsMessageSink)
        {
            DiagnosticsMessageSink = diagnosticsMessageSink;

            if (!IsHostAutomationSupported())
            {
                DiagnosticsMessageSink.OnMessage(new DiagnosticMessage("Host does not support browser automation."));
                return;
            }

            var opts = new ChromeOptions();

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
                DiagnosticsMessageSink.OnMessage(new DiagnosticMessage($"Set {nameof(ChromeOptions)}.{nameof(opts.BinaryLocation)} to {binaryLocation}"));
            }

            var driver = new RemoteWebDriver(SeleniumStandaloneServer.Instance.Uri, opts);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1);
            Browser = driver;
            Logs = new RemoteLogs(driver);
        }

        public IWebDriver Browser { get; }

        public ILogs Logs { get; }

        public IMessageSink DiagnosticsMessageSink { get; }

        public static bool IsHostAutomationSupported()
        {
            // We emit an assemblymetadata attribute that reflects the value of SeleniumE2ETestsSupported at build
            // time and we use that to conditionally skip Selenium tests parts.
            var attribute = typeof(BrowserFixture).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .SingleOrDefault(a => a.Key == "Microsoft.AspNetCore.Testing.Selenium.Supported");
            var attributeValue = attribute != null ? bool.Parse(attribute.Value) : false;

            // The environment variable below can be set up before running the tests so as to override the default
            // value provided in the attribute.
            var environmentOverride = Environment
                .GetEnvironmentVariable("MICROSOFT_ASPNETCORE_TESTING_SELENIUM_SUPPORTED");
            var environmentOverrideValue = !string.IsNullOrWhiteSpace(environmentOverride) ? bool.Parse(attribute.Value) : false;

            if (environmentOverride != null)
            {
                return environmentOverrideValue;
            }
            else
            {
                return attributeValue;
            }
        }

        public void Dispose()
        {
            if (Browser != null)
            {
                Browser.Dispose();
            }
        }
    }
}
