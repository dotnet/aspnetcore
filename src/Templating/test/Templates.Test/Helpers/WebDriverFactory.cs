using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Templates.Test.Helpers
{
    public static class WebDriverFactory
    {
        // Maximum time any action performed by WebDriver will wait before failing.
        // Any action will have to be completed in at most 10 seconds.
        // Providing a smaller value won't improve the speed of the tests in any
        // significant way and will make them more prone to fail on slower drivers.
        internal const int DefaultMaxWaitTimeInSeconds = 10;

        public static bool HostSupportsBrowserAutomation
            => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_BROWSER_AUTOMATION_DISABLED")) &&
               (IsAppVeyor || (IsVSTS && RuntimeInformation.OSDescription.Contains("Microsoft Windows")) || OSSupportsEdge());

        private static bool IsAppVeyor
            => Environment.GetEnvironmentVariables().Contains("APPVEYOR");

        private static bool IsVSTS
            => Environment.GetEnvironmentVariables().Contains("TF_BUILD");

        private static int GetWindowsVersion()
        {
            var osDescription = RuntimeInformation.OSDescription;
            var windowsVersion = Regex.Match(osDescription, "^Microsoft Windows (\\d+)\\..*");
            return windowsVersion.Success ? int.Parse(windowsVersion.Groups[1].Value) : -1;
        }

        private static bool OSSupportsEdge()
        {
            var windowsVersion = GetWindowsVersion();
            return (windowsVersion >= 10 && windowsVersion < 2000)
                || (windowsVersion >= 2016);
        }
    }
}
