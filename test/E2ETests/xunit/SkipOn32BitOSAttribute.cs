using System;
using Microsoft.AspNet.Testing.xunit;

namespace E2ETests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SkipOn32BitOSAttribute : Attribute, ITestCondition
    {
        public bool IsMet
        {
            get
            {
                return Environment.Is64BitOperatingSystem;
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