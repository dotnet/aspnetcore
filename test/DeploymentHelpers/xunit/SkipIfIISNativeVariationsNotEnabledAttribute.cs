using System;
using Microsoft.AspNet.Testing.xunit;

namespace DeploymentHelpers
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SkipIfIISNativeVariationsNotEnabledAttribute : Attribute, ITestCondition
    {
        public bool IsMet
        {
            get
            {
                return Environment.GetEnvironmentVariable("IIS_NATIVE_VARIATIONS_ENABLED") == "true";
            }
        }

        public string SkipReason
        {
            get
            {
                return "Skipping Native module test since native module variations are not enabled. " +
                        "To run the test, setup the native module and set the environment variable IIS_NATIVE_VARIATIONS_ENABLED=true.";
            }
        }
    }
}