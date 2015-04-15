using System;
using Microsoft.AspNet.Testing.xunit;

namespace DeploymentHelpers
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SkipIfIISVariationsNotEnabledAttribute : Attribute, ITestCondition
    {
        public bool IsMet
        {
            get
            {
                return Environment.GetEnvironmentVariable("IIS_VARIATIONS_ENABLED") == "true";
            }
        }

        public string SkipReason
        {
            get
            {
                return "Skipping IIS variation of tests. " +
                        "To run the IIS variations, setup IIS and set the environment variable IIS_VARIATIONS_ENABLED=true.";
            }
        }
    }
}