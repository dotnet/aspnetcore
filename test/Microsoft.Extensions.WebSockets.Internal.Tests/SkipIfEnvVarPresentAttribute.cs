using System;
using Microsoft.AspNetCore.Testing.xunit;

namespace Microsoft.Extensions.WebSockets.Internal.Tests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class SkipIfEnvVarPresentAttribute : Attribute, ITestCondition
    {
        private readonly string _environmentVariable;
        private readonly string _skipReason;

        public bool IsMet => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(_environmentVariable));

        public string SkipReason => _skipReason;

        public SkipIfEnvVarPresentAttribute(string environmentVariable, string skipReason)
        {
            _environmentVariable = environmentVariable;
            _skipReason = skipReason;
        }
    }
}
