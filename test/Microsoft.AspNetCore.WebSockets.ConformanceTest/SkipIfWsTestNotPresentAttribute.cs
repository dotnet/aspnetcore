using System;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.AspNetCore.WebSockets.ConformanceTest.Autobahn;

namespace Microsoft.AspNetCore.WebSockets.ConformanceTest
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SkipIfWsTestNotPresentAttribute : Attribute, ITestCondition
    {
        public bool IsMet => Wstest.Default != null;
        public string SkipReason => "Autobahn Test Suite is not installed on the host machine.";
    }
}