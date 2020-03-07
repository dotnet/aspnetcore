using System;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.WebSockets.ConformanceTest.Autobahn;

namespace Microsoft.AspNetCore.WebSockets.ConformanceTest
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SkipIfWsTestNotPresentAttribute : Attribute, ITestCondition
    {
        public bool IsMet => IsOnCi || Wstest.Default != null;
        public string SkipReason => "Autobahn Test Suite is not installed on the host machine.";

        private static bool IsOnCi =>
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMCITY_VERSION")) ||
            string.Equals(Environment.GetEnvironmentVariable("TRAVIS"), "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Environment.GetEnvironmentVariable("APPVEYOR"), "true", StringComparison.OrdinalIgnoreCase);
    }
}
