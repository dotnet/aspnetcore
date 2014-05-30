using System;
using Xunit;

namespace Microsoft.AspNet.Razor.Test
{
    public static class ExceptionHelpers
    {
        public static void ValidateArgumentException(string parameterName, string expectedMessage, ArgumentException exception)
        {
            Assert.Equal(string.Format("{0}{1}Parameter name: {2}", expectedMessage, Environment.NewLine, parameterName), exception.Message);
        }
    }
}