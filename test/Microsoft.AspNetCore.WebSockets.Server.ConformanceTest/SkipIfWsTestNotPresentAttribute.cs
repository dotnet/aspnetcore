using System;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.AspNetCore.WebSockets.Server.Test.Autobahn;

namespace Microsoft.AspNetCore.WebSockets.Server.Test
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SkipIfWsTestNotPresentAttribute : Attribute, ITestCondition
    {
        public bool IsMet => Wstest.Default != null;
        public string SkipReason => "Autobahn Test Suite is not installed on the host machine.";
    }
}