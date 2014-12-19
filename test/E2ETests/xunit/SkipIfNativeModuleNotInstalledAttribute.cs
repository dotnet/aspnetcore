using System;
using Microsoft.AspNet.Testing.xunit;

namespace E2ETests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SkipIfNativeModuleNotInstalledAttribute : Attribute, ITestCondition
    {
        public bool IsMet
        {
            get
            {
                // TODO: Need a better way to detect native module on the machine.
                return Environment.GetEnvironmentVariable("IIS_NATIVE_MODULE_SETUP") == "true";
            }
        }

        public string SkipReason
        {
            get
            {
                return "Skipping Native module test since native module is not installed on IIS. " +
                        "To run the test, setup the native module and set the environment variable IIS_NATIVE_MODULE_SETUP=true.";
            }
        }
    }
}