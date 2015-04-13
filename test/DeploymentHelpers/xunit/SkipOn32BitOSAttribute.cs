using System;
using System.IO;
using Microsoft.AspNet.Testing.xunit;

namespace DeploymentHelpers
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SkipOn32BitOSAttribute : Attribute, ITestCondition
    {
        public bool IsMet
        {
            get
            {
                // Directory found only on 64-bit OS.
                return Directory.Exists(Path.Combine(Environment.GetEnvironmentVariable("SystemRoot"), "SysWOW64"));
            }
        }

        public string SkipReason
        {
            get
            {
                return "Skipping the AMD64 test since the OS is 32-bit";
            }
        }
    }
}