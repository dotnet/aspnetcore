using System;
using System.Diagnostics;
using Microsoft.AspNet.Testing.xunit;

namespace E2ETests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SkipIfCurrentRuntimeIsCoreClrAttribute : Attribute, ITestCondition
    {
        public bool IsMet
        {
            get
            {
                return !Process.GetCurrentProcess().ProcessName.ToLower().Contains("coreclr");
            }
        }

        public string SkipReason
        {
            get
            {
                return "Cannot run these test variations using CoreCLR DNX as helpers are not available on CoreCLR.";
            }
        }
    }
}
