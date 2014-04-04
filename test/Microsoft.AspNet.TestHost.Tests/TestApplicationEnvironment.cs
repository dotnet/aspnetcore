using System;
using System.Runtime.Versioning;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.TestHost.Tests
{
    public class TestApplicationEnvironment : IApplicationEnvironment
    {
        public string ApplicationName
        {
            get { return "Test App environment"; }
        }

        public string Version
        {
            get { return "1.0.0"; }
        }

        public string ApplicationBasePath
        {
            get { return Environment.CurrentDirectory; }
        }

        public FrameworkName TargetFramework
        {
            get { return new FrameworkName(".NETFramework", new Version(4, 5)); }
        }
    }
}
