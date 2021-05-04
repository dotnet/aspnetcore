// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Safari;
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

        public string UserProfileDir { get; private set; }

        public static void EnforceSupportedConfigurations()
        {
            // Do not change the current platform support without explicit approval.
            Assert.False(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && RuntimeInformation.ProcessArchitecture == Architecture.X64,
                "Selenium tests should be running in this platform.");
        }

        public static bool IsHostAutomationSupported()
        {
            return true;
            //// We emit an assemblymetadata attribute that reflects the value of SeleniumE2ETestsSupported at build
            //// time and we use that to conditionally skip Selenium tests parts.
            //var attribute = typeof(BrowserFixture).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            //    .SingleOrDefault(a => a.Key == "Microsoft.AspNetCore.Testing.Selenium.Supported");
            //var attributeValue = attribute != null ? bool.Parse(attribute.Value) : false;

            //// The environment variable below can be set up before running the tests so as to override the default
            //// value provided in the attribute.
            //var environmentOverride = Environment
            //    .GetEnvironmentVariable("MICROSOFT_ASPNETCORE_TESTING_SELENIUM_SUPPORTED");
            //var environmentOverrideValue = !string.IsNullOrWhiteSpace(environmentOverride) ? bool.Parse(attribute.Value) : false;

            //if (environmentOverride != null)
            //{
            //    return environmentOverrideValue;
            //}
            //else
            //{
            //    return attributeValue;
            //}
        }

        public async Task DisposeAsync()
        {
            var browsers = await Task.WhenAll(_browsers.Values);
            foreach (var (browser, log) in browsers)
            {
                browser?.Quit();
                browser?.Dispose();
            }

            await DeleteBrowserUserProfileDirectoriesAsync();
        }

        private async Task DeleteBrowserUserProfileDirectoriesAsync()
        {
            foreach (var context in _browsers.Keys)
            {
                var userProfileDirectory = UserProfileDirectory(context);
                if (!string.IsNullOrEmpty(userProfileDirectory) && Directory.Exists(userProfileDirectory))
                {
                    var attemptCount = 0;
                    while (true)
                    {
                        try
                        {
                            Directory.Delete(userProfileDirectory, recursive: true);
                            break;
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            attemptCount++;
                            if (attemptCount < 5)
                            {
                                Console.WriteLine($"Failed to delete browser profile directory '{userProfileDirectory}': '{ex}'. Will retry.");
                                await Task.Delay(2000);
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                }
            }
        }

        public Task<(IWebDriver, ILogs)> GetOrCreateBrowserAsync(ITestOutputHelper output, string isolationContext = "")
        {
            Func<string, ITestOutputHelper, Task<(IWebDriver, ILogs)>> createBrowserFunc;
            if (E2ETestOptions.Instance.SauceTest)
            {
                createBrowserFunc = CreateSauceBrowserAsync;
            }
            else
            {
                if (!IsHostAutomationSupported())
                {
                    output.WriteLine($"{nameof(BrowserFixture)}: Host does not support browser automation.");
                    return Task.FromResult<(IWebDriver, ILogs)>(default);
                }

                createBrowserFunc = CreateBrowserAsync;
            }


            return _browsers.GetOrAdd(isolationContext, createBrowserFunc, output);
        }

        public Task InitializeAsync() => Task.CompletedTask;

        private async Task<(IWebDriver browser, ILogs log)> CreateBrowserAsync(string context, ITestOutputHelper output)
        {
            var opts = new ChromeOptions();

            // Force language to english for tests
            opts.AddUserProfilePreference("intl.accept_languages", "en");

            if (!Debugger.IsAttached &&
                !string.Equals(Environment.GetEnvironmentVariable("E2E_TEST_VISIBLE"), "true", StringComparison.OrdinalIgnoreCase))
            {
                opts.AddArgument("--headless");
            }

            opts.AddArgument("--no-sandbox");
            opts.AddArgument("--ignore-certificate-errors");

            // Log errors
            opts.SetLoggingPreference(LogType.Browser, LogLevel.All);
            opts.SetLoggingPreference(LogType.Driver, LogLevel.All);

            // On Windows/Linux, we don't need to set opts.BinaryLocation
            // But for Travis Mac builds we do
            var binaryLocation = Environment.GetEnvironmentVariable("TEST_CHROME_BINARY");
            if (!string.IsNullOrEmpty(binaryLocation))
            {
                opts.BinaryLocation = binaryLocation;
                output.WriteLine($"Set {nameof(ChromeOptions)}.{nameof(opts.BinaryLocation)} to {binaryLocation}");
            }

            var userProfileDirectory = UserProfileDirectory(context);
            UserProfileDir = userProfileDirectory;
            if (!string.IsNullOrEmpty(userProfileDirectory))
            {
                Directory.CreateDirectory(userProfileDirectory);
                opts.AddArgument($"--user-data-dir={userProfileDirectory}");
                opts.AddUserProfilePreference("download.default_directory", Path.Combine(userProfileDirectory, "Downloads"));
            }

            var instance = await SeleniumStandaloneServer.GetInstanceAsync(output);

            var attempt = 0;
            const int maxAttempts = 3;
            Exception innerException;
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
                    var driver = new RemoteWebDriverWithLogs(
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
                    innerException = ex;
                }

                attempt++;

            } while (attempt < maxAttempts);

            throw new InvalidOperationException("Couldn't create a Selenium remote driver client. The server is irresponsive", innerException);
        }

        private string UserProfileDirectory(string context)
        {
            if (string.IsNullOrEmpty(context))
            {
                return null;
            }

            return Path.Combine(Path.GetTempPath(), "BrowserFixtureUserProfiles", context);
        }

        private async Task<(IWebDriver browser, ILogs log)> CreateSauceBrowserAsync(string context, ITestOutputHelper output)
        {
            var sauce = E2ETestOptions.Instance.Sauce;

            if (sauce == null ||
                string.IsNullOrEmpty(sauce.TestName) ||
                string.IsNullOrEmpty(sauce.Username) ||
                string.IsNullOrEmpty(sauce.AccessKey) ||
                string.IsNullOrEmpty(sauce.TunnelIdentifier) ||
                string.IsNullOrEmpty(sauce.PlatformName) ||
                string.IsNullOrEmpty(sauce.BrowserName))
            {
                throw new InvalidOperationException("Required SauceLabs environment variables not set.");
            }

            var name = sauce.TestName;
            if (!string.IsNullOrEmpty(context))
            {
                name = $"{name} - {context}";
            }

            DriverOptions options;

            switch (sauce.BrowserName.ToLowerInvariant())
            {
                case "chrome":
                    options = new ChromeOptions();
                    break;
                case "safari":
                    options = new SafariOptions();
                    break;
                case "internet explorer":
                    options = new InternetExplorerOptions();
                    break;
                case "microsoftedge":
                    options = new EdgeOptions();
                    break;
                default:
                    throw new InvalidOperationException($"Browser name {sauce.BrowserName} not recognized");
            }

            // Required config
            options.AddAdditionalOption("username", sauce.Username);
            options.AddAdditionalOption("accessKey", sauce.AccessKey);
            options.AddAdditionalOption("tunnelIdentifier", sauce.TunnelIdentifier);
            options.AddAdditionalOption("name", name);

            if (!string.IsNullOrEmpty(sauce.BrowserName))
            {
                options.AddAdditionalOption("browserName", sauce.BrowserName);
            }

            if (!string.IsNullOrEmpty(sauce.PlatformVersion))
            {
                options.PlatformName = sauce.PlatformName;
                options.AddAdditionalOption("platformVersion", sauce.PlatformVersion);
            }
            else
            {
                // In some cases (like macOS), SauceLabs expects us to set "platform" instead of "platformName".
                options.AddAdditionalOption("platform", sauce.PlatformName);
            }

            if (!string.IsNullOrEmpty(sauce.BrowserVersion))
            {
                options.BrowserVersion = sauce.BrowserVersion;
            }

            if (!string.IsNullOrEmpty(sauce.DeviceName))
            {
                options.AddAdditionalOption("deviceName", sauce.DeviceName);
            }

            if (!string.IsNullOrEmpty(sauce.DeviceOrientation))
            {
                options.AddAdditionalOption("deviceOrientation", sauce.DeviceOrientation);
            }

            if (!string.IsNullOrEmpty(sauce.AppiumVersion))
            {
                options.AddAdditionalOption("appiumVersion", sauce.AppiumVersion);
            }

            if (!string.IsNullOrEmpty(sauce.SeleniumVersion))
            {
                options.AddAdditionalOption("seleniumVersion", sauce.SeleniumVersion);
            }

            var capabilities = options.ToCapabilities();

            await SauceConnectServer.StartAsync(output);

            var attempt = 0;
            const int maxAttempts = 3;
            do
            {
                try
                {
                    // Attempt to create a new browser in SauceLabs.
                    var driver = new RemoteWebDriver(
                        new Uri("http://localhost:4445/wd/hub"),
                        capabilities,
                        TimeSpan.FromSeconds(60).Add(TimeSpan.FromSeconds(attempt * 60)));

                    // Make sure implicit waits are disabled as they don't mix well with explicit waiting
                    // see https://www.selenium.dev/documentation/en/webdriver/waits/#implicit-wait
                    driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
                    var logs = new RemoteLogs(driver);

                    return (driver, logs);
                }
                catch (Exception ex)
                {
                    output.WriteLine($"Error initializing RemoteWebDriver: {ex.Message}");
                }

                attempt++;

            } while (attempt < maxAttempts);

            throw new InvalidOperationException("Couldn't create a SauceLabs remote driver client.");
        }

        // This is a workaround for https://github.com/SeleniumHQ/selenium/issues/8229
        private class RemoteWebDriverWithLogs : RemoteWebDriver, ISupportsLogs
        {
            public RemoteWebDriverWithLogs(Uri remoteAddress, ICapabilities desiredCapabilities, TimeSpan commandTimeout)
                : base(remoteAddress, desiredCapabilities, commandTimeout)
            {
            }
        }
    }
}
