using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;

namespace Templates.Test.Helpers
{
    public static class WebDriverFactory
    {
        // Maximum time any action performed by WebDriver will wait before failing.
        // Any action will have to be completed in at most 10 seconds.
        // Providing a smaller value won't improve the speed of the tests in any
        // significant way and will make them more prone to fail on slower drivers.
        private const int DefaultMaxWaitTimeInSeconds = 10;

        public static bool HostSupportsBrowserAutomation
            => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_BROWSER_AUTOMATION_DISABLED")) &&
               (IsAppVeyor || (IsVSTS && RuntimeInformation.OSDescription.Contains("Microsoft Windows")) || OSSupportsEdge());

        private static bool IsAppVeyor
            => Environment.GetEnvironmentVariables().Contains("APPVEYOR");

        private static bool IsVSTS
            => Environment.GetEnvironmentVariables().Contains("TF_BUILD");

        public static IWebDriver CreateWebDriver()
        {
            // Where possible, it's preferable to use Edge because it's
            // far faster to automate than Chrome/Firefox. But on AppVeyor
            // only Firefox is available and VSTS doesn't have Edge.
            var result = (IsAppVeyor || IsVSTS || UseFirefox()) ? CreateFirefoxDriver() : CreateEdgeDriver();
            result.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(DefaultMaxWaitTimeInSeconds);
            return result;

            bool UseFirefox() => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_BROWSER_AUTOMATION_FIREFOX"));
        }

        private static IWebDriver CreateEdgeDriver()
            => new EdgeDriver(EdgeDriverService.CreateDefaultService(BinDir));

        private static IWebDriver CreateFirefoxDriver()
            => new FirefoxDriver(
                FirefoxDriverService.CreateDefaultService(BinDir),
                new FirefoxOptions()
                {
                    AcceptInsecureCertificates = true
                },
                TimeSpan.FromSeconds(DefaultMaxWaitTimeInSeconds));

        private static string BinDir
            => Path.GetDirectoryName(typeof(WebDriverFactory).Assembly.Location);

        private static int GetWindowsVersion()
        {
            var osDescription = RuntimeInformation.OSDescription;
            var windowsVersion = Regex.Match(osDescription, "^Microsoft Windows (\\d+)\\..*");
            return windowsVersion.Success ? int.Parse(windowsVersion.Groups[1].Value) : -1;
        }

        private static bool OSSupportsEdge()
        {
            var windowsVersion = GetWindowsVersion();
            return (windowsVersion >= DefaultMaxWaitTimeInSeconds && windowsVersion < 2000)
                || (windowsVersion >= 2016);
        }
    }
}
