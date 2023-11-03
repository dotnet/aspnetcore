// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.E2ETesting;

public class BrowserFixture : IAsyncLifetime
{
    public static string StreamingContext { get; } = "streaming";
    private readonly ConcurrentDictionary<string, (IWebDriver browser, ILogs log)> _browsers = new();

    public BrowserFixture(IMessageSink diagnosticsMessageSink)
    {
        DiagnosticsMessageSink = diagnosticsMessageSink;
    }

    public ILogs Logs { get; private set; }

    public IMessageSink DiagnosticsMessageSink { get; }

    public string UserProfileDir { get; private set; }

    public bool EnsureNotHeadless { get; set; }

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
            .SingleOrDefault(a => a.Key == "Microsoft.AspNetCore.InternalTesting.Selenium.Supported");
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
        var browsers = _browsers.Values;
        foreach (var (browser, _) in browsers)
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

    public (IWebDriver, ILogs) GetOrCreateBrowser(ITestOutputHelper output, string isolationContext = "")
    {
        if (!IsHostAutomationSupported())
        {
            output.WriteLine($"{nameof(BrowserFixture)}: Host does not support browser automation.");
            return default;
        }

        return _browsers.GetOrAdd(isolationContext, CreateBrowser, output);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    private (IWebDriver browser, ILogs log) CreateBrowser(string context, ITestOutputHelper output)
    {
        var opts = new ChromeOptions();

        if (context?.StartsWith(StreamingContext, StringComparison.Ordinal) == true)
        {
            // Tells Selenium not to wait until the page navigation has completed before continuing with the tests
            opts.PageLoadStrategy = PageLoadStrategy.None;
        }

        // Force language to english for tests
        opts.AddUserProfilePreference("intl.accept_languages", "en");

        if (!EnsureNotHeadless &&
            !Debugger.IsAttached &&
            !string.Equals(Environment.GetEnvironmentVariable("E2E_TEST_VISIBLE"), "true", StringComparison.OrdinalIgnoreCase))
        {
            opts.AddArgument("--headless=new");
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
                var driver = new ChromeDriver(
                    CreateChromeDriverService(output),
                    opts,
                    TimeSpan.FromSeconds(60).Add(TimeSpan.FromSeconds(attempt * 60)));

                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1);
                var logs = driver.Manage().Logs;

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

    private static ChromeDriverService CreateChromeDriverService(ITestOutputHelper output)
    {
        // In AzDO, the path to the system chromedriver is in an env var called CHROMEWEBDRIVER
        // We want to use this because it should match the installed browser version
        // If the env var is not set, then we fall back on allowing Selenium Manager to download
        // and use an up-to-date chromedriver
        var chromeDriverPathEnvVar = Environment.GetEnvironmentVariable("CHROMEWEBDRIVER");
        if (!string.IsNullOrEmpty(chromeDriverPathEnvVar))
        {
            output.WriteLine($"Using chromedriver at path {chromeDriverPathEnvVar}");
            return ChromeDriverService.CreateDefaultService(chromeDriverPathEnvVar);
        }
        else
        {
            output.WriteLine($"Using default chromedriver from Selenium Manager");
            return ChromeDriverService.CreateDefaultService();
        }
    }

    private static string UserProfileDirectory(string context)
    {
        if (string.IsNullOrEmpty(context))
        {
            return null;
        }

        return Path.Combine(Path.GetTempPath(), "BrowserFixtureUserProfiles", context);
    }
}
