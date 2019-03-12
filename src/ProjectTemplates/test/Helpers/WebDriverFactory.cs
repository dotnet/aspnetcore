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
    }
}
