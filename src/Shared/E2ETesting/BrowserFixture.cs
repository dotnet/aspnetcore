// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.E2ETesting
{
    public class BrowserFixture : IAsyncLifetime
    {
        private ConcurrentDictionary<string, Task<(IWebDriver browser, ILogs log)>> _browsers = new ConcurrentDictionary<string, Task<(IWebDriver, ILogs)>>();

        public BrowserFixture(IMessageSink diagnosticsMessageSink)
        {
            DiagnosticsMessageSink = diagnosticsMessageSink;
        }

        public ILogs Logs { get; private set; }

        public IMessageSink DiagnosticsMessageSink { get; }

        public static void EnforceSupportedConfigurations()
        {
            // Do not change the current platform support without explicit approval.
            Assert.False(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && RuntimeInformation.ProcessArchitecture == Architecture.X64,
                "Selenium tests should be running in this platform.");
        }

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

        public async Task DisposeAsync()
        {
            var browsers = await Task.WhenAll(_browsers.Values);
            foreach (var (browser, log) in browsers)
            {
                browser.Dispose();
            }
        }

        public Task<(IWebDriver, ILogs)> GetOrCreateBrowserAsync(ITestOutputHelper output, string isolationContext = "")
        {
            if (!IsHostAutomationSupported())
            {
                output.WriteLine($"{nameof(BrowserFixture)}: Host does not support browser automation.");
                return Task.FromResult<(IWebDriver, ILogs)>(default);
            }

            return _browsers.GetOrAdd(isolationContext, CreateBrowserAsync, output);
        }

        public Task InitializeAsync() => Task.CompletedTask;

        private async Task<(IWebDriver browser, ILogs log)> CreateBrowserAsync(string context, ITestOutputHelper output)
        {
            var opts = new ChromeOptions();

            // Comment this out if you want to watch or interact with the browser (e.g., for debugging)
            if (!Debugger.IsAttached)
            {
                opts.AddArgument("--headless");
            }

            // Log errors
            opts.SetLoggingPreference(LogType.Browser, LogLevel.All);

            // On Windows/Linux, we don't need to set opts.BinaryLocation
            // But for Travis Mac builds we do
            var binaryLocation = Environment.GetEnvironmentVariable("TEST_CHROME_BINARY");
            if (!string.IsNullOrEmpty(binaryLocation))
            {
                opts.BinaryLocation = binaryLocation;
                output.WriteLine($"Set {nameof(ChromeOptions)}.{nameof(opts.BinaryLocation)} to {binaryLocation}");
            }

            var instance = await SeleniumStandaloneServer.GetInstanceAsync(output);

            var attempt = 0;
            const int maxAttempts = 3;
            do
            {
                try
                {
                    // The driver opens the browser window and tries to connect to it on the constructor.
                    // Under heavy load, this can cause issues
                    // To prevent this we let the client attempt several times to connect to the server, increasing
                    // the max allowed timeout for a command on each attempt linearly.
                    // This can also be caused if many tests are running concurrently, we might want to manage
                    // chrome and chromedriver instances more aggressively if we have to.
                    // Additionally, if we think the selenium server has become irresponsive, we could spin up
                    // replace the current selenium server instance and let a new instance take over for the
                    // remaining tests.
                    var driver = new RemoteWebDriver(
                        instance.Uri,
                        opts.ToCapabilities(),
                        TimeSpan.FromSeconds(60).Add(TimeSpan.FromSeconds(attempt * 60)));

                    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1);
                    var logs = new RemoteLogs(driver);

                    return (driver, logs);
                }
                catch (Exception ex)
                {
                    output.WriteLine($"Error initializing RemoteWebDriver: {ex.Message}");
                }

                attempt++;

            } while (attempt < maxAttempts);

            throw new InvalidOperationException("Couldn't create a Selenium remote driver client. The server is irresponsive");
        }
    }
}
